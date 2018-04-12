/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Binding
{
    // Legacy connected mode:
    // Handles binding a solution for legacy connected mode i.e. writes the
    // solution-level files and adds rulesets to every applicable project.

    /// <summary>
    /// Workflow execution for the bind command
    /// </summary>
    internal class BindingWorkflow : IBindingWorkflow
    {
        private readonly IHost host;
        private readonly BindCommandArgs bindingArgs;
        private readonly IProjectSystemHelper projectSystem;
        private readonly ISolutionBindingOperation solutionBindingOperation;

        public BindingWorkflow(IHost host,
            BindCommandArgs bindingArgs,
            ISolutionBindingOperation solutionBindingOperation,
            INuGetBindingOperation nugetBindingOperation)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (bindingArgs == null)
            {
                throw new ArgumentNullException(nameof(bindingArgs));
            }
            Debug.Assert(bindingArgs.ProjectKey != null);
            Debug.Assert(bindingArgs.ProjectName != null);
            Debug.Assert(bindingArgs.Connection != null);

            if (solutionBindingOperation == null)
            {
                throw new ArgumentNullException(nameof(solutionBindingOperation));
            }

            if (nugetBindingOperation == null)
            {
                throw new ArgumentNullException(nameof(nugetBindingOperation));
            }

            this.host = host;
            this.bindingArgs = bindingArgs;
            this.projectSystem = this.host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.solutionBindingOperation = solutionBindingOperation;
            this.NuGetBindingOperation = nugetBindingOperation;
        }

        internal /*for testing*/ INuGetBindingOperation NuGetBindingOperation { get; private set;}

        #region Workflow state

        public ISet<Project> BindingProjects
        {
            get;
        } = new HashSet<Project>();

        public Dictionary<Language, RuleSet> Rulesets
        {
            get;
        } = new Dictionary<Language, RuleSet>();

        public Dictionary<Language, string> SolutionRulesetPaths
        {
            get;
        } = new Dictionary<Language, string>();

        public Dictionary<Language, SonarQubeQualityProfile> QualityProfiles
        {
            get;
        } = new Dictionary<Language, SonarQubeQualityProfile>();

        internal /*for testing purposes*/ bool BindingOperationSucceeded
        {
            get;
            set;
        } = true;

        #endregion

        #region Workflow startup

        public IProgressEvents Run()
        {
            Debug.Assert(this.host.ActiveSection != null, "Expect the section to be attached at least until this method returns");
            Debug.Assert(this.projectSystem.GetSolutionProjects().Any(), "Expecting projects in solution");

            IProgressEvents progress = ProgressStepRunner.StartAsync(this.host,
                this.host.ActiveSection.ProgressHost,
                controller => this.CreateWorkflowSteps(controller));

#if DEBUG
            progress.RunOnFinished(r => this.host.Logger.WriteLine("DEBUGONLY: Binding workflow finished, Execution result: {0}", r));
#endif
            return progress;
        }

        private ProgressStepDefinition[] CreateWorkflowSteps(IProgressController controller)
        {
            StepAttributes IndeterminateNonCancellableUIStep = StepAttributes.Indeterminate | StepAttributes.NonCancellable;
            StepAttributes HiddenIndeterminateNonImpactingNonCancellableUIStep = IndeterminateNonCancellableUIStep | StepAttributes.Hidden | StepAttributes.NoProgressImpact;
            StepAttributes HiddenNonImpactingBackgroundStep = StepAttributes.BackgroundThread | StepAttributes.Hidden | StepAttributes.NoProgressImpact;

            return new ProgressStepDefinition[]
            {
                // Some of the steps are broken into multiple sub-steps, either
                // because the work needs to be done on a different thread or
                // to report progress separate.

                //*****************************************************************
                // Initialization
                //*****************************************************************
                // Show an initial message and check the solution isn't dirty
                new ProgressStepDefinition(null, HiddenNonImpactingBackgroundStep,
                        (token, notifications) => notifications.ProgressChanged(Strings.StartedSolutionBindingWorkflow)),

                new ProgressStepDefinition(null, StepAttributes.Indeterminate | StepAttributes.Hidden,
                        (token, notifications) => this.PromptSaveSolutionIfDirty(controller, token)),

                //*****************************************************************
                // Preparation
                //*****************************************************************
                // Check for eligible projects in the solution
                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.Indeterminate,
                        (token, notifications) => this.DiscoverProjects(controller, notifications)),

                // Fetch data from Sonar server and write shared ruleset file(s) to temporary location on disk
                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread,
                        (token, notifications) => this.DownloadQualityProfileAsync(controller, notifications, this.GetBindingLanguages(), token).GetAwaiter().GetResult()),

                //*****************************************************************
                // NuGet package handling
                //*****************************************************************
                // Initialize the VS NuGet installer service
                new ProgressStepDefinition(null, HiddenIndeterminateNonImpactingNonCancellableUIStep,
                        (token, notifications) => { this.PrepareToInstallPackages(); }),

                // Install the appropriate package for each project
                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread,
                        (token, notifications) => this.InstallPackages(controller, notifications, token)),

                //*****************************************************************
                // Solution update phase
                //*****************************************************************
                // * copy shared ruleset to shared location
                // * add files to solution
                // * create/update per-project ruleset
                // * set project-level properties
                // Most of the work is delegated to SolutionBindingOperation
                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, IndeterminateNonCancellableUIStep,
                        (token, notifications) => this.InitializeSolutionBindingOnUIThread(notifications)),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread | StepAttributes.Indeterminate,
                        (token, notifications) => this.PrepareSolutionBinding(token)),

                new ProgressStepDefinition(null, StepAttributes.Hidden | StepAttributes.Indeterminate,
                        (token, notifications) => this.FinishSolutionBindingOnUIThread(controller, token)),

                //*****************************************************************
                // Finalization
                //*****************************************************************
                // Save solution and show message
                new ProgressStepDefinition(null, HiddenIndeterminateNonImpactingNonCancellableUIStep,
                        (token, notifications) => this.SilentSaveSolutionIfDirty()),

                new ProgressStepDefinition(null, HiddenNonImpactingBackgroundStep,
                        (token, notifications) => this.EmitBindingCompleteMessage(notifications))
            };
        }

        #endregion

        #region Workflow steps

        internal /*for testing purposes*/ void PromptSaveSolutionIfDirty(IProgressController controller, CancellationToken token)
        {
            if (!VsShellUtils.SaveSolution(this.host, silent: false))
            {
                this.host.Logger.WriteLine(Strings.SolutionSaveCancelledBindAborted);

                this.AbortWorkflow(controller, token);
            }
        }

        internal /*for testing purposes*/ void DiscoverProjects(IProgressController controller, IProgressStepExecutionEvents notifications)
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expected step to be run on the UI thread");

            notifications.ProgressChanged(Strings.DiscoveringSolutionProjectsProgressMessage);

            var patternFilteredProjects = this.projectSystem.GetFilteredSolutionProjects();
            var pluginAndPatternFilteredProjects =
                patternFilteredProjects.Where(p => this.host.SupportedPluginLanguages.Contains(Language.ForProject(p)));

            this.BindingProjects.UnionWith(pluginAndPatternFilteredProjects);
            this.InformAboutFilteredOutProjects();

            if (!this.BindingProjects.Any())
            {
                AbortWorkflow(controller, CancellationToken.None);
            }
        }

        internal /*for testing purposes*/ async System.Threading.Tasks.Task DownloadQualityProfileAsync(
            IProgressController controller, IProgressStepExecutionEvents notificationEvents, IEnumerable<Language> languages,
            CancellationToken cancellationToken)
        {
            Debug.Assert(controller != null);
            Debug.Assert(notificationEvents != null);

            var rulesets = new Dictionary<Language, RuleSet>();
            var languageList = languages as IList<Language> ?? languages.ToList();
            DeterminateStepProgressNotifier notifier = new DeterminateStepProgressNotifier(notificationEvents, languageList.Count);

            notifier.NotifyCurrentProgress(Strings.DownloadingQualityProfileProgressMessage);

            foreach (var language in languageList)
            {
                var serverLanguage = language.ToServerLanguage();

                var qualityProfileInfo = await WebServiceHelper.SafeServiceCall(() =>
                    this.host.SonarQubeService.GetQualityProfileAsync(
                        this.bindingArgs.ProjectKey, this.bindingArgs.Connection.Organization?.Key, serverLanguage, cancellationToken),
                    this.host.Logger);
                if (qualityProfileInfo == null)
                {
                    this.host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                       string.Format(Strings.CannotDownloadQualityProfileForLanguage, language.Name)));
                    this.AbortWorkflow(controller, cancellationToken);
                    return;
                }
                this.QualityProfiles[language] = qualityProfileInfo;

                var roslynProfileExporter = await WebServiceHelper.SafeServiceCall(() =>
                    this.host.SonarQubeService.GetRoslynExportProfileAsync(qualityProfileInfo.Name,
                        this.bindingArgs.Connection.Organization?.Key, serverLanguage, cancellationToken),
                    this.host.Logger);
                if (roslynProfileExporter == null)
                {
                    this.host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                        string.Format(Strings.QualityProfileDownloadFailedMessageFormat, qualityProfileInfo.Name,
                            qualityProfileInfo.Key, language.Name)));
                    this.AbortWorkflow(controller, cancellationToken);
                    return;
                }

                var tempRuleSetFilePath = Path.GetTempFileName();
                File.WriteAllText(tempRuleSetFilePath, roslynProfileExporter.Configuration.RuleSet.OuterXml);
                RuleSet ruleSet = RuleSet.LoadFromFile(tempRuleSetFilePath);

                if (ruleSet == null ||
                    ruleSet.Rules.Count == 0 ||
                    ruleSet.Rules.All(rule => rule.Action == RuleAction.None))
                {
                    this.host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                        string.Format(Strings.NoSonarAnalyzerActiveRulesForQualityProfile, qualityProfileInfo.Name, language.Name)));
                    this.AbortWorkflow(controller, cancellationToken);
                    return;
                }

                if (!this.NuGetBindingOperation.ProcessExport(language, roslynProfileExporter))
                {
                    this.AbortWorkflow(controller, cancellationToken);
                    return;
                }

                // Remove/Move/Refactor code when XML ruleset file is no longer downloaded but the proper API is used to retrieve rules
                UpdateDownloadedSonarQubeQualityProfile(ruleSet, qualityProfileInfo);

                rulesets[language] = ruleSet;
                notifier.NotifyIncrementedProgress(string.Empty);

                this.host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.QualityProfileDownloadSuccessfulMessageFormat, qualityProfileInfo.Name, qualityProfileInfo.Key, language.Name)));
            }

            // Set the rule set which should be available for the following steps
            foreach (var keyValue in rulesets)
            {
                this.Rulesets[keyValue.Key] = keyValue.Value;
            }
        }

        private void UpdateDownloadedSonarQubeQualityProfile(RuleSet ruleSet, SonarQubeQualityProfile qualityProfile)
        {
            ruleSet.NonLocalizedDisplayName = string.Format(Strings.SonarQubeRuleSetNameFormat, this.bindingArgs.ProjectName, qualityProfile.Name);

            var ruleSetDescriptionBuilder = new StringBuilder();
            ruleSetDescriptionBuilder.AppendLine(ruleSet.Description);
            ruleSetDescriptionBuilder.AppendFormat(Strings.SonarQubeQualityProfilePageUrlFormat, this.bindingArgs.Connection.ServerUri, qualityProfile.Key);
            ruleSet.NonLocalizedDescription = ruleSetDescriptionBuilder.ToString();

            ruleSet.WriteToFile(ruleSet.FilePath);
        }

        private void InitializeSolutionBindingOnUIThread(IProgressStepExecutionEvents notificationEvents)
        {
            Debug.Assert(System.Windows.Application.Current?.Dispatcher.CheckAccess() ?? false, "Expected to run on UI thread");

            notificationEvents.ProgressChanged(Strings.RuleSetGenerationProgressMessage);

            this.solutionBindingOperation.RegisterKnownRuleSets(this.Rulesets);
            this.solutionBindingOperation.Initialize(this.BindingProjects, this.QualityProfiles);
        }

        private void PrepareSolutionBinding(CancellationToken token)
        {
            this.solutionBindingOperation.Prepare(token);
        }

        private void FinishSolutionBindingOnUIThread(IProgressController controller, CancellationToken token)
        {
            Debug.Assert(System.Windows.Application.Current?.Dispatcher.CheckAccess() ?? false, "Expected to run on UI thread");

            if (!this.solutionBindingOperation.CommitSolutionBinding())
            {
                AbortWorkflow(controller, token);
            }
        }

        internal /*for testing purposes*/ void PrepareToInstallPackages()
        {
            this.NuGetBindingOperation.PrepareOnUIThread();
        }

        internal /*for testing purposes*/ void InstallPackages(IProgressController controller, IProgressStepExecutionEvents notificationEvents, CancellationToken token)
        {
            this.BindingOperationSucceeded = this.NuGetBindingOperation.InstallPackages(this.BindingProjects, controller, notificationEvents, token);
        }

        internal /*for testing purposes*/ void SilentSaveSolutionIfDirty()
        {
            bool saved = VsShellUtils.SaveSolution(this.host, silent: true);
            Debug.Assert(saved, "Should not be cancellable");
        }

        internal /*for testing purposes*/ void EmitBindingCompleteMessage(IProgressStepExecutionEvents notifications)
        {
            var message = this.BindingOperationSucceeded
                ? Strings.FinishedSolutionBindingWorkflowSuccessful
                : Strings.FinishedSolutionBindingWorkflowNotAllPackagesInstalled;
            notifications.ProgressChanged(message);
        }

        #endregion

        #region Helpers

        private void AbortWorkflow(IProgressController controller, CancellationToken token)
        {
            bool aborted = controller.TryAbort();
            Debug.Assert(aborted || token.IsCancellationRequested, "Failed to abort the workflow");
        }

        internal /*testing purposes*/ IEnumerable<Language> GetBindingLanguages()
        {
            return this.BindingProjects.Select(Language.ForProject)
                                       .Distinct()
                                       .Where(this.host.SupportedPluginLanguages.Contains);
        }

        private void InformAboutFilteredOutProjects()
        {
            var includedProjects = this.BindingProjects.ToList();
            var excludedProjects = this.projectSystem.GetSolutionProjects().Except(this.BindingProjects).ToList();

            var output = new StringBuilder();

            output.AppendFormat(Strings.SubTextPaddingFormat, Strings.DiscoveringSolutionIncludedProjectsHeader).AppendLine();
            if (includedProjects.Count == 0)
            {
                var message = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, Strings.NoProjectsApplicableForBinding);
                output.AppendFormat(Strings.SubTextPaddingFormat, message).AppendLine();
            }
            else
            {
                includedProjects.ForEach(
                    p =>
                    {
                        var message = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, p.UniqueName);
                        output.AppendFormat(Strings.SubTextPaddingFormat, message).AppendLine();
                    });
            }

            output.AppendFormat(Strings.SubTextPaddingFormat, Strings.DiscoveringSolutionExcludedProjectsHeader).AppendLine();
            if (excludedProjects.Count == 0)
            {
                var message = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, Strings.NoProjectsExcludedFromBinding);
                output.AppendFormat(Strings.SubTextPaddingFormat, message).AppendLine();
            }
            else
            {
                excludedProjects.ForEach(
                    p =>
                    {
                        var message = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, p.UniqueName);
                        output.AppendFormat(Strings.SubTextPaddingFormat, message).AppendLine();
                    });
            }
            output.AppendFormat(Strings.SubTextPaddingFormat, Strings.FilteredOutProjectFromBindingEnding);

            this.host.Logger.WriteLine(output.ToString());
        }

        #endregion
    }
}

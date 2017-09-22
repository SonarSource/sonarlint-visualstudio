/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    /// <summary>
    /// Workflow execution for the bind command
    /// </summary>
    internal class BindingWorkflow
    {
        private readonly IHost host;
        private readonly ConnectionInformation connectionInformation;
        private readonly SonarQubeProject project;
        private readonly IProjectSystemHelper projectSystem;
        private readonly SolutionBindingOperation solutionBindingOperation;

        public BindingWorkflow(IHost host, ConnectionInformation connectionInformation, SonarQubeProject project)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            this.host = host;
            this.connectionInformation = connectionInformation;
            this.project = project;
            this.projectSystem = this.host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.solutionBindingOperation = new SolutionBindingOperation(
                    this.host,
                    this.connectionInformation,
                    this.project.Key);
        }

        #region Workflow state

        public ISet<Project> BindingProjects
        {
            get;
        } = new HashSet<Project>();

        public Dictionary<Language, RuleSet> Rulesets
        {
            get;
        } = new Dictionary<Language, RuleSet>();

        public Dictionary<Language, List<NuGetPackageInfo>> NuGetPackages
        {
            get;
        } = new Dictionary<Language, List<NuGetPackageInfo>>();

        public Dictionary<Language, string> SolutionRulesetPaths
        {
            get;
        } = new Dictionary<Language, string>();

        public Dictionary<Language, QualityProfile> QualityProfiles
        {
            get;
        } = new Dictionary<Language, QualityProfile>();

        internal /*for testing purposes*/ bool AllNuGetPackagesInstalled
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

            this.DebugOnly_MonitorProgress(progress);

            return progress;
        }

        [Conditional("DEBUG")]
        private void DebugOnly_MonitorProgress(IProgressEvents progress)
        {
            progress.RunOnFinished(r => VsShellUtils.WriteToSonarLintOutputPane(this.host, "DEBUGONLY: Binding workflow finished, Execution result: {0}", r));
        }

        private ProgressStepDefinition[] CreateWorkflowSteps(IProgressController controller)
        {
            StepAttributes IndeterminateNonCancellableUIStep = StepAttributes.Indeterminate | StepAttributes.NonCancellable;
            StepAttributes HiddenIndeterminateNonImpactingNonCancellableUIStep = IndeterminateNonCancellableUIStep | StepAttributes.Hidden | StepAttributes.NoProgressImpact;
            StepAttributes HiddenNonImpactingBackgroundStep = StepAttributes.BackgroundThread | StepAttributes.Hidden | StepAttributes.NoProgressImpact;

            return new ProgressStepDefinition[]
            {
                new ProgressStepDefinition(null, HiddenNonImpactingBackgroundStep,
                        (token, notifications) => notifications.ProgressChanged(Strings.StartedSolutionBindingWorkflow)),

                new ProgressStepDefinition(null, StepAttributes.Indeterminate | StepAttributes.Hidden,
                        (token, notifications) => this.PromptSaveSolutionIfDirty(controller, token)),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.Indeterminate,
                        (token, notifications) => this.DiscoverProjects(controller, notifications)),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread,
                        (token, notifications) => this.DownloadQualityProfileAsync(controller, notifications, this.GetBindingLanguages(), token)),

                new ProgressStepDefinition(null, HiddenIndeterminateNonImpactingNonCancellableUIStep,
                        (token, notifications) => { NuGetHelper.LoadService(this.host); /*The service needs to be loaded on UI thread*/ }),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread,
                        (token, notifications) => this.InstallPackages(controller, token, notifications)),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, IndeterminateNonCancellableUIStep,
                        (token, notifications) => this.InitializeSolutionBindingOnUIThread(notifications)),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread | StepAttributes.Indeterminate,
                        (token, notifications) => this.PrepareSolutionBinding(token)),

                new ProgressStepDefinition(null, StepAttributes.Hidden | StepAttributes.Indeterminate,
                        (token, notifications) => this.FinishSolutionBindingOnUIThread(controller, token)),

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
                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SolutionSaveCancelledBindAborted);

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

                var qualityProfileInfo = await SafeServiceCall(
                    () => this.host.SonarQubeService.GetQualityProfileAsync(this.project.Key, serverLanguage, cancellationToken),
                    controller, cancellationToken);
                if (qualityProfileInfo == null)
                {
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, string.Format(Strings.SubTextPaddingFormat,
                       string.Format(Strings.CannotDownloadQualityProfileForLanguage, language.Name)));
                    this.AbortWorkflow(controller, cancellationToken);
                    return;
                }
                this.QualityProfiles[language] = qualityProfileInfo;

                var roslynProfileExporter = await SafeServiceCall(
                    () => this.host.SonarQubeService.GetRoslynExportProfileAsync(qualityProfileInfo.Name, serverLanguage,
                        cancellationToken), controller, cancellationToken);
                if (roslynProfileExporter == null)
                {
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, string.Format(Strings.SubTextPaddingFormat,
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
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, string.Format(Strings.SubTextPaddingFormat,
                        string.Format(Strings.NoSonarAnalyzerActiveRulesForQualityProfile, qualityProfileInfo.Name, language.Name)));
                    this.AbortWorkflow(controller, cancellationToken);
                    return;
                }

                if (roslynProfileExporter.Deployment.NuGetPackages.Count == 0)
                {
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, string.Format(Strings.SubTextPaddingFormat,
                        string.Format(Strings.NoNuGetPackageForQualityProfile, language.Name)));
                    this.AbortWorkflow(controller, cancellationToken);
                    return;
                }

                this.NuGetPackages.Add(language, roslynProfileExporter.Deployment.NuGetPackages);

                // Remove/Move/Refactor code when XML ruleset file is no longer downloaded but the proper API is used to retrieve rules
                UpdateDownloadedSonarQubeQualityProfile(ruleSet, qualityProfileInfo);

                rulesets[language] = ruleSet;
                notifier.NotifyIncrementedProgress(string.Empty);

                VsShellUtils.WriteToSonarLintOutputPane(this.host, string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.QualityProfileDownloadSuccessfulMessageFormat, qualityProfileInfo.Name, qualityProfileInfo.Key, language.Name)));
            }

            // Set the rule set which should be available for the following steps
            foreach (var keyValue in rulesets)
            {
                this.Rulesets[keyValue.Key] = keyValue.Value;
            }
        }

        private void UpdateDownloadedSonarQubeQualityProfile(RuleSet ruleSet, QualityProfile qualityProfile)
        {
            ruleSet.NonLocalizedDisplayName = string.Format(Strings.SonarQubeRuleSetNameFormat, this.project.Name, qualityProfile.Name);

            var ruleSetDescriptionBuilder = new StringBuilder();
            ruleSetDescriptionBuilder.AppendLine(ruleSet.Description);
            ruleSetDescriptionBuilder.AppendFormat(Strings.SonarQubeQualityProfilePageUrlFormat, this.connectionInformation.ServerUri, qualityProfile.Key);
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

        /// <summary>
        /// Will install the NuGet packages for the current managed projects.
        /// The packages that will be installed will be based on the information from <see cref="Analyzer.GetRequiredNuGetPackages"/>
        /// and is specific to the <see cref="RuleSet"/>.
        /// </summary>
        internal /*for testing purposes*/ void InstallPackages(IProgressController controller, CancellationToken token, IProgressStepExecutionEvents notificationEvents)
        {
            if (!this.NuGetPackages.Any())
            {
                return;
            }

            Debug.Assert(this.NuGetPackages.Count == this.NuGetPackages.Distinct().Count(), "Duplicate NuGet packages specified");

            if (!this.BindingProjects.Any())
            {
                Debug.Fail("Not expected to be called when there are no projects");
                return;
            }

            var projectNugets = this.BindingProjects
                .SelectMany(bindingProject =>
                {
                    var projectLanguage = Language.ForProject(bindingProject);

                    List<NuGetPackageInfo> nugetPackages;
                    if (!this.NuGetPackages.TryGetValue(projectLanguage, out nugetPackages))
                    {
                        var message = string.Format(Strings.BindingProjectLanguageNotMatchingAnyQualityProfileLanguage, bindingProject.Name);
                        VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SubTextPaddingFormat, message);
                        nugetPackages = new List<NuGetPackageInfo>();
                    }

                    return nugetPackages.Select(nugetPackage => new { Project = bindingProject, NugetPackage = nugetPackage });
                })
                .ToArray();

            DeterminateStepProgressNotifier progressNotifier = new DeterminateStepProgressNotifier(notificationEvents, projectNugets.Length);
            foreach (var projectNuget in projectNugets)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                string message = string.Format(CultureInfo.CurrentCulture, Strings.EnsuringNugetPackagesProgressMessage, projectNuget.NugetPackage.Id, projectNuget.Project.Name);
                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SubTextPaddingFormat, message);

                var isNugetInstallSuccessful = NuGetHelper.TryInstallPackage(this.host, projectNuget.Project, projectNuget.NugetPackage.Id, projectNuget.NugetPackage.Version);

                if (isNugetInstallSuccessful) // NuGetHelper.TryInstallPackage already displayed the error message so only take care of the success message
                {
                    message = string.Format(CultureInfo.CurrentCulture, Strings.SuccessfullyInstalledNugetPackageForProject, projectNuget.NugetPackage.Id, projectNuget.Project.Name);
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SubTextPaddingFormat, message);
                }

                // TODO: SVS-33 (https://jira.sonarsource.com/browse/SVS-33) Trigger a Team Explorer warning notification to investigate the partial binding in the output window.
                this.AllNuGetPackagesInstalled &= isNugetInstallSuccessful;

                progressNotifier.NotifyIncrementedProgress(string.Empty);
            }
        }

        internal /*for testing purposes*/ void SilentSaveSolutionIfDirty()
        {
            bool saved = VsShellUtils.SaveSolution(this.host, silent: true);
            Debug.Assert(saved, "Should not be cancellable");
        }

        internal /*for testing purposes*/ void EmitBindingCompleteMessage(IProgressStepExecutionEvents notifications)
        {
            var message = this.AllNuGetPackagesInstalled
                ? Strings.FinishedSolutionBindingWorkflowSuccessful
                : Strings.FinishedSolutionBindingWorkflowNotAllPackagesInstalled;
            notifications.ProgressChanged(message);
        }

        #endregion

        #region Helpers

        private async Task<T> SafeServiceCall<T>(Func<Task<T>> call, IProgressController controller, CancellationToken token)
        {
            try
            {
                return await call();
            }
            catch (HttpRequestException e)
            {
                // For some errors we will get an inner exception which will have a more specific information
                // that we would like to show i.e.when the host could not be resolved
                var innerException = e.InnerException as System.Net.WebException;
                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarQubeRequestFailed, e.Message, innerException?.Message);
            }
            catch (TaskCanceledException)
            {
                // Canceled or timeout
                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarQubeRequestTimeoutOrCancelled);
            }
            catch (Exception ex)
            {
                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarQubeRequestFailed, ex.Message, null);
            }

            return default(T);
        }

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

            VsShellUtils.WriteToSonarLintOutputPane(this.host, output.ToString());
        }

        #endregion
    }
}
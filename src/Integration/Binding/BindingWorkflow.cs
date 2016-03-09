//-----------------------------------------------------------------------
// <copyright file="BindingWorkflow.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Service.DataModel;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Workflow execution for the bind command
    /// </summary>
    internal class BindingWorkflow
    {
        private readonly IHost host;
        private readonly ICommand parentCommand;
        private readonly ProjectInformation project;
        private readonly IProjectSystemHelper projectSystemHelper;
        private readonly SolutionRuleSetWriter solutionRuleSetWriter;
        private readonly ProjectRuleSetWriter projectRuleSetWriter;

        internal readonly Dictionary<string, RuleSetGroup> LanguageToGroupMapping = new Dictionary<string, RuleSetGroup>
        {
            {SonarQubeServiceWrapper.CSharpLanguage, RuleSetGroup.CSharp },
            {SonarQubeServiceWrapper.VBLanguage, RuleSetGroup.VB }
        };

        public BindingWorkflow(IHost host, ICommand parentCommand, ProjectInformation project, SolutionRuleSetWriter solutionRuleSetWriter = null, ProjectRuleSetWriter projectRuleSetWriter = null, IProjectSystemHelper projectSystemHelper = null)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (parentCommand == null)
            {
                throw new ArgumentNullException(nameof(parentCommand));
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            this.host = host;
            this.parentCommand = parentCommand;
            this.project = project;
            this.projectSystemHelper = projectSystemHelper ?? new ProjectSystemHelper(this.host);
            this.solutionRuleSetWriter = solutionRuleSetWriter ?? new SolutionRuleSetWriter(this.project);
            this.projectRuleSetWriter = projectRuleSetWriter ?? new ProjectRuleSetWriter();
        }

        #region Workflow state

        public Dictionary<RuleSetGroup, RuleSet> Rulesets
        {
            get;
        } = new Dictionary<RuleSetGroup, RuleSet>();

        public List<NuGetPackageInfo> NuGetPackages
        {
            get;
        } = new List<NuGetPackageInfo>();

        public Dictionary<RuleSetGroup, string> SolutionRulesetPaths
        {
            get;
        } = new Dictionary<RuleSetGroup, string>();

        internal RuleSetInjection.RuleSetInjector RuleSetInjector
        {
            get;
            private set;
        }

        internal /*for testing purposes*/ bool AllNuGetPackagesInstalled
        {
            get;
            set;
        } = true;

        #endregion

        #region Workflow startup

        public IProgressEvents Run()
        {
            this.host.ActiveSection.UserNotifications?.HideNotification(NotificationIds.FailedToBindId);

            List<string> languages = new List<string>();
            if (this.projectSystemHelper.GetSolutionManagedProjects().Any(p => ProjectSystemHelper.IsCSharpProject(p)))
            {
                languages.Add(SonarQubeServiceWrapper.CSharpLanguage);
            }

            if (this.projectSystemHelper.GetSolutionManagedProjects().Any(p => ProjectSystemHelper.IsVBProject(p)))
            {
                languages.Add(SonarQubeServiceWrapper.VBLanguage);
            }

            Debug.Assert(languages.Count > 0, "Expecting managed projects in solution");
            Debug.Assert(this.host.ActiveSection != null, "Expect the section to be attached at least until this method returns");

            IProgressEvents progress = ProgressStepRunner.StartAsync(this.host,
                this.host.ActiveSection.ProgressHost,
                (controller) => this.CreateWorkflowSteps(controller, languages));

            this.DebugOnly_MonitorProgress(progress);

            return progress;
        }

        [Conditional("DEBUG")]
        private void DebugOnly_MonitorProgress(IProgressEvents progress)
        {
            progress.RunOnFinished(r => VsShellUtils.WriteToGeneralOutputPane(this.host, "DEBUGONLY: Binding workflow finished, Execution result: {0}", r));
        }

        private ProgressStepDefinition[] CreateWorkflowSteps(IProgressController controller, IEnumerable<string> languages)
        {
            StepAttributes IndeterminateNonCancellableUIStep = StepAttributes.Indeterminate | StepAttributes.NonCancellable;
            StepAttributes HiddenIndeterminateNonImpactingNonCancellableUIStep = IndeterminateNonCancellableUIStep | StepAttributes.Hidden | StepAttributes.NoProgressImpact;
            StepAttributes HiddenNonImpactingBackgroundStep = StepAttributes.BackgroundThread | StepAttributes.Hidden | StepAttributes.NoProgressImpact;

            return new ProgressStepDefinition[]
            {
                new ProgressStepDefinition(null, HiddenNonImpactingBackgroundStep,
                        (token, notifications) => notifications.ProgressChanged(Strings.StartedSolutionBindingWorkflow, double.NaN)),

                new ProgressStepDefinition(null, StepAttributes.Indeterminate | StepAttributes.Hidden,
                        (token, notifications) => this.PromptSaveSolutionIfDirty(controller, token)),

                new ProgressStepDefinition(null, StepAttributes.BackgroundThread | StepAttributes.Hidden | StepAttributes.Indeterminate,
                        (token, notifications) => this.VerifyServerPlugins(controller, token, notifications)),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread,
                        (token, notifications) => this.DownloadQualityProfile(controller, token, notifications, languages)),

                new ProgressStepDefinition(null, IndeterminateNonCancellableUIStep,
                        (token, notifications) => { NuGetHelper.LoadService(this.host); /*The service needs to be loaded on UI thread*/ }),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread,
                        (token, notifications) => this.InstallPackages(controller, token, notifications)),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, IndeterminateNonCancellableUIStep,
                        (token, notifications) => this.PrepareRuleSetInjector(notifications)),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread | StepAttributes.Indeterminate,
                        (token, notifications) => this.PrepareRuleSets(token)),

                new ProgressStepDefinition(null, HiddenIndeterminateNonImpactingNonCancellableUIStep,
                        (token, notifications) => this.FinishBindingOnUIThread()),

                new ProgressStepDefinition(null, HiddenIndeterminateNonImpactingNonCancellableUIStep,
                        (token, notifications) => this.SilentSaveSolutionIfDirty()),

                new ProgressStepDefinition(null, HiddenNonImpactingBackgroundStep,
                        (token, notifications) => this.EmitBindingCompleteMessage(notifications))
            };
        }

        #endregion

        #region Workflow steps
        private void AbortWorkflow(IProgressController controller, CancellationToken token)
        {
            bool aborted = controller.TryAbort();
            Debug.Assert(aborted || token.IsCancellationRequested, "Failed to abort the workflow");
        }

        internal /*for testing purposes*/ void PromptSaveSolutionIfDirty(IProgressController controller, CancellationToken token)
        {
            if (!VsShellUtils.SaveSolution(this.host, silent: false))
            {
                VsShellUtils.WriteToGeneralOutputPane(this.host, Strings.SolutionSaveCancelledBindAborted);

                this.AbortWorkflow(controller, token);
            }
        }

        internal /*for testing purposes*/ void SilentSaveSolutionIfDirty()
        {
            bool saved = VsShellUtils.SaveSolution(this.host, silent: true);
            Debug.Assert(saved, "Should not be cancellable");
        }

        internal /*for testing purposes*/ void VerifyServerPlugins(IProgressController controller, CancellationToken token, IProgressStepExecutionEvents notifications)
        {
            var csPluginVersion = this.host.SonarQubeService.GetPluginVersion(ServerPlugin.CSharpPluginKey, token);
            if (string.IsNullOrWhiteSpace(csPluginVersion) || VersionHelper.Compare(csPluginVersion, ServerPlugin.CSharpPluginMinimumVersion) < 0)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, Strings.ServerDoesNotHaveCorrectVersionOfCSharpPlugin, ServerPlugin.CSharpPluginMinimumVersion);
                VsShellUtils.WriteToGeneralOutputPane(this.host, errorMessage);

                this.AbortWorkflow(controller, token);
            }
        }

        internal /*for testing purposes*/ void DownloadQualityProfile(IProgressController controller, CancellationToken cancellationToken, IProgressStepExecutionEvents notificationEvents, IEnumerable<string> languages)
        {
            Debug.Assert(controller != null);
            Debug.Assert(notificationEvents != null);

            bool failed = false;
            Dictionary<string, RuleSet> rulesets = new Dictionary<string, RuleSet>();
            DeterminateStepProgressNotifier notifier = new DeterminateStepProgressNotifier(notificationEvents, languages.Count());

            foreach (var language in languages)
            {
                notifier.NotifyCurrentProgress(string.Format(CultureInfo.CurrentCulture, Strings.DownloadingQualityProfileProgressMessage, language));

                var export = this.host.SonarQubeService.GetExportProfile(this.project, language, cancellationToken);

                if (export == null)
                {
                    failed = true;
                    break;
                }

                this.NuGetPackages.AddRange(export.Deployment.NuGetPackages);

                var tempRuleSetFilePath = Path.GetTempFileName();
                File.WriteAllText(tempRuleSetFilePath, export.Configuration.RuleSet.OuterXml);
                RuleSet ruleSet = RuleSet.LoadFromFile(tempRuleSetFilePath);

                rulesets[language] = ruleSet;
                notifier.NotifyIncrementedProgress(string.Empty);
                if (rulesets[language] == null)
                {
                    failed = true;
                    break;
                }
            }

            if (failed)
            {
                VsShellUtils.WriteToGeneralOutputPane(this.host, Strings.QualityProfileDownloadFailedMessage);
                bool aborted = controller.TryAbort();
                Debug.Assert(aborted || cancellationToken.IsCancellationRequested, "Failed to abort the workflow");
            }
            else
            {
                // Set the rule set which should be available for the following steps
                foreach(var keyValue in rulesets)
                {
                    this.Rulesets[this.LanguageToGroup(keyValue.Key)] = keyValue.Value;
                }

                notifier.NotifyCurrentProgress(Strings.QualityProfileDownloadedSuccessfulMessage);
            }
        }

        private RuleSetGroup LanguageToGroup(string language)
        {
            RuleSetGroup group;
            if (!this.LanguageToGroupMapping.TryGetValue(language, out group))
            {
                Debug.Fail("Unsupported language: " + language);
                throw new InvalidOperationException();
            }
            return group;
        }

        private void PrepareRuleSetInjector(IProgressStepExecutionEvents notificationEvents)
        {
            Debug.Assert(System.Windows.Application.Current?.Dispatcher.CheckAccess() ?? false, "Expected to run on UI thread");

            notificationEvents.ProgressChanged(Strings.RuleSetGenerationProgressMessage, double.NaN);

            this.RuleSetInjector = new RuleSetInjection.RuleSetInjector(this.projectSystemHelper, this.SetSolutionRuleSet, this.UpdateProjectRuleSet);
        }

        private void PrepareRuleSets(CancellationToken token)
        {
            this.RuleSetInjector.PrepareUpdates(token);
        }

        private void FinishBindingOnUIThread()
        {
            Debug.Assert(System.Windows.Application.Current?.Dispatcher.CheckAccess() ?? false, "Expected to run on UI thread");

            this.RuleSetInjector.CommitUpdates();

            this.PersistBinding();
        }

        /// <summary>
        /// Will persist the binding information for next time usage
        /// </summary>
        internal /*for testing purposes*/ void PersistBinding(ICredentialStore credentialStore = null, IProjectSystemHelper projectSystem = null)
        {
            Debug.Assert(this.host.SonarQubeService.CurrentConnection != null, "Connection expected");
            ConnectionInformation connection = this.host.SonarQubeService.CurrentConnection;

            BasicAuthCredentials credentials = connection.UserName == null? null : new BasicAuthCredentials(connection.UserName, connection.Password);

            SolutionBinding binding = new SolutionBinding(this.host, credentialStore, projectSystem);
            binding.WriteSolutionBinding(new BoundSonarQubeProject(connection.ServerUri, this.project.Key, credentials));
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

            var managedProjects = this.projectSystemHelper.GetSolutionManagedProjects().ToArray();
            if (!managedProjects.Any())
            {
                Debug.Fail("Not expected to be called when there are no managed projects");
                return;
            }

            DeterminateStepProgressNotifier progressNotifier = new DeterminateStepProgressNotifier(notificationEvents, managedProjects.Length * this.NuGetPackages.Count);
            foreach (var project in managedProjects)
            {
                foreach (var packageInfo in this.NuGetPackages)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    string message = string.Format(CultureInfo.CurrentCulture, Strings.EnsuringNugetPackagesProgressMessage, packageInfo.Id, project.Name);
                    progressNotifier.NotifyCurrentProgress(message);

                    // TODO: SVS-33 (https://jira.sonarsource.com/browse/SVS-33) Trigger a Team Explorer warning notification to investigate the partial binding in the output window.
                    this.AllNuGetPackagesInstalled &= NuGetHelper.TryInstallPackage(this.host, project, packageInfo.Id, packageInfo.Version);

                    progressNotifier.NotifyIncrementedProgress(string.Empty);
                }
            }
        }

        internal /*for testing purposes*/ string SetSolutionRuleSet(RuleSetGroup group, string solutionFullPath)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(solutionFullPath), "Expecting a solution file path");
            Debug.Assert(this.Rulesets.ContainsKey(group) && this.Rulesets[group] != null, $"Rule set should have been stashed by previous step ({nameof(DownloadQualityProfile)})");

            RuleSet ruleset;
            string path = null;
            if (this.Rulesets.TryGetValue(group, out ruleset) && ruleset!=null)
            {
                path = this.solutionRuleSetWriter.WriteSolutionLevelRuleSet(solutionFullPath, ruleset, fileNameSuffix: group.ToString());
                this.SolutionRulesetPaths[group] = path;
            }

            return path;
        }

        internal /*for testing purposes*/ string UpdateProjectRuleSet(RuleSetGroup group, string projectFullPath, string configurationName, string currentRuleSet)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(projectFullPath), "Expecting a project full path");
            Debug.Assert(this.SolutionRulesetPaths.ContainsKey(group) && this.SolutionRulesetPaths[group] != null, $"Rule set should have been stashed by previous step ({nameof(DownloadQualityProfile)})");

            string solutionRuleSetPath = null;
            string projectRuleSetPath = null;
            if (this.SolutionRulesetPaths.TryGetValue(group, out solutionRuleSetPath))
            {
                projectRuleSetPath = this.projectRuleSetWriter.WriteProjectLevelRuleSet(projectFullPath, configurationName, solutionRuleSetPath, currentRuleSet);
            }

            return projectRuleSetPath;
        }

        internal /*for testing purposes*/ void EmitBindingCompleteMessage(IProgressStepExecutionEvents notifications)
        {
            var message = this.AllNuGetPackagesInstalled
                ? Strings.FinishedSolutionBindingWorkflowSuccessful
                : Strings.FinishedSolutionBindingWorkflowNotAllPackagesInstalled;
            notifications.ProgressChanged(message, double.NaN);
        }

        #endregion
    }
}

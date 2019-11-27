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
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Progress.Controller;

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
        private readonly IProjectSystemHelper projectSystem;

        public BindingWorkflow(IHost host,
            BindCommandArgs bindingArgs,
            ISolutionBindingOperation solutionBindingOperation,
            INuGetBindingOperation nugetBindingOperation,
            ISolutionBindingInformationProvider bindingInformationProvider,
            bool isFirstBinding = false)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.BindingProcessImpl = new BindingProcessImpl(host, bindingArgs, solutionBindingOperation,
                nugetBindingOperation, bindingInformationProvider, isFirstBinding);

            this.host = host;
            this.projectSystem = this.host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();
        }

        // duncanp
        internal /*for testing*/ BindingProcessImpl BindingProcessImpl { get; }

        // duncanp - remove when tests refactored
        internal /*for testing*/ BindingProcessImpl.BindingProcessState State { get { return BindingProcessImpl.InternalState; } }

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
            const StepAttributes IndeterminateNonCancellableUIStep = StepAttributes.Indeterminate | StepAttributes.NonCancellable;
            const StepAttributes HiddenIndeterminateNonImpactingNonCancellableUIStep = IndeterminateNonCancellableUIStep | StepAttributes.Hidden | StepAttributes.NoProgressImpact;
            const StepAttributes HiddenNonImpactingBackgroundStep = StepAttributes.BackgroundThread | StepAttributes.Hidden | StepAttributes.NoProgressImpact;

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
                        (token, notifications) => this.DownloadQualityProfileAsync(controller, notifications, BindingProcessImpl.GetBindingLanguages(), token).GetAwaiter().GetResult()),

                //*****************************************************************
                // NuGet package handling
                //*****************************************************************
                // Initialize the VS NuGet installer service
                new ProgressStepDefinition(null, HiddenIndeterminateNonImpactingNonCancellableUIStep,
                        (token, notifications) => { this.PrepareToInstallPackages(); }),

                // Install the appropriate package for each project
                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread,
                        (token, notifications) => this.InstallPackages(notifications, token)),

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
            if (!BindingProcessImpl.PromptSaveSolutionIfDirty())
            {
                this.host.Logger.WriteLine(Strings.SolutionSaveCancelledBindAborted);

                this.AbortWorkflow(controller, token);
            }
        }

        internal /*for testing purposes*/ void DiscoverProjects(IProgressController controller, IProgressStepExecutionEvents notifications)
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expected step to be run on the UI thread");

            notifications.ProgressChanged(Strings.DiscoveringSolutionProjectsProgressMessage);

            if (!BindingProcessImpl.DiscoverProjects())
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

            if (!await BindingProcessImpl.DownloadQualityProfileAsync(notificationEvents, languages, cancellationToken).ConfigureAwait(false))
            {
                this.AbortWorkflow(controller, cancellationToken);
            }
        }

        private void InitializeSolutionBindingOnUIThread(IProgressStepExecutionEvents notificationEvents)
        {
            Debug.Assert(host.UIDispatcher.CheckAccess(), "Expected to run on UI thread");

            notificationEvents.ProgressChanged(Strings.RuleSetGenerationProgressMessage);

            BindingProcessImpl.InitializeSolutionBindingOnUIThread();
        }

        private void PrepareSolutionBinding(CancellationToken token)
        {
            this.BindingProcessImpl.PrepareSolutionBinding(token);
        }

        private void FinishSolutionBindingOnUIThread(IProgressController controller, CancellationToken token)
        {
            Debug.Assert(host.UIDispatcher.CheckAccess(), "Expected to run on UI thread");

            if (!BindingProcessImpl.FinishSolutionBindingOnUIThread())
            {
                AbortWorkflow(controller, token);
            }
        }

        internal /*for testing purposes*/ void PrepareToInstallPackages()
        {
            BindingProcessImpl.PrepareToInstallPackages();
        }

        internal /*for testing purposes*/ void InstallPackages(IProgressStepExecutionEvents notificationEvents, CancellationToken token)
        {
            BindingProcessImpl.InstallPackages(notificationEvents, token);
        }

        internal /*for testing purposes*/ void SilentSaveSolutionIfDirty()
        {
            BindingProcessImpl.SilentSaveSolutionIfDirty();
        }

        internal /*for testing purposes*/ void EmitBindingCompleteMessage(IProgressStepExecutionEvents notifications)
        {
            var message = this.BindingProcessImpl.BindOperationSucceeded
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

        #endregion
    }
}

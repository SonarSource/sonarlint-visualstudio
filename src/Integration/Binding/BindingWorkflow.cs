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

        private readonly IBindingProcess bindingProcess;

        public BindingWorkflow(IHost host,
            IBindingProcess bindingProcess)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (bindingProcess == null)
            {
                throw new ArgumentNullException(nameof(bindingProcess));
            }

            this.bindingProcess = bindingProcess;
            this.host = host;
        }

        #region Workflow startup

        public IProgressEvents Run()
        {
            Debug.Assert(this.host.ActiveSection != null, "Expect the section to be attached at least until this method returns");

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
                        (token, notifications) => this.DownloadQualityProfileAsync(controller, notifications, token).GetAwaiter().GetResult()),

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
            if (!bindingProcess.PromptSaveSolutionIfDirty())
            {
                this.AbortWorkflow(controller, token);
            }
        }

        internal /*for testing purposes*/ void DiscoverProjects(IProgressController controller, IProgressStepExecutionEvents notifications)
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expected step to be run on the UI thread");

            notifications.ProgressChanged(Strings.DiscoveringSolutionProjectsProgressMessage);

            if (!bindingProcess.DiscoverProjects())
            {
                AbortWorkflow(controller, CancellationToken.None);
            }
        }

        internal /*for testing purposes*/ async System.Threading.Tasks.Task DownloadQualityProfileAsync(
            IProgressController controller, IProgressStepExecutionEvents notificationEvents,
            CancellationToken cancellationToken)
        {
            Debug.Assert(controller != null);
            Debug.Assert(notificationEvents != null);

            var progressAdapter = new FixedStepsProgressAdapter(notificationEvents);
            if (!await bindingProcess.DownloadQualityProfileAsync(progressAdapter, cancellationToken).ConfigureAwait(false))
            {
                this.AbortWorkflow(controller, cancellationToken);
            }
        }

        internal /* for testing */ void InitializeSolutionBindingOnUIThread(IProgressStepExecutionEvents notificationEvents)
        {
            Debug.Assert(host.UIDispatcher.CheckAccess(), "Expected to run on UI thread");

            notificationEvents.ProgressChanged(Strings.RuleSetGenerationProgressMessage);

            bindingProcess.InitializeSolutionBindingOnUIThread();
        }

        internal /* for testing */ void PrepareSolutionBinding(CancellationToken token)
        {
            this.bindingProcess.PrepareSolutionBinding(token);
        }

        internal /* for testing */ void FinishSolutionBindingOnUIThread(IProgressController controller, CancellationToken token)
        {
            Debug.Assert(host.UIDispatcher.CheckAccess(), "Expected to run on UI thread");

            if (!bindingProcess.FinishSolutionBindingOnUIThread())
            {
                AbortWorkflow(controller, token);
            }
        }

        internal /*for testing purposes*/ void PrepareToInstallPackages()
        {
            bindingProcess.PrepareToInstallPackages();
        }

        internal /*for testing purposes*/ void InstallPackages(IProgressStepExecutionEvents notificationEvents, CancellationToken token)
        {
            var progressAdapter = new FixedStepsProgressAdapter(notificationEvents);
            bindingProcess.InstallPackages(progressAdapter, token);
        }

        internal /*for testing purposes*/ void SilentSaveSolutionIfDirty()
        {
            bindingProcess.SilentSaveSolutionIfDirty();
        }

        internal /*for testing purposes*/ void EmitBindingCompleteMessage(IProgressStepExecutionEvents notifications)
        {
            var message = this.bindingProcess.BindOperationSucceeded
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

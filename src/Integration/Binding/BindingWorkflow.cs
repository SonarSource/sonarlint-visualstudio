﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Diagnostics;
using System.Threading;
using SonarLint.VisualStudio.ConnectedMode.Binding;
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
            const StepAttributes HiddenNonImpactingBackgroundStep = StepAttributes.BackgroundThread | StepAttributes.Hidden | StepAttributes.NoProgressImpact;

            return new ProgressStepDefinition[]
            {
                // Some of the steps are broken into multiple sub-steps, either
                // because the work needs to be done on a different thread or
                // to report progress separate.

                //*****************************************************************
                // Initialization
                //*****************************************************************
                // Show an initial message
                new ProgressStepDefinition(null, HiddenNonImpactingBackgroundStep,
                        (token, notifications) => notifications.ProgressChanged(Strings.StartedSolutionBindingWorkflow)),

                //*****************************************************************
                // Preparation
                //*****************************************************************
                // Fetch data from Sonar server and write shared ruleset file(s) to temporary location on disk
                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread,
                        (token, notifications) => this.DownloadQualityProfileAsync(controller, notifications, token).GetAwaiter().GetResult()),

                //*****************************************************************
                // Solution update phase
                //*****************************************************************
                // * write config files to non-scc location
                // Most of the work is delegated to SolutionBindingOperation

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread | StepAttributes.Indeterminate,
                        (token, notifications) => this.SaveRuleConfiguration(token)),

                new ProgressStepDefinition(Strings.BindingProjectsDisplayMessage, StepAttributes.BackgroundThread | StepAttributes.Indeterminate,
                        (token, notifications) => this.SaveServerExclusionsAsync(controller, notifications, token).GetAwaiter().GetResult()),

                //*****************************************************************
                // Finalization
                //*****************************************************************
                // Show final message
                new ProgressStepDefinition(null, HiddenNonImpactingBackgroundStep,
                        (token, notifications) => this.EmitBindingCompleteMessage(notifications))
            };
        }

        #endregion

        #region Workflow steps

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

        internal /*for testing purposes*/ async System.Threading.Tasks.Task SaveServerExclusionsAsync(
            IProgressController controller, IProgressStepExecutionEvents notificationEvents,
            CancellationToken cancellationToken)
        {
            Debug.Assert(controller != null);
            Debug.Assert(notificationEvents != null);

            notificationEvents.ProgressChanged(Strings.SaveServerExclusionsMessage);
            if (!await bindingProcess.SaveServerExclusionsAsync(cancellationToken).ConfigureAwait(false))
            {
                this.AbortWorkflow(controller, cancellationToken);
            }
        }

        internal /* for testing */ void SaveRuleConfiguration(CancellationToken token)
        {
            this.bindingProcess.SaveRuleConfiguration(token);
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

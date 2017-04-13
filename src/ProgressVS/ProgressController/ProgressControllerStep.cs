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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.Threading;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Implementation of a step that can be executed by a <see cref="IProgressController"/>
    /// </summary>
    /// <remarks>Each instance can be executed only once</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2302:FlagServiceProviders", Justification = "Forwarding service provider")]
    public partial class ProgressControllerStep
    {
        #region Fields
        private readonly ProgressStepDefinition definition;
        private readonly IProgressController controller;
        private StepExecutionState state;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a step from definition
        /// </summary>
        /// <param name="controller">The hosting <see cref="IProgressController"/> for this step</param>
        /// <param name="definition">The <see cref="ProgressStepDefinition"/> for which to create the step</param>
        public ProgressControllerStep(IProgressController controller, ProgressStepDefinition definition)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            this.controller = controller;
            this.definition = definition;
            this.Initialize();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Whether to execute the step on the foreground or background thread
        /// </summary>
        public StepExecution Execution
        {
            get;
            private set;
        }

        #endregion

        #region Extension support
        /// <summary>
        /// Executes <see cref="ExecuteOperation"/> while updating the <see cref="State"/> on every change and suppressing non-critical exceptions.
        /// </summary>
        /// <param name="progressCallback">Callback instance to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Stateful", Justification = "False positive")]
        protected virtual void DoStatefulExecution(IProgressStepExecutionEvents progressCallback, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(this.ExecutionState == StepExecutionState.NotStarted, "Unexpected stated");
                if (this.Execution == StepExecution.ForegroundThread)
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                }
                else
                {
                    Debug.Assert(this.Execution == StepExecution.BackgroundThread, "Unexpected enum value: " + this.Execution);
                    ThreadHelper.ThrowIfOnUIThread();
                }

                this.ExecutionState = StepExecutionState.Executing;
                this.ExecuteOperation(cancellationToken, progressCallback);
                this.ExecutionState = StepExecutionState.Succeeded;
            }
            catch (OperationCanceledException)
            {
                this.ExecutionState = StepExecutionState.Cancelled;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString(), "DoStatefulExecution-Failed");
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }

                this.ExecutionState = StepExecutionState.Failed;

                Debug.Assert(this.controller.ErrorNotificationManager != null, "Expecting valid ErrorNotifier");
                if (this.controller.ErrorNotificationManager != null)
                {
                    this.controller.ErrorNotificationManager.Notify(ex);
                }
            }
        }

        /// <summary>
        /// Sets the whether <see cref="Indeterminate"/> and updates the inital <see cref="Progress"/>
        /// </summary>
        /// <param name="indeterminate">Whether requested an indeterminate step</param>
        protected void SetStepKind(bool indeterminate)
        {
            if (this.ExecutionState != StepExecutionState.NotStarted)
            {
                Debug.Fail("StepKind can only be changed whilst not started");
                return;
            }

            if (indeterminate)
            {
                this.Progress = ProgressControllerHelper.Indeterminate;
                this.Indeterminate = true;
            }
            else
            {
                this.Progress = 0;
                this.Indeterminate = false;
            }
        }

        /// <summary>
        /// Executes the operation
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="progressCallback">The callback instance to use when executing the operation</param>
        protected void ExecuteOperation(CancellationToken cancellationToken, IProgressStepExecutionEvents progressCallback)
        {
            this.definition.Operation(cancellationToken, progressCallback);
        }
        #endregion

        #region Static helpers
        /// <summary>
        /// Returns the <see cref="VsTaskRunContext"/> for <see cref="StepExecution"/>
        /// </summary>
        /// <param name="execution">The requested <see cref="StepExecution"/></param>
        /// <returns>The <see cref="VsTaskRunContext"/> to use</returns>
        private static VsTaskRunContext GetContext(StepExecution execution)
        {
            if (execution == StepExecution.ForegroundThread)
            {
                return VsTaskRunContext.UIThreadNormalPriority;
            }
            else
            {
                Debug.Assert(execution == StepExecution.BackgroundThread, "Unexpected input: " + execution);
                return VsTaskRunContext.BackgroundThread;
            }
        }
        #endregion

        #region Private
        /// <summary>
        /// Initializes the step
        /// </summary>
        private void Initialize()
        {
            this.state = StepExecutionState.NotStarted;

            bool indeterminate = (this.definition.Attributes & StepAttributes.Indeterminate) != 0;
            StepExecution execution = (this.definition.Attributes & StepAttributes.BackgroundThread) != 0 ? StepExecution.BackgroundThread : StepExecution.ForegroundThread;
            bool hidden = (this.definition.Attributes & StepAttributes.Hidden) != 0;
            bool cancellable = (this.definition.Attributes & StepAttributes.NonCancellable) == 0;
            bool impactingProgress = (this.definition.Attributes & StepAttributes.NoProgressImpact) == 0;
            string displayText = this.definition.DisplayText;

            this.SetStepKind(indeterminate);
            this.Hidden = hidden;
            this.DisplayText = displayText;
            this.Execution = execution;
            this.Cancellable = cancellable;
            this.ImpactsProgress = impactingProgress;
        }
        #endregion
    }
}

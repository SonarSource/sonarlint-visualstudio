//-----------------------------------------------------------------------
// <copyright file="ProgressControllerStep.IProgressStepExecutionEvents.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Threading;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressStepExecutionEvents"/>
    /// </summary>
    public partial class ProgressControllerStep : IProgressStepExecutionEvents
    {
        private event EventHandler<StepExecutionChangedEventArgs> StateChangedPrivate;

        void IProgressStepExecutionEvents.ProgressChanged(string progressDetailText, double progress)
        {
            this.UpdateProgress(progressDetailText, progress);
        }

        /// <summary>
        /// Updates the progress with the specified values
        /// </summary>
        /// <param name="progressDetailText">Optional progress detail text</param>
        /// <param name="progress">Progress in a range of 0.0 to 1.0</param>
        protected void UpdateProgress(string progressDetailText, double progress)
        {
            Debug.Assert(this.ExecutionState == StepExecutionState.Executing, "ProgressChanged is expected to be used only when executing");
            this.ProgressDetailText = progressDetailText;
            this.Progress = progress;
            this.OnExecutionStateChanged();
        }

        /// <summary>
        /// Invokes the <see cref="StateChanged"/> event based on the <see cref="ExecutionState"/> of the object
        /// </summary>
        protected virtual void OnExecutionStateChanged()
        {
            if (this.StateChangedPrivate != null)
            {
                VsThreadingHelper.RunInline(this.controller, VsTaskRunContext.UIThreadBackgroundPriority,
                    () =>
                    {
                        var delegates = this.StateChangedPrivate;
                        if (delegates != null)
                        {
                            delegates(this, new StepExecutionChangedEventArgs(this));
                        }
                    });
            }
        }
    }
}

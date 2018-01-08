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
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Progress.Threading;

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

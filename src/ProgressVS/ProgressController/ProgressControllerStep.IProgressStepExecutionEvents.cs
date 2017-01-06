/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

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

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

using System;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Event arguments for a single <see cref="IProgressStep"/> being executed by the <see cref="IProgressController"/>
    /// <seealso cref="IProgressEvents"/>
    /// </summary>
    public class StepExecutionChangedEventArgs : ProgressEventArgs
    {
        public StepExecutionChangedEventArgs(IProgressStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            this.Step = step;
            this.State = step.ExecutionState;
            this.Progress = step.Progress;
            this.ProgressDetailText = step.ProgressDetailText;
        }

        /// <summary>
        /// Step execution state
        /// </summary>
        public StepExecutionState State
        {
            get;
            private set;
        }

        /// <summary>
        /// Progress text details. Can be null
        /// </summary>
        public string ProgressDetailText
        {
            get;
            private set;
        }

        /// <summary>
        /// Progress values between 0.0 and 1.0 or indeterminate. Use <see cref="IsProgressDeterminate"/> to decide
        /// </summary>
        public double Progress
        {
            get;
            private set;
        }

        /// <summary>
        /// The step being executed
        /// </summary>
        public IProgressStep Step
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns whether <see cref="Progress"/>  is indeterminate
        /// </summary>
        /// <returns>Whether the current arguments are for indeterminate progress</returns>
        public bool IsProgressIndeterminate()
        {
            return ProgressControllerHelper.IsIndeterminate(this.Progress);
        }
    }
}

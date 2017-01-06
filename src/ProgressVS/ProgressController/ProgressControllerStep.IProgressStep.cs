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

using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressStep"/>
    /// </summary>
    public partial class ProgressControllerStep : IProgressStep
    {
        /// <summary>
        /// Step execution change event
        /// </summary>
        public event EventHandler<StepExecutionChangedEventArgs> StateChanged
        {
            add
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.StateChangedPrivate += value;
            }

            remove
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.StateChangedPrivate -= value;
            }
        }

        public string DisplayText
        {
            get;
            private set;
        }

        public double Progress
        {
            get;
            private set;
        }

        public string ProgressDetailText
        {
            get;
            private set;
        }

        public StepExecutionState ExecutionState
        {
            get
            {
                return this.state;
            }

            protected set
            {
                Debug.Assert(this.state != value, "Unexpected transition to the same state");
                if (this.state != value)
                {
                    this.state = value;
                    this.OnExecutionStateChanged();
                }
            }
        }

        public bool Hidden
        {
            get;
            private set;
        }

        public bool Indeterminate
        {
            get;
            private set;
        }

        public bool Cancellable
        {
            get;
            private set;
        }

        public bool ImpactsProgress
        {
            get;
            private set;
        }
    }
}

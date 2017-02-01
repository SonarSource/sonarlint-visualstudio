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
using System.Threading;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressStep"/>
    /// </summary>
    public partial class ConfigurableProgressTestOperation : IProgressStep
    {
        private readonly Action<CancellationToken, IProgressStepExecutionEvents> operation;
        internal bool IsExecuted { get; private set; }

        public ConfigurableProgressTestOperation(Action<CancellationToken, IProgressStepExecutionEvents> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException("operation");
            }

            this.operation = operation;
            this.CancellableAction = () => true;
            this.ExecutionResult = StepExecutionState.Succeeded;
        }

#pragma warning disable 67

        public event EventHandler<StepExecutionChangedEventArgs> StateChanged;

#pragma warning restore 67

        #region Customization methods

        /// <summary>
        /// Simulate this final execution result after running the operation
        /// </summary>
        public StepExecutionState ExecutionResult
        {
            get;
            set;
        }

        /// <summary>
        /// Delegate that is executed to determine if a step is cancellable
        /// </summary>
        public Func<bool> CancellableAction
        {
            get;
            set;
        }

        #endregion Customization methods

        #region IProgressStep

        public string DisplayText
        {
            get;
            set;
        }

        public double Progress
        {
            get;
            set;
        }

        public string ProgressDetailText
        {
            get;
            set;
        }

        public StepExecutionState ExecutionState
        {
            get;
            set;
        }

        public bool Hidden
        {
            get;
            set;
        }

        public bool Indeterminate
        {
            get;
            set;
        }

        public bool Cancellable
        {
            get
            {
                return this.CancellableAction();
            }
        }

        public bool ImpactsProgress
        {
            get;
            set;
        }

        #endregion IProgressStep
    }
}
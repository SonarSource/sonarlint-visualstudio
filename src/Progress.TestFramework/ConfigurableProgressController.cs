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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// A test implementation of <see cref="IProgressController"/> which is capable of verifying <see cref="IProgressStepExecutionEvents"/>
    /// in addition to executing <see cref="IProgressStepOperation"/> and provide cancellation support
    /// </summary>
    public partial class ConfigurableProgressController : IDisposable
    {
        #region Fields

        private const int DefaultWaitForCompletionMS = 2000;

        private readonly int waitForCompletion;
        private CancellationTokenSource cts;
        private readonly List<Tuple<string, double>> progressChanges = new List<Tuple<string, double>>();
        private readonly List<IProgressStepOperation> stepOperations = new List<IProgressStepOperation>();
        private bool canAbort;

        #endregion Fields

        public ConfigurableProgressController(IServiceProvider serviceProvider, int waitForCompletion = DefaultWaitForCompletionMS)
        {
            this.ServiceProvider = serviceProvider;
            this.waitForCompletion = Debugger.IsAttached ? waitForCompletion * 100 : waitForCompletion;
            this.Reset();
        }

        #region Customization properties

        /// <summary>
        /// An optional delegate to be executed after the controller has started and before the test step is invoked
        /// </summary>
        public Action<IProgressController> ExecuteAfterStarted
        {
            get;
            set;
        }

        public ProgressControllerResult ReturnResult
        {
            get;
            set;
        }

        public Func<bool> TryAbortAction
        {
            get;
            set;
        }

        public ConfigurableProgressEvents Events
        {
            get;
            set;
        }

        public IErrorNotificationManager ErrorNotificationManager
        {
            get;
            private set;
        }

        public IServiceProvider ServiceProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the current step being executed is cancellable
        /// </summary>
        public bool IsCurrentStepCancellable
        {
            get { return this.canAbort; }
        }

        #endregion Customization properties

        #region Configuration and verification methods

        /// <summary>
        /// Resets the configuration
        /// </summary>
        public void Reset()
        {
            this.canAbort = true;
            if (this.cts != null)
            {
                this.cts.Dispose();
            }

            this.cts = new CancellationTokenSource();
            this.progressChanges.Clear();
            this.Events = new ConfigurableProgressEvents();
            this.ErrorNotificationManager = new ErrorNotificationManager();
        }

        /// <summary>
        /// Cancels the execution
        /// </summary>
        public void Cancel()
        {
            this.cts.Cancel();
        }

        /// <summary>
        /// Executes the operation and waits for completion
        /// </summary>
        /// <param name="stepOperation">The operation to execute</param>
        public void Execute(IProgressStepOperation stepOperation)
        {
            this.stepOperations.Clear();
            // Insert a delegate that will be executed after the controller has started and before the actual step operation
            if (this.ExecuteAfterStarted != null)
            {
                this.stepOperations.Add(new ConfigurableProgressTestOperation((c, n) => this.ExecuteAfterStarted(this)));
            }

            this.stepOperations.Add(stepOperation);

            using (ManualResetEventSlim signal = new ManualResetEventSlim())
            {
                ((IProgressController)this).Start().ContinueWith(t => signal.Set());

                signal.Wait(this.waitForCompletion);
            }
        }

        public void AssertProgressChangeEvents(List<Tuple<string, double>> expectedOrderedProgressEvents)
        {
            progressChanges.Should().Equal(expectedOrderedProgressEvents);
        }

        public void AssertNoProgressChangeEvents()
        {
            progressChanges.Should().BeEmpty();
        }

        #endregion Configuration and verification methods

        #region IDisposable

        public void Dispose()
        {
            if (this.cts != null)
            {
                this.cts.Dispose();
                this.cts = null;
            }
        }

        #endregion IDisposable
    }
}
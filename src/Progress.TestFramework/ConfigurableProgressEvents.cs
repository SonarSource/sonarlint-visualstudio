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

using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressEvents"/>
    /// </summary>
    public class ConfigurableProgressEvents : IProgressEvents
    {
        public ConfigurableProgressEvents()
        {
            this.Steps = new IProgressStep[0];
        }

        #region Test implementation of IProgressEvents
        public event EventHandler<ProgressEventArgs> Started;

        public event EventHandler<ProgressControllerFinishedEventArgs> Finished;

        public event EventHandler<StepExecutionChangedEventArgs> StepExecutionChanged;

        public event EventHandler<CancellationSupportChangedEventArgs> CancellationSupportChanged;

        public IEnumerable<IProgressStep> Steps
        {
            get;
            set;
        }
        #endregion

        #region Simulation
        public void InvokeStarted()
        {
            if (this.Started != null)
            {
                this.Started(this, new ProgressEventArgs());
            }
        }

        public void InvokeFinished(ProgressControllerResult result)
        {
            if (this.Finished != null)
            {
                this.Finished(this, new ProgressControllerFinishedEventArgs(result));
            }
        }

        public void InvokeStepExecutionChanged(StepExecutionChangedEventArgs args)
        {
            if (this.StepExecutionChanged != null)
            {
                this.StepExecutionChanged(this, args);
            }
        }

        public void InvokeCancellationSupportChanged(bool cancellable)
        {
            if (this.CancellationSupportChanged != null)
            {
                this.CancellationSupportChanged(this, new CancellationSupportChangedEventArgs(cancellable));
            }
        }
        #endregion

        #region Verification
        public void AssertAllEventsAreRegistered()
        {
            Assert.IsNotNull(this.Started, "The Started event isn't registered");
            Assert.IsNotNull(this.Finished, "The Finished event wasn't registered");
            Assert.IsNotNull(this.StepExecutionChanged, "The StepExecutionChanged event isn't registered");
            Assert.IsNotNull(this.CancellationSupportChanged, "The CancellationSupportChanged event isn't registered");
        }

        public void AssertAllEventsAreUnregistered()
        {
            Assert.IsNull(this.Started, "The Started event is registered");
            Assert.IsNull(this.Finished, "The Finished event is registered");
            Assert.IsNull(this.StepExecutionChanged, "The StepExecutionChanged event is registered");
            Assert.IsNull(this.CancellationSupportChanged, "The CancellationSupportChanged event is registered");
        }
        #endregion
    }
}

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
using System.Threading.Tasks;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressController"/>
    /// </summary>
    public partial class ConfigurableProgressController : IProgressController
    {
        IProgressEvents IProgressController.Events
        {
            get { return this.Events; }
        }

        IErrorNotificationManager IProgressController.ErrorNotificationManager
        {
            get { return this.ErrorNotificationManager; }
        }

        Task<ProgressControllerResult> IProgressController.Start()
        {
            return Task.Factory.StartNew(() =>
                {
                    stepOperations.ForEach(op =>
                    {
                        try
                        {
                            this.canAbort = op.Step.Cancellable;
                            StepExecutionState state = op.Run(this.cts.Token, this).Result;
                            VerificationHelper.CheckState(op.Step, state);
                        }
                        catch (Exception ex)
                        {
                            FluentAssertions.Execution.Execute.Assertion.FailWith(ex.ToString());
                        }
                    });
                    return this.ReturnResult;
                });
        }

        void IProgressController.Initialize(IProgressStepFactory stepFactory, IEnumerable<IProgressStepDefinition> stepsDefinition)
        {
            throw new NotImplementedException();
        }

        bool IProgressController.TryAbort()
        {
            if (this.TryAbortAction != null)
            {
                return this.TryAbortAction();
            }

            throw new NotImplementedException();
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            if (this.ServiceProvider != null)
            {
                return this.ServiceProvider.GetService(serviceType);
            }

            throw new NotImplementedException();
        }
    }
}
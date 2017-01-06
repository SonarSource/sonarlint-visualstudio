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
using System.Threading;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressStepOperation"/>
    /// </summary>
    public partial class ProgressControllerStep : IProgressStepOperation
    {
        IProgressStep IProgressStepOperation.Step
        {
            get
            {
                return this;
            }
        }

        async Task<StepExecutionState> IProgressStepOperation.Run(CancellationToken cancellationToken, IProgressStepExecutionEvents progressCallback)
        {
            if (this.ExecutionState != StepExecutionState.NotStarted)
            {
                throw new InvalidOperationException(ProgressResources.StepOperationWasAlreadyExecuted);
            }

            if (this.Cancellable && cancellationToken.IsCancellationRequested)
            {
                return this.ExecutionState = StepExecutionState.Cancelled;
            }

            VsTaskRunContext context = GetContext(this.Execution);

            StepExecutionState stepState = await VsThreadingHelper.RunTask<StepExecutionState>(this.controller, context,
                () =>
                {
                    DoStatefulExecution(progressCallback, cancellationToken);
                    return this.ExecutionState;
                },

                cancellationToken);

            return stepState;
        }
    }
}

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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Partial implementation of <see cref="IProgressStepOperation"/>
    /// </summary>
    public partial class ConfigurableProgressTestOperation : IProgressStepOperation
    {
        IProgressStep IProgressStepOperation.Step
        {
            get { return this; }
        }

        Task<StepExecutionState> IProgressStepOperation.Run(CancellationToken cancellationToken, IProgressStepExecutionEvents executionNotify)
        {
            cancellationToken.Should().NotBeNull("cancellationToken is not expected to be null");
            executionNotify.Should().NotBeNull("executionNotify is not expected to be null");
            return Task.Factory.StartNew(() =>
            {
                this.ExecutionState = StepExecutionState.Executing;
                this.operation(cancellationToken, executionNotify);
                this.executed = true;
                return this.ExecutionState = this.ExecutionResult;
            });
        }
    }
}
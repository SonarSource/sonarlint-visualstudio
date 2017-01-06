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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The interface represents the actual operation that needed to be executed in a <see cref="IProgressStep"/>
    /// <seealso cref="ProgressControllerStep"/>
    /// </summary>
    public interface IProgressStepOperation
    {
        /// <summary>
        /// The <see cref="IProgressStep"/> associated with this operation
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Step", Justification = "NA")]
        IProgressStep Step { get; }

        /// <summary>
        /// Execute the operation
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="progressCallback">Allows to update the <see cref="IProgressController"/> with the execution progress and cancellation support. <seealso cref="IProgressEvents"/></param>
        /// <returns>An awaitable task that returns a <see cref="StepExecutionState"/> result</returns>
        Task<StepExecutionState> Run(CancellationToken cancellationToken, IProgressStepExecutionEvents progressCallback);
    }
}

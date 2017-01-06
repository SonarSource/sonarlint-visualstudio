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
    /// Read-only information about a step which is executed by <see cref="IProgressController"/>
    /// <seealso cref="ProgressControllerStep"/>
    /// <seealso cref="IProgressStepExecutionEvents"/>
    /// <seealso cref="IProgressEvents"/>
    /// </summary>
    public interface IProgressStep
    {
        /// <summary>
        /// Execution state change event
        /// </summary>
        event EventHandler<StepExecutionChangedEventArgs> StateChanged;

        /// <summary>
        /// The display text for a step. Can be null.
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// The progress (0..1) during execution. Can change over time.
        /// <remarks>Can be double.NaN in case the step has no intra step progress reporting ability</remarks>
        /// </summary>
        double Progress { get; }

        /// <summary>
        /// The progress details text during executing. Can change over time.
        /// </summary>
        string ProgressDetailText { get; }

        /// <summary>
        /// The execution state of the step. Can change over time.
        /// </summary>
        StepExecutionState ExecutionState { get; }

        /// <summary>
        /// Whether the step is supposed to be visible or an internal detail which needs to be hidden
        /// </summary>
        bool Hidden { get; }

        /// <summary>
        /// Whether cancellable. Can change over time.
        /// </summary>
        bool Cancellable { get; }

        /// <summary>
        /// Whether the progress is indeterminate
        /// </summary>
        bool Indeterminate { get; }

        /// <summary>
        /// Whether impacts the progress calculations
        /// </summary>
        bool ImpactsProgress { get; }
    }
}

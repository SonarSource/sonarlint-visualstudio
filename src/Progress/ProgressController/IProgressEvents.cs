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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The interface is used for notification of <see cref="IProgressController"/> progress and state
    /// </summary>
    /// <remarks>
    /// Registering/Unregistering to the events needs to be done on UIThread.
    /// All the events will be raised on the UI thread</remarks>
    public interface IProgressEvents
    {
        /// <summary>
        /// <see cref="IProgressController"/> started to execute. Always raised if the <see cref="IProgressController"/> was started.
        /// </summary>
        event EventHandler<ProgressEventArgs> Started;

        /// <summary>
        /// <see cref="IProgressController"/> finished to execute. Always raised if the <see cref="IProgressController"/> was started.
        /// </summary>
        event EventHandler<ProgressControllerFinishedEventArgs> Finished;

        /// <summary>
        /// Changes in <see cref="IProgressController"/> execution of <see cref="IProgressStep"/>
        /// </summary>
        event EventHandler<StepExecutionChangedEventArgs> StepExecutionChanged;

        /// <summary>
        /// Changes in <see cref="IProgressController"/> cancellability
        /// </summary>
        event EventHandler<CancellationSupportChangedEventArgs> CancellationSupportChanged;

        /// <summary>
        /// The steps associated with the <see cref="IProgressController"/>.
        /// May be null until the <see cref="IProgressController"/> is initialized.
        /// The set of steps cannot be changed once initialized.
        /// </summary>
        IEnumerable<IProgressStep> Steps { get; }
    }
}

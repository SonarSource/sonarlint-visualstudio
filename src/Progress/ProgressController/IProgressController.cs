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

using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The interface represents a progress controller that can execute steps
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2302:FlagServiceProviders", Justification = "Service provider for future use and only forwarding by the current APIs")]
    public interface IProgressController : IServiceProvider
    {
        /// <summary>
        /// Returns <see cref="IProgressController"/> events which are exposed via <see cref="IProgressEvents"/>
        /// </summary>
        IProgressEvents Events { get; }

        /// <summary>
        /// Returns <see cref="IErrorNotificationManager"/> used by the controller
        /// </summary>
        IErrorNotificationManager ErrorNotificationManager { get; }

        /// <summary>
        /// Initializes a controller with fixed set of steps. The set of steps cannot be changed once it has been set.
        /// </summary>
        /// <param name="stepFactory">An instance of <see cref="IProgressStepFactory"/>. Required.</param>
        /// <param name="stepsDefinition">Set of <see cref="IProgressStepDefinition"/> to construct the steps from</param>
        void Initialize(IProgressStepFactory stepFactory, IEnumerable<IProgressStepDefinition> stepsDefinition);

        /// <summary>
        /// Starts execution
        /// </summary>
        /// <returns>An await-able object with a <see cref="ProgressControllerResult"/> result</returns>
        Task<ProgressControllerResult> Start();

        /// <summary>
        /// Tries to abort the current execution
        /// </summary>
        /// <returns>Whether was able to abort or not</returns>
        bool TryAbort();
    }
}

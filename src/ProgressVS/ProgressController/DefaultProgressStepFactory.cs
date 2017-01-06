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
using System.Globalization;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Default implementation of <see cref="IProgressStepFactory"/>
    /// </summary>
    /// <remarks>The class supports only <see cref="ProgressStepDefinition"/> definitions and <see cref="ProgressControllerStep"/> operation</remarks>
    public class DefaultProgressStepFactory : IProgressStepFactory
    {
        #region IProgressStepFactory
        /// <summary>
        /// Creates an operation from definition
        /// </summary>
        /// <param name="controller">Required <see cref="IProgressController"/></param>
        /// <param name="definition">Required and must derived from <see cref="ProgressStepDefinition"/></param>
        /// <returns><see cref="ProgressControllerStep"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "False positive")]
        public IProgressStepOperation CreateStepOperation(IProgressController controller, IProgressStepDefinition definition)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            ProgressStepDefinition supportedDefinition = definition as ProgressStepDefinition;
            if (supportedDefinition == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ProgressResources.UnsupportedTypeException, definition.GetType().FullName, typeof(ProgressStepDefinition).FullName));
            }

            ProgressControllerStep step = new ProgressControllerStep(controller, supportedDefinition);
            return step;
        }

        /// <summary>
        /// Returns a callback for <see cref="IProgressStepOperation"/>
        /// </summary>
        /// <param name="stepOperation">Required and must derived from <see cref="ProgressControllerStep"/></param>
        /// <returns>Returns a callback supporting instance</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "False positive")]
        public IProgressStepExecutionEvents GetExecutionCallback(IProgressStepOperation stepOperation)
        {
            ProgressControllerStep supportedStep = stepOperation as ProgressControllerStep;
            if (supportedStep == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ProgressResources.UnsupportedTypeException, stepOperation.GetType().FullName, typeof(ProgressControllerStep).FullName));
            }

            return supportedStep;
        }
        #endregion
    }
}

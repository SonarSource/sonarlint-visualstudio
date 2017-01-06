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
using System.Diagnostics;
using System.Threading;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Simple data class representing the step definition
    /// </summary>
    public class ProgressStepDefinition : IProgressStepDefinition
    {
        public ProgressStepDefinition(string displayText, StepAttributes attributes, Action<CancellationToken, IProgressStepExecutionEvents> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            this.DisplayText = displayText;
            this.Attributes = attributes;
            this.Operation = operation;
        }

        /// <summary>
        /// Display text describing the step.
        /// </summary>
        public string DisplayText { get; private set; }

        /// <summary>
        /// Operation to run when executing the step
        /// </summary>
        public Action<CancellationToken, IProgressStepExecutionEvents> Operation { get; private set; }

        /// <summary>
        /// Attributes of the step
        /// </summary>
        public StepAttributes Attributes { get; private set; }
    }
}

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
    /// A helper class that wraps the notifications for determinate step.
    /// </summary>
    public class DeterminateStepProgressNotifier
    {
        private readonly IProgressStepExecutionEvents executionEvents;
        private readonly int maxNumberOfIncrements;
        private int currentValue = 0;

        internal int CurrentValue
        {
            get
            {
                return currentValue;
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="DeterminateStepProgressNotifier"/>
        /// </summary>
        /// <param name="executionEvents">Required <see cref="IProgressStepExecutionEvents"/></param>
        /// <param name="numberOfIncrements">The number of predefined increments to the progress, at least one is expected.</param>
        public DeterminateStepProgressNotifier(IProgressStepExecutionEvents executionEvents, int numberOfIncrements)
        {
            if (executionEvents == null)
            {
                throw new ArgumentNullException(nameof(executionEvents));
            }

            if (numberOfIncrements < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfIncrements));
            }

            this.executionEvents = executionEvents;
            this.maxNumberOfIncrements = numberOfIncrements;
        }

        /// <summary>
        /// Notifies the progress without incrementing the progress.
        /// </summary>
        /// <remarks>
        /// <seealso cref="IProgressStepExecutionEvents.ProgressChanged(string, double)"/>
        /// </remarks>
        public void NotifyCurrentProgress(string message)
        {
            this.executionEvents.ProgressChanged(message, this.GetCurrentProgress());
        }

        /// <summary>
        /// Increments and notifies the progress with a message
        /// </summary>
        public void NotifyIncrementedProgress(string message, int increment = 1)
        {
            this.IncrementProgress(increment);
            this.NotifyCurrentProgress(message);
        }

        /// <summary>
        /// Advances the progress by an increment. The progress needs to remain in valid range for this to succeed.
        /// </summary>
        /// <param name="increment">1 by default</param>
        public void IncrementProgress(int increment = 1)
        {
            if (increment < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(increment));
            }

            if (this.currentValue + increment > this.maxNumberOfIncrements)
            {
                throw new ArgumentOutOfRangeException(nameof(increment), this.currentValue + increment, string.Format(CultureInfo.CurrentCulture, ControllerResources.InclusiveRangeExpectedExceptionMessage, 1, this.maxNumberOfIncrements));
            }

            this.currentValue += increment;
        }

        private double GetCurrentProgress()
        {
            if (this.currentValue == this.maxNumberOfIncrements)
            {
                return 1.0; // avoid rounding/floating point errors for the last step
            }

            return (double)this.currentValue / this.maxNumberOfIncrements;
        }
    }
}

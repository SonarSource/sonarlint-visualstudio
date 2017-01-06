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

using SonarLint.VisualStudio.Progress.MVVM;
using System;

namespace SonarLint.VisualStudio.Progress.Observation.ViewModels
{
    /// <summary>
    /// View model for the main/sub progress
    /// </summary>
    public class ProgressViewModel : ViewModelBase
    {
        private double progress;
        private bool indeterminate;

        /// <summary>
        /// Marginal error for values greater than 1.0 which we will round to 1.0 when calling <see cref="SetUpperBoundLimitedValue(double)"/>
        /// </summary>
        public const double UpperBoundMarginalErrorSupport = 0.00001;

        /// <summary>
        /// The current progress for the step
        /// </summary>
        /// <remarks>double.NaN indicates indeterminate progress</remarks>
        public double Value
        {
            get
            {
                return this.progress;
            }

            set
            {
                if (double.IsInfinity(value) || value < 0.0 || value > 1.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this.SetAndRaisePropertyChanged(ref this.progress, value);
            }
        }

        /// <summary>
        /// Whether the step is indeterminate
        /// </summary>
        public bool IsIndeterminate
        {
            get
            {
                return this.indeterminate;
            }

            set
            {
                this.SetAndRaisePropertyChanged(ref this.indeterminate, value);
            }
        }

        /// <summary>
        /// Will set the value whilst taking into account potential floating point errors when
        /// incrementing the value in a way that the sum is greater than 1.0 (within <see cref="UpperBoundMarginalErrorSupport"/>).
        /// Double.NaN values are also supported by this method (pass-through).
        /// </summary>
        public void SetUpperBoundLimitedValue(double value)
        {
            if (double.IsInfinity(value) || value < 0.0 || value > 1.0 + UpperBoundMarginalErrorSupport)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, string.Empty);
            }

            this.Value = Math.Min(1.0, value); // min should return NaN if value is NaN
        }
    }
}

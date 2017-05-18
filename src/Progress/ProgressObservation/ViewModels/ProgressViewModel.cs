/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using SonarLint.VisualStudio.Progress.MVVM;

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

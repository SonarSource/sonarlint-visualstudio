/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Adapter class to translate between <see cref="IProgress{T}" /> and the custom progress 
    /// IProgressStepExecutionEvents class
    /// </summary>
    /// <remarks>This class does effectively the same as SonarLint.VisualStudio.Progress.DeterminateStepProgressNotifier
    /// i.e. converts reporting of a fixed number of steps to a double between zero and one.</remarks>
    internal class FixedStepsProgressAdapter : IProgress<FixedStepsProgress>
    {
        private readonly IProgressStepExecutionEvents executionEvents;
        private int maxNumberOfIncrements;
        private int lastStep;

        public FixedStepsProgressAdapter(IProgressStepExecutionEvents executionEvents)
        {
            this.executionEvents = executionEvents ?? throw new ArgumentNullException(nameof(executionEvents));
        }

        private double GetCurrentProgress(int currentStep)
        {
            if (currentStep == this.maxNumberOfIncrements)
            {
                return 1.0; // avoid rounding/floating point errors for the last step
            }

            return (double)currentStep / this.maxNumberOfIncrements;
        }

        #region IProgress<FixedStepsProcess> methods

        void IProgress<FixedStepsProgress>.Report(FixedStepsProgress value)
        {
            if (maxNumberOfIncrements == 0)
            {
                // First call
                Debug.Assert(value.TotalSteps > 0);
                this.maxNumberOfIncrements = value.TotalSteps;
            }
            else
            {
                if (value.TotalSteps != maxNumberOfIncrements)
                {
                    // Can't change the total number of steps once it has been set
                    throw new ArgumentOutOfRangeException(nameof(value.TotalSteps));
                }
                if (value.CurrentStep < this.lastStep)
                {
                    // Can't reduce the current step number
                    throw new ArgumentOutOfRangeException(nameof(value.CurrentStep));
                }
            }

            this.lastStep = value.CurrentStep;
            executionEvents.ProgressChanged(value.Message, GetCurrentProgress(value.CurrentStep));
        }

        #endregion IProgress<FixedStepsProcess> methods
    }
}

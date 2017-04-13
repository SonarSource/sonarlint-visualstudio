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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Event arguments for a single <see cref="IProgressStep"/> being executed by the <see cref="IProgressController"/>
    /// <seealso cref="IProgressEvents"/>
    /// </summary>
    public class StepExecutionChangedEventArgs : ProgressEventArgs
    {
        public StepExecutionChangedEventArgs(IProgressStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            this.Step = step;
            this.State = step.ExecutionState;
            this.Progress = step.Progress;
            this.ProgressDetailText = step.ProgressDetailText;
        }

        /// <summary>
        /// Step execution state
        /// </summary>
        public StepExecutionState State
        {
            get;
            private set;
        }

        /// <summary>
        /// Progress text details. Can be null
        /// </summary>
        public string ProgressDetailText
        {
            get;
            private set;
        }

        /// <summary>
        /// Progress values between 0.0 and 1.0 or indeterminate. Use <see cref="IsProgressDeterminate"/> to decide
        /// </summary>
        public double Progress
        {
            get;
            private set;
        }

        /// <summary>
        /// The step being executed
        /// </summary>
        public IProgressStep Step
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns whether <see cref="Progress"/>  is indeterminate
        /// </summary>
        /// <returns>Whether the current arguments are for indeterminate progress</returns>
        public bool IsProgressIndeterminate()
        {
            return ProgressControllerHelper.IsIndeterminate(this.Progress);
        }
    }
}

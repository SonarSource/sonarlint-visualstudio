/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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

using SonarLint.VisualStudio.Progress.MVVM;
using SonarLint.VisualStudio.Progress.Controller;
using System;

namespace SonarLint.VisualStudio.Progress.Observation.ViewModels
{
    /// <summary>
    /// A view model data class that is used in <see cref="ProgressControllerViewModel"/>
    /// to represent a single visible step
    /// </summary>
    /// <seealso cref="ProgressObserver"/>
    /// <seealso cref="WPFWindowHost"/>
    public class ProgressStepViewModel : ViewModelBase
    {
        #region Fields
        private readonly ProgressViewModel progress = new ProgressViewModel();
        private StepExecutionState state;
        private string displayText;
        private string progressDetailText;

        #endregion

        #region Properties
        /// <summary>
        /// Display text for a step
        /// </summary>
        public string DisplayText
        {
            get
            {
                return this.displayText;
            }

            set
            {
                this.SetAndRaisePropertyChanged(ref this.displayText, value);
            }
        }

        /// <summary>
        /// Execution state for the step
        /// </summary>
        public StepExecutionState ExecutionState
        {
            get
            {
                return this.state;
            }

            set
            {
                this.SetAndRaisePropertyChanged(ref this.state, value);
            }
        }

        /// <summary>
        /// The current progress for the step
        /// </summary>
        public ProgressViewModel Progress
        {
            get
            {
                return this.progress;
            }
        }

        /// <summary>
        /// The current progress details text for the step
        /// </summary>
        public string ProgressDetailText
        {
            get
            {
                return this.progressDetailText;
            }

            set
            {
                this.SetAndRaisePropertyChanged(ref this.progressDetailText, value);
            }
        }

        #endregion
    }
}

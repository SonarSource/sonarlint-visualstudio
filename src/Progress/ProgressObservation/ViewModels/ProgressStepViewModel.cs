//-----------------------------------------------------------------------
// <copyright file="ProgressStepViewModel.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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

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
using SonarLint.VisualStudio.Progress.MVVM;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Progress.Observation.ViewModels
{
    /// <summary>
    /// A view model data class that is used in <see cref="ProgressObserverControl"/>
    /// supplied by <see cref="IProgressVisualizer"/>
    /// </summary>
    /// <seealso cref="ProgressObserver"/>
    /// <seealso cref="WPFWindowHost"/>
    public class ProgressControllerViewModel : ViewModelBase
    {
        #region Fields
        private readonly ObservableCollection<ProgressStepViewModel> viewModelSteps = new ObservableCollection<ProgressStepViewModel>();
        private readonly ProgressViewModel mainProgress = new ProgressViewModel() { IsIndeterminate = false, Value = 0 };
        private string title = null;
        private bool cancellable = true;
        private ProgressStepViewModel currentViewModelItem;
        private ICommand cancellationCommand = null;
        #endregion

        #region Properties
        /// <summary>
        /// The title for the view model. Can be null.
        /// </summary>
        public string Title
        {
            get
            {
                return this.title;
            }

            set
            {
                this.SetAndRaisePropertyChanged(ref this.title, value);
            }
        }

        /// <summary>
        /// The current <see cref="ProgressStepViewModel"/> which is being executed. Can be null.
        /// </summary>
        /// <exception cref="ArgumentException">When the <see cref="ProgressStepViewModel"/> is not in <see cref="Steps"/></exception>
        public ProgressStepViewModel Current
        {
            get
            {
                return this.currentViewModelItem;
            }

            set
            {
                if (value != null && !this.Steps.Contains(value))
                {
                    throw new ArgumentException(ProgressObserverCoreResources.NonObservedStepException);
                }

                this.SetAndRaisePropertyChanged(ref this.currentViewModelItem, value);
            }
        }

        /// <summary>
        /// Whether currently cancellable
        /// </summary>
        public bool Cancellable
        {
            get
            {
                return this.cancellable;
            }

            set
            {
                this.SetAndRaisePropertyChanged(ref this.cancellable, value);
            }
        }

        /// <summary>
        /// The main progress. Values between 0.0 and 1.0 are expected.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">When value is not in range [0.0, 1.0]</exception>
        public ProgressViewModel MainProgress
        {
            get
            {
                return this.mainProgress;
            }
        }
        #endregion

        #region Step VM items
        /// <summary>
        /// <see cref="ProgressStepViewModel"/> items observed by the current view model. Not null.
        /// </summary>
        public ObservableCollection<ProgressStepViewModel> Steps
        {
            get
            {
                return this.viewModelSteps;
            }
        }
        #endregion

        #region Commands
        /// <summary>
        /// Cancellation command. Can be null.
        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                return this.cancellationCommand;
            }

            set
            {
                this.SetAndRaisePropertyChanged(ref this.cancellationCommand, value);
            }
        }
        #endregion
    }
}

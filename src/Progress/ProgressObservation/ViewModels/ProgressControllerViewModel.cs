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

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

using SonarLint.VisualStudio.Progress.Observation;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SonarLint.VisualStudio.Integration.Progress
{
    /// <summary>
    /// Interaction logic for ProgressControl.xaml
    /// </summary>
    public partial class ProgressControl : Grid, IProgressVisualizer
    {
        public static readonly DependencyProperty ViewModelProperty =
                DependencyProperty.Register("ViewModel", typeof(ProgressControllerViewModel), typeof(ProgressControl));

        public ProgressControl()
        {
            InitializeComponent();
        }

        public bool Visible
        {
            get
            {
                return this.Visibility == Visibility.Visible;
            }
        }

        #region IProgressVisualizer
        /// <summary>
        /// The view model to which the control is bound to
        /// </summary>
        public ProgressControllerViewModel ViewModel
        {
            get { return (ProgressControllerViewModel)this.GetValue(ViewModelProperty); }
            set { this.SetValue(ViewModelProperty, value); }
        }

        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }

        public void Show()
        {
            this.Visibility = Visibility.Visible;
        }
        #endregion
    }
}

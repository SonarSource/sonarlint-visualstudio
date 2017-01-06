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

using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.Windows;

namespace SonarLint.VisualStudio.Progress.Observation.View
{
    /// <summary>
    /// Visualizes progress in a WPF window using a <see cref="ProgressVisualizerControl"/>
    /// </summary>
    public partial class WpfWindowProgressVisualizer : DialogWindow, IProgressVisualizer
    {
        #region Fields
        public static readonly DependencyProperty ViewModelProperty = ProgressVisualizerControl.ViewModelProperty.AddOwner(typeof(WpfWindowProgressVisualizer));
        private readonly bool modal;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a modal WPF dialog
        /// </summary>
        public WpfWindowProgressVisualizer()
            : this(true)
        {
        }

        /// <summary>
        /// Creates a WPF dialog
        /// </summary>
        /// <param name="modal">Specify whether the dialog will be modal</param>
        public WpfWindowProgressVisualizer(bool modal)
        {
            this.InitializeComponent();
            this.modal = modal;
            this.HasMaximizeButton = false;
            this.HasMinimizeButton = false;
        }
        #endregion

        #region Properties
        public ProgressControllerViewModel ViewModel
        {
            get { return (ProgressControllerViewModel)this.GetValue(ViewModelProperty); }
            set { this.SetValue(ViewModelProperty, value); }
        }
        #endregion

        #region IProgressObserverControlHost implementation
        void IProgressVisualizer.Show()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.modal)
            {
                this.ShowModal();
            }
            else
            {
                this.Show();
            }
        }

        void IProgressVisualizer.Hide()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.Hide();
        }
        #endregion
    }
}

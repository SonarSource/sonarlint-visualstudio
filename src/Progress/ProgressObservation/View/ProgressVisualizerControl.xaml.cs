/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA and Microsoft Corporation
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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;

namespace SonarLint.VisualStudio.Progress.Observation.View
{
    /// <summary>
    /// Interaction logic for ProgressVisualizerControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ProgressVisualizerControl : UserControl
    {
        #region Static
        public static readonly DependencyProperty ViewModelProperty =
                DependencyProperty.Register("ViewModel", typeof(ProgressControllerViewModel), typeof(ProgressVisualizerControl));

        public static readonly DependencyProperty HeaderStyleProperty =
                DependencyProperty.Register("HeaderStyle", typeof(Style), typeof(ProgressVisualizerControl));

        public static readonly DependencyProperty BorderStyleProperty =
                DependencyProperty.Register("BorderStyle", typeof(Style), typeof(ProgressVisualizerControl));

        #endregion

        #region Constructor
        public ProgressVisualizerControl()
        {
            this.InitializeComponent();
            this.HeaderStyle = this.TryFindResource("DefaultProgressHeaderStyle") as Style;
            this.BorderStyle = this.TryFindResource("DefaultProgressBorderStyle") as Style;
        }
        #endregion

        #region Properties
        public ProgressControllerViewModel ViewModel
        {
            get { return (ProgressControllerViewModel)this.GetValue(ViewModelProperty); }
            set { this.SetValue(ViewModelProperty, value); }
        }

        public Style HeaderStyle
        {
            get { return (Style)this.GetValue(HeaderStyleProperty); }
            set { this.SetValue(HeaderStyleProperty, value); }
        }

        public Style BorderStyle
        {
            get { return (Style)this.GetValue(BorderStyleProperty); }
            set { this.SetValue(BorderStyleProperty, value); }
        }
        #endregion
    }
}

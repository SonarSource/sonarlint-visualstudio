//-----------------------------------------------------------------------
// <copyright file="ProgressVisualizerControl.xaml.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SonarLint.VisualStudio.Progress.Observation.View
{
    /// <summary>
    /// Interaction logic for ProgressVisualizerControl.xaml
    /// </summary>
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

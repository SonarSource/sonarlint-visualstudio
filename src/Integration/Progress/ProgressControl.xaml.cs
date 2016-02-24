//-----------------------------------------------------------------------
// <copyright file="ProgressControl.xaml.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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

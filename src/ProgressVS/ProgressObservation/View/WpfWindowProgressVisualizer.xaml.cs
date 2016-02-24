//-----------------------------------------------------------------------
// <copyright file="WpfWindowProgressVisualizer.xaml.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
        private bool modal;
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

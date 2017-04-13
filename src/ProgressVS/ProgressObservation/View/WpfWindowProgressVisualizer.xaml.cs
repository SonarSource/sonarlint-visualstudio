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

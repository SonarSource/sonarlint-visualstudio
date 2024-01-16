/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Windows;
using System.Windows.Markup;
using Microsoft.VisualStudio.PlatformUI;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Transition
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [ContentProperty(nameof(MuteWindowDialog))]
    public partial class MuteWindowDialog : DialogWindow
    {
        public MuteWindowDialog(bool showAccept)
        {
            InitializeComponent();

            SetVisibility(showAccept);
        }

        private void SetVisibility(bool showAccept)
        {
            BorderWontFix.Visibility = showAccept ? Visibility.Hidden : Visibility.Visible;
            BorderAccept.Visibility = showAccept ? Visibility.Visible : Visibility.Hidden;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public SonarQubeIssueTransition SelectedIssueTransition { get; private set; }

        public string Comment => txtComment.Text;

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            switch (sender)
            {
                case var value when value == rbWontFix:
                    SelectedIssueTransition = SonarQubeIssueTransition.WontFix;
                    break;

                case var value when value == rbAccept:
                    SelectedIssueTransition = SonarQubeIssueTransition.Accept;
                    break;

                case var value when value == rbFalsePositive:
                    SelectedIssueTransition = SonarQubeIssueTransition.FalsePositive;
                    break;
            }
        }
    }
}

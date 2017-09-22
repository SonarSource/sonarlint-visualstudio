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

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    public partial class NotificationIndicator : UserControl
    {
        public NotificationIndicator()
        {
            InitializeComponent();
        }

        private void Close()
        {
            PART_Button.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Close();

            e.Handled = true;

            var startInfo = new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
    }
}

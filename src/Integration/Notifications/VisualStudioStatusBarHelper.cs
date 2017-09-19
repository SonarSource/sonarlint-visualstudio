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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    public static class VisualStudioStatusBarHelper
    {
        private const string SccStatusBarHostName = "PART_SccStatusBarHost";
        private const string StatusBarPanelName = "StatusBarPanel";

        public static void RemoveStatusBarIcon(FrameworkElement statusBarIcon)
        {
            var dockPanel = statusBarIcon.Parent as DockPanel;
            if (dockPanel == null)
            {
                return;
            }

            dockPanel.Children.Remove(statusBarIcon);
        }

        public static void AddStatusBarIcon(FrameworkElement statusBarIcon)
        {
            var root = VisualTreeHelper.GetChild(
                VisualTreeHelper.GetChild(Application.Current.MainWindow, 0), 0);

            var dockPanel = GetStatusBarPanel(root);
            if (dockPanel == null)
            {
                return;
            }

            var index = GetStatusBarHostIndex(dockPanel);

            DockPanel.SetDock(statusBarIcon, Dock.Right);

            dockPanel.Children.Insert(Math.Max(1, index + 1), statusBarIcon);
        }

        private static DockPanel GetStatusBarPanel(DependencyObject mainWindow)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(mainWindow); i++)
            {
                var dockPanel = VisualTreeHelper.GetChild(mainWindow, i) as DockPanel;
                if (dockPanel?.Name == StatusBarPanelName)
                {
                    return dockPanel;
                }
            }
            return null;
        }

        private static int GetStatusBarHostIndex(DockPanel dockPanel)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dockPanel); i++)
            {
                var content = VisualTreeHelper.GetChild(dockPanel, i) as ContentControl;
                if (content?.Name == SccStatusBarHostName)
                {
                    return i;
                }
            }
            return 0;
        }
    }
}

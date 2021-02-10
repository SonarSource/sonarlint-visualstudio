/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    [ExcludeFromCodeCoverage] // Not really unit-testable
    public static class VisualStudioStatusBarHelper
    {
        private const string SccStatusBarHostName = "PART_SccStatusBarHost";

        public static void RemoveStatusBarIcon(FrameworkElement statusBarIcon)
        {
            var dockPanel = statusBarIcon.Parent as DockPanel;
            dockPanel?.Children.Remove(statusBarIcon);
        }

        public static void AddStatusBarIcon(FrameworkElement statusBarIcon)
        {
            if (statusBarIcon.Parent != null)
            {
                return;
            }

            var dockPanel = FindSccStatusBarParentPanel(Application.Current.MainWindow);
            if (dockPanel == null)
            {
                return;
            }

            DockPanel.SetDock(statusBarIcon, Dock.Right);

            var sourceControlHostIndex = GetStatusBarSourceControlHostIndex(dockPanel);
            dockPanel.Children.Insert(sourceControlHostIndex + 1, statusBarIcon);
        }

        private static DockPanel FindSccStatusBarParentPanel(Window window)
        {
            // We want to add our icon as in the VS status bar as a sibling of the source control visuals.

            // Assumptions:
            // 1) there is a control named "PART_SccStatusBarHost"
            // 2) that control is hosted in a dock panel 

            // This approach is a bit ugly, but it's also the approach used in the extension -
            // see https://github.com/github/VisualStudio/blob/824bab4ab2c3d8f6787cf38ff5ff9d4e9df00e98/src/GitHub.InlineReviews/Services/PullRequestStatusBarManager.cs#L171

            // Note: the visual tree of VS2019 is different from that of VS2015 and VS2017, but they
            // all have contain this named PART control. See #1751.

            var statusBarPart = window?.Template?.FindName(SccStatusBarHostName,  window) as FrameworkElement;
            return statusBarPart?.Parent as DockPanel;
        }

        private static int GetStatusBarSourceControlHostIndex(DockPanel dockPanel)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dockPanel); i++)
            {
                var content = VisualTreeHelper.GetChild(dockPanel, i) as ContentControl;
                if (content?.Name == SccStatusBarHostName)
                {
                    return i;
                }
            }

            Debug.Fail("Not expecting GetStatusBarSourceControlHostIndex to be called unless the expected element exists");
            return -1;
        }
    }
}

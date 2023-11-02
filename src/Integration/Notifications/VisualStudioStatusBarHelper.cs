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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    [ExcludeFromCodeCoverage] // Not really unit-testable
    public static class VisualStudioStatusBarHelper
    {
        private const string SccStatusBarHostName = "PART_SccStatusBarHost";
        private const string StatusBarRightFrameControlContainerName = "PART_StatusBarRightFrameControlContainer";

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

            DockPanel.SetDock(statusBarIcon, Dock.Right);

            AddStatusBarIcon(Application.Current.MainWindow, statusBarIcon);
        }

        /// <summary>
        /// Adds our status bar icon as a sibling of the source control visuals.
        /// </summary>
        /// <remarks>
        /// We want to add our icon as in the VS status bar as a sibling of the source control visuals.
        /// Assumptions:
        /// 1) there is a control named "PART_SccStatusBarHost"
        /// 2) that control is hosted in a dock panel
        /// This approach is a bit ugly, but it's also the approach used in the extension -see https://github.com/github/VisualStudio/blob/824bab4ab2c3d8f6787cf38ff5ff9d4e9df00e98/src/GitHub.InlineReviews/Services/PullRequestStatusBarManager.cs#L171
        /// </remarks>
        private static void AddStatusBarIcon(Window window, UIElement statusBarIcon)
        {
            if(!GetOldStatusBarPlacement(SccStatusBarHostName, window, out var statusBarPart, out var parent)
               && !GetOldStatusBarPlacement(StatusBarRightFrameControlContainerName, window, out statusBarPart, out parent))
            {
                Debug.Fail("Could not find status bar container");
                return;
            }

            var index = parent.Children.IndexOf(statusBarPart);
            parent.Children.Insert(index + 1, statusBarIcon);
        }

        private static bool GetOldStatusBarPlacement(string statusBarElementToAttachAfter, Window window, out FrameworkElement statusBarElement, out DockPanel parent)
        {
            statusBarElement = window?.Template?.FindName(statusBarElementToAttachAfter, window) as FrameworkElement;
            parent = statusBarElement?.Parent as DockPanel;
            return parent != null;
        }
    }
}

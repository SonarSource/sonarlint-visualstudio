/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using static SonarLint.VisualStudio.ConnectedMode.UI.WindowExtensions;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList
{
    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    internal sealed partial class HotspotsControl : UserControl
    {
        public IHotspotsControlViewModel ViewModel { get; }

        public HotspotsControl(IHotspotsControlViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();
        }

        private void ReviewHotspotMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO by https://sonarsource.atlassian.net/browse/SLVS-2140 and https://sonarsource.atlassian.net/browse/SLVS-2142: fill the current status and allowed statuses
            var dialog = new ReviewHotspotWindow(HotspotStatus.Acknowledge, [HotspotStatus.TO_REVIEW, HotspotStatus.FIXED, HotspotStatus.ACKNOWLEDGED, HotspotStatus.SAFE]);
            dialog.ShowDialog(Application.Current.MainWindow);
        }
    }
}

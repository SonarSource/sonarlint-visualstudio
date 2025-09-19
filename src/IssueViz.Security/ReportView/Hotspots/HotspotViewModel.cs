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

using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;

internal class HotspotViewModel : ViewModelBase, IAnalysisIssueViewModel
{
    public LocalHotspot LocalHotspot { get; }

    public HotspotViewModel(LocalHotspot localHotspot)
    {
        LocalHotspot = localHotspot;
        RuleInfo = new RuleInfoViewModel(localHotspot.Visualization.RuleId, localHotspot.Visualization.IssueId);
    }

    public int? Line => LocalHotspot.Visualization.Issue.PrimaryLocation.TextRange.StartLine;
    public int? Column => LocalHotspot.Visualization.Issue.PrimaryLocation.TextRange.StartLineOffset;
    public string Title => LocalHotspot.Visualization.Issue.PrimaryLocation.Message;
    public string FilePath => LocalHotspot.Visualization.Issue.PrimaryLocation.FilePath;
    public RuleInfoViewModel RuleInfo { get; }
    public IAnalysisIssueVisualization Issue => LocalHotspot.Visualization;
}

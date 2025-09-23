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

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal abstract class AnalysisIssueViewModelBase : ViewModelBase, IAnalysisIssueViewModel
{
    protected AnalysisIssueViewModelBase(IAnalysisIssueVisualization analysisIssueVisualization)
    {
        Issue = analysisIssueVisualization;
        RuleInfo = new RuleInfoViewModel(Issue.RuleId, Issue.IssueId);
    }

    public int? Line => Issue.Issue.PrimaryLocation.TextRange.StartLine;
    public int? Column => Issue.Issue.PrimaryLocation.TextRange.StartLineOffset;
    public string Title => Issue.Issue.PrimaryLocation.Message;
    public string FilePath => Issue.Issue.PrimaryLocation.FilePath;
    public RuleInfoViewModel RuleInfo { get; }
    public abstract DisplaySeverity DisplaySeverity { get; }
    public abstract IssueType IssueType { get; }
    public IAnalysisIssueVisualization Issue { get; }

    public bool IsSameAnalysisIssue(IAnalysisIssueVisualization analysisIssueVisualization) =>
        Issue.Issue.Id == analysisIssueVisualization.Issue.Id && Issue.Issue.IssueServerKey == analysisIssueVisualization.Issue.IssueServerKey;
}

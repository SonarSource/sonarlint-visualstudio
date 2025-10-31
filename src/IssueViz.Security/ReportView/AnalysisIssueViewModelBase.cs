﻿/*
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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

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
    public abstract DisplayStatus Status { get; }
    public IAnalysisIssueVisualization Issue { get; }

    public bool IsSameAnalysisIssue(IAnalysisIssueVisualization analysisIssueVisualization) =>
        Issue.Issue.Id == analysisIssueVisualization.Issue.Id && Issue.Issue.IssueServerKey == analysisIssueVisualization.Issue.IssueServerKey;

    protected static DisplaySeverity? GetDisplaySeverity(SoftwareQualitySeverity? softwareQualitySeverity)
    {
        if (!softwareQualitySeverity.HasValue)
        {
            return null;
        }

        return softwareQualitySeverity switch
        {
            SoftwareQualitySeverity.Info => DisplaySeverity.Info,
            SoftwareQualitySeverity.Low => DisplaySeverity.Low,
            SoftwareQualitySeverity.Medium => DisplaySeverity.Medium,
            SoftwareQualitySeverity.High => DisplaySeverity.High,
            SoftwareQualitySeverity.Blocker => DisplaySeverity.Blocker,
            _ => DisplaySeverity.Info
        };
    }

    protected static DisplaySeverity? GetDisplaySeverity(AnalysisIssueSeverity? severity)
    {
        if (!severity.HasValue)
        {
            return null;
        }

        return severity switch
        {
            AnalysisIssueSeverity.Info => DisplaySeverity.Info,
            AnalysisIssueSeverity.Minor => DisplaySeverity.Low,
            AnalysisIssueSeverity.Major => DisplaySeverity.Medium,
            AnalysisIssueSeverity.Critical => DisplaySeverity.High,
            AnalysisIssueSeverity.Blocker => DisplaySeverity.Blocker,
            _ => DisplaySeverity.Info
        };
    }
}

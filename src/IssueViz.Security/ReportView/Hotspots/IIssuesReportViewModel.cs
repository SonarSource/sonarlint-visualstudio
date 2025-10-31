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

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;

internal interface IIssuesReportViewModel : IDisposable
{
    ObservableCollection<IGroupViewModel> GetIssuesGroupViewModels();
    event EventHandler<IssuesChangedEventArgs> IssuesChanged;
}

[Export(typeof(IIssuesReportViewModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method:ImportingConstructor]
internal sealed class IssuesReportViewModel(ILocalIssuesStore localIssuesStore, IThreadHandling threadHandling) : IssuesReportViewModelBase(localIssuesStore, threadHandling), IIssuesReportViewModel
{
    protected override IEnumerable<IIssueViewModel> GetIssueViewModels() => localIssuesStore.GetAll().Select(x => new IssueViewModel(x));

    public ObservableCollection<IGroupViewModel> GetIssuesGroupViewModels() => GetGroupViewModels();
}

internal class IssueViewModel : AnalysisIssueViewModelBase
{
    public IAnalysisIssue AnalysisIssue => Issue.Issue as IAnalysisIssue;

    public IssueViewModel(IAnalysisIssueVisualization analysisIssueVisualization) : base(analysisIssueVisualization)
    {
        DisplaySeverity = GetDisplaySeverity(AnalysisIssue.HighestImpact?.Severity) ?? GetDisplaySeverity(AnalysisIssue.Severity) ?? DisplaySeverity.Info;
        Status = analysisIssueVisualization.IsResolved ? DisplayStatus.Resolved : DisplayStatus.Open;
    }

    public override DisplaySeverity DisplaySeverity { get; }
    public override IssueType IssueType => IssueType.Issue;
    public override DisplayStatus Status { get; }
}

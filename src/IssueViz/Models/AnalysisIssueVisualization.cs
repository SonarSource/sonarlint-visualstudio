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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppressions;

namespace SonarLint.VisualStudio.IssueVisualization.Models;

public interface IAnalysisIssueVisualization : IAnalysisIssueLocationVisualization, IFilterableIssue
{

    IReadOnlyList<IAnalysisIssueFlowVisualization> Flows { get; }

    IAnalysisIssueBase Issue { get; }

    IReadOnlyList<IQuickFixApplication> QuickFixes { get; }

    bool IsResolved { get; }

    bool IsOnNewCode { get; }
}

internal class AnalysisIssueVisualization : IAnalysisIssueVisualization
{
    private static readonly SnapshotSpan EmptySpan = new();
    private string currentFilePath;
    private SnapshotSpan? span;

    public AnalysisIssueVisualization(
        IReadOnlyList<IAnalysisIssueFlowVisualization> flows,
        IAnalysisIssueBase issue,
        SnapshotSpan? span,
        IReadOnlyList<IQuickFixApplication> quickFixes)
    {
        Flows = flows;
        Issue = issue;
        CurrentFilePath = issue.PrimaryLocation.FilePath;
        Span = span;
        QuickFixes = quickFixes;
        SonarRuleId = SonarCompositeRuleId.TryParse(issue.RuleKey, out var id) ? id : throw new ArgumentException(nameof(issue.RuleKey));
    }

    public SonarCompositeRuleId SonarRuleId { get; }
    public IReadOnlyList<IAnalysisIssueFlowVisualization> Flows { get; }
    public IReadOnlyList<IQuickFixApplication> QuickFixes { get; }
    public IAnalysisIssueBase Issue { get; }
    public int StepNumber => 0;
    public IAnalysisIssueLocation Location => Issue.PrimaryLocation;

    public SnapshotSpan? Span
    {
        get => span;
        set
        {
            span = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsResolved => Issue.IsResolved;
    public bool IsOnNewCode => Issue.IsOnNewCode;

    public string CurrentFilePath
    {
        get => currentFilePath;
        set
        {
            currentFilePath = value;

            if (string.IsNullOrEmpty(currentFilePath))
            {
                Span = EmptySpan;
            }

            NotifyPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public Guid? IssueId => Issue.Id;
    string IFilterableIssue.RuleId => Issue.RuleKey;

    string IFilterableIssue.FilePath => CurrentFilePath;

    string IFilterableIssue.LineHash => Issue.PrimaryLocation.TextRange.LineHash;

    int? IFilterableIssue.StartLine => Issue.PrimaryLocation.TextRange.StartLine;

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public static class AnalysisIssueVisualizationExtensions
{
    /// <summary>
    ///     Returns primary and all secondary locations of the given <see cref="issueVisualization" />
    /// </summary>
    public static IEnumerable<IAnalysisIssueLocationVisualization> GetAllLocations(this IAnalysisIssueVisualization issueVisualization)
    {
        var primaryLocation = issueVisualization;
        var secondaryLocations = issueVisualization.GetSecondaryLocations();

        var allLocations = new List<IAnalysisIssueLocationVisualization> { primaryLocation };
        allLocations.AddRange(secondaryLocations);

        return allLocations;
    }

    /// <summary>
    ///     Returns all secondary locations of the given <see cref="issueVisualization" />
    /// </summary>
    public static IEnumerable<IAnalysisIssueLocationVisualization> GetSecondaryLocations(this IAnalysisIssueVisualization issueVisualization)
    {
        var secondaryLocations = issueVisualization.Flows.SelectMany(x => x.Locations);

        return secondaryLocations;
    }

    public static bool IsFileLevel(this IAnalysisIssueVisualization issueVisualization) => issueVisualization.Issue.IsFileLevel();
}

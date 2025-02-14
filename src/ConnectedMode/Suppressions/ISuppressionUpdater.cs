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

using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions;

/// <summary>
/// Fetches suppressed issues from the server and raises events. This is mainly needed for Roslyn languages
/// </summary>
internal interface ISuppressionUpdater
{
    /// <summary>
    /// Fetches all available suppressions from the server and raises the <see cref="SuppressedIssuesReloaded"/> event.
    /// </summary>
    Task UpdateAllServerSuppressionsAsync();

    /// <summary>
    /// For resolved issues, the <see cref="NewIssuesSuppressed"/> event is invoked.
    /// For unresolved issues (i.e. issues that are re-opened), the <see cref="SuppressionsRemoved"/> event is invoked.
    /// </summary>
    Task UpdateSuppressedIssuesAsync(bool isResolved, string[] issueKeys, CancellationToken cancellationToken);

    event EventHandler<SuppressionsEventArgs> SuppressedIssuesReloaded;
    event EventHandler<SuppressionsEventArgs> NewIssuesSuppressed;
    event EventHandler<SuppressionsRemovedEventArgs> SuppressionsRemoved;
}

public class SuppressionsEventArgs(IReadOnlyList<SonarQubeIssue> suppressedIssues) : EventArgs
{
    public IReadOnlyList<SonarQubeIssue> SuppressedIssues { get; } = suppressedIssues;
}

public class SuppressionsRemovedEventArgs(IReadOnlyList<string> issueServerKeys) : EventArgs
{
    public IReadOnlyList<string> IssueServerKeys { get; } = issueServerKeys;
}

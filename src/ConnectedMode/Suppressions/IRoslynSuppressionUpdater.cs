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

using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions;

/// <summary>
/// Fetches suppressed issues from the server and updates the store
/// </summary>
internal interface IRoslynSuppressionUpdater
{
    /// <summary>
    /// Fetches all available suppressions from the server and updates the server issues store
    /// </summary>
    Task UpdateAllServerSuppressionsAsync();

    /// <summary>
    /// Updates the suppression status of the given issue key(s). If the issues are not found locally, they are fetched.
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

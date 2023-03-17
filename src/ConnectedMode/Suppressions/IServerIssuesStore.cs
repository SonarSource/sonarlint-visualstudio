﻿/*
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

using System;
using System.Collections.Generic;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    /// <summary>
    /// Stores all server issues known to the client for the current branch i.e. that have been fetched from the server.
    /// </summary>
    /// <returns>There may be other server-side issues that have not been fetched</returns>
    public interface IServerIssuesStore
    {
        /// <summary>
        /// Raised when the contents of the store have changed
        /// </summary>
        /// <remarks>The event is raised for any type of change: issue added, issue removed, issue state changed</remarks>
        event EventHandler ServerIssuesChanged;

        /// <summary>
        /// Returns all issues in the store
        /// </summary>
        IEnumerable<SonarQubeIssue> Get();
    }

    /// <summary>
    /// Write-interface for <see cref="IServerIssuesStore"/> i.e. contains methods to update the store
    /// </summary>
    internal interface IServerIssuesStoreWriter : IServerIssuesStore
    {
        /// <summary>
        /// Updates the IsResolved status of the issue with the specified key 
        /// </summary>
        /// <remarks>If the issue key cannot be matched to an issue in the store it will be ignored.
        /// Design decision: if we can't find an issue we could have decided to go to the server and
        /// fetch it, but there could be dozens or even hundreds of IDEs listening to the server
        /// events, so we don't want to trigger hundreds of requests to the server.
        /// </remarks>
        void UpdateIssue(string issueKey, bool isResolved);

        /// <summary>
        /// Adds the specified issues to the repository
        /// </summary>
        /// <param name="issues">The issues to add to the store. Can be empty.</param>
        /// <param name="clearAllExistingIssues">True if the store should be emptied before adding the new issues.
        /// If false, existing issues with matching keys will be updated.</param>
        /// <remarks>Passing an empty list to <paramref name="issues"/> and false to <paramref name="clearAllExistingIssues"/>
        /// has the effect of clearing the store in a single atomic operation.</remarks>
        void AddIssues(IEnumerable<SonarQubeIssue> issues, bool clearAllExistingIssues);
    }
}

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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    [Export(typeof(IServerIssuesStore))]
    [Export(typeof(IServerIssuesStoreWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ServerIssuesStore : IServerIssuesStore, IServerIssuesStoreWriter
    {
        private List<SonarQubeIssue> serverIssues = new List<SonarQubeIssue>();

        public event EventHandler ServerIssuesChanged;

        #region IServerIssuesStore implementation

        public IEnumerable<SonarQubeIssue> Get()
        {
            return serverIssues ?? Enumerable.Empty<SonarQubeIssue>();
        }

        #endregion IServerIssuesStore implementation

        #region IServerIssueStoreWriter implementation

        public void AddIssues(IEnumerable<SonarQubeIssue> issues, bool clearAllExistingIssues)
        {
            if (issues == null) { return; }

            if (clearAllExistingIssues)
            {
                serverIssues = new List<SonarQubeIssue>();
            }

            serverIssues.AddRange(issues);

            ServerIssuesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateIssue(string issueKey, bool isResolved)
        {
            var issue = serverIssues.SingleOrDefault(x => x.IssueKey == issueKey);

            if (issue == null)
            {
                return;
            }

            issue.IsResolved = isResolved;

            ServerIssuesChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion IServerIssueStoreWriter implementation
    }
}

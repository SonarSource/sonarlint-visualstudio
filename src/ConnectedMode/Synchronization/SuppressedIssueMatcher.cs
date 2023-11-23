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
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Synchronization
{
    public interface IIssueMatcher
    {
        SonarQubeIssue Match(IFilterableIssue issue);
    }   

    [Export(typeof(IIssueMatcher))]
    public class SuppressedIssueMatcher : IIssueMatcher
    {
        private readonly IServerIssuesStore serverIssuesStore;

        [ImportingConstructor]
        public SuppressedIssueMatcher(IServerIssuesStore serverIssuesStore)
        {
            this.serverIssuesStore = serverIssuesStore;
        }

        public SonarQubeIssue Match(IFilterableIssue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            // File-level issues (i.e. line = null) match if:
            // 1. Same component, same file, same error code.

            // Non-file-level issues match if:
            // 1. Same component, same file, same error code, same line hash        // tolerant to line number changing
            // 2. Same component, same file, same error code, same line             // tolerant to code on the line changing e.g. var rename

            // File-level issues never match non-file-level issues.

            var serverIssues = serverIssuesStore.Get();

            // Try to find an issue with the same ID and either the same line number or some line hash
            return serverIssues.FirstOrDefault(s => IsMatch(issue, s));
        }

        private static bool IsMatch(IFilterableIssue issue, SonarQubeIssue serverIssue)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(issue.RuleId, serverIssue.RuleId))
            {
                return false;
            }

            if (!PathHelper.IsServerFileMatch(issue.FilePath, serverIssue.FilePath))
            {
                return false;
            }

            if (!issue.StartLine.HasValue) // i.e. file-level issue
            {
                return serverIssue.TextRange == null;
            }

            // Non-file level issue
            return issue.StartLine == serverIssue.TextRange?.StartLine || StringComparer.Ordinal.Equals(issue.LineHash, serverIssue.Hash);
        }
    }
}

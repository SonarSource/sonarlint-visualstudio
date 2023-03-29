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
using System.Diagnostics;
using System.IO;
using System.Linq;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    internal interface ISuppressedIssueMatcher
    {
        bool SuppressionExists(IFilterableIssue issue);
    }

    public class SuppressedIssueMatcher : ISuppressedIssueMatcher
    {
        private readonly IServerIssuesStore serverIssuesStore;

        public SuppressedIssueMatcher(IServerIssuesStore serverIssuesStore)
        {
            this.serverIssuesStore = serverIssuesStore ?? throw new ArgumentNullException(nameof(serverIssuesStore));
        }

        public bool SuppressionExists(IFilterableIssue issue)
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
            bool isSuppressed = serverIssues.Any(s => s.IsResolved && IsMatch(issue, s));

            return isSuppressed;
        }

        private static bool IsMatch(IFilterableIssue issue, SonarQubeIssue serverIssue)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(issue.RuleId, serverIssue.RuleId))
            {
                return false;
            }

            if (!IsFileMatch(issue, serverIssue))
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

        private static bool IsFileMatch(IFilterableIssue issue, SonarQubeIssue serverIssue)
        {
            var localPath = issue.FilePath ?? string.Empty;
            var serverPath = serverIssue.FilePath ?? string.Empty;

            // A null/empty path means it's a module (project) level issue, and can only match
            // another module-level issue.
            if (localPath == string.Empty || serverPath == string.Empty)
            {
                return localPath == serverPath;
            }

            Debug.Assert(Path.IsPathRooted(localPath) && !localPath.Contains("/"),
                $"Expecting the client-side file path to be an absolute path with only back-slashes delimiters but got '{issue.FilePath}'.");

            // NB all server file paths should have been normalized. See SonarQube.Client.Helpers.FilePathNormalizer.
            Debug.Assert(!Path.IsPathRooted(serverPath) && !serverPath.Contains("/"),
                $"Expecting the server-side file path to be relative and not to contain forward-slashes.");

            Debug.Assert(!serverPath.StartsWith("\\"), "Not expecting server file path to start with a back-slash");
            if(localPath.EndsWith(serverPath, StringComparison.OrdinalIgnoreCase))
            {
                // Check the preceding local path character is a backslash  - we want to make sure a server path
                // of `aaa\foo.txt` matches `c:\aaa\foo.txt` but not `c:`bbbaaa\foo.txt`
                return localPath.Length > serverPath.Length &&
                    localPath[localPath.Length - serverPath.Length - 1] == '\\';
            }

            return false;
        }
    }
}

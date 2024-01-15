﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode
{
    internal interface IIssueMatcher
    {
        /// <summary>
        /// This method attempts to match <paramref name="issue"/> with <paramref name="serverIssue"/>.
        /// </summary>
        /// <remarks>There's a possibility of False Positive matches: in case false tail-match of file paths, as the project root is not taken into account,
        /// or when line number matches but line hash doesn't.
        /// </remarks>
        /// <param name="issue">Local issue</param>
        /// <param name="serverIssue">Server issue</param>
        /// <returns>The best possible match based on rule id, file name (tail matched to the server path), line number and line hash (not checked when line numbers match)</returns>
        bool IsLikelyMatch(IFilterableIssue issue, SonarQubeIssue serverIssue);

        /// <summary>
        /// Returns the first likely matching issue. 
        /// </summary>
        /// <remarks>For this method to work correctly, all <paramref name="serverIssuesFromSameFile"/>; need to be from the same server file.
        /// False Positives are possible, since &lt;see cref="IsLikelyMatch"/&gt; can return true for multiple issues in the same file and only the firs one is returned.
        /// </remarks>
        /// <param name="issue">Local issue</param>
        /// <param name="serverIssuesFromSameFile">List of server issues from the same file</param>
        /// <returns>A matching server issue, if present in the <paramref name="serverIssuesFromSameFile"/> list, or null</returns>
        SonarQubeIssue GetFirstLikelyMatchFromSameFileOrNull(IFilterableIssue issue, IEnumerable<SonarQubeIssue> serverIssuesFromSameFile);
    }

    [Export(typeof(IIssueMatcher))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class IssueMatcher : IIssueMatcher
    {
        public bool IsLikelyMatch(IFilterableIssue issue, SonarQubeIssue serverIssue)
        {
            return IsMatch(issue, serverIssue, true);
        }

        public SonarQubeIssue GetFirstLikelyMatchFromSameFileOrNull(IFilterableIssue issue, IEnumerable<SonarQubeIssue> serverIssuesFromSameFile)
        {
            return serverIssuesFromSameFile?.FirstOrDefault(serverIssue => IsMatch(issue, serverIssue, false));
        }

        private static bool IsMatch(IFilterableIssue issue, SonarQubeIssue serverIssue, bool checkFilePath)
        {
            // File-level issues (i.e. line = null) match if:
            // 1. Same component, same file, same error code.

            // Non-file-level issues match if:
            // 1. Same component, same file, same error code, same line hash        // tolerant to line number changing
            // 2. Same component, same file, same error code, same line             // tolerant to code on the line changing e.g. var rename

            // File-level issues never match non-file-level issues.
            
            if (!StringComparer.OrdinalIgnoreCase.Equals(issue.RuleId, serverIssue.RuleId))
            {
                return false;
            }

            if (checkFilePath && !PathHelper.IsServerFileMatch(issue.FilePath, serverIssue.FilePath))
            {
                return false;
            }

            // file level issue
            if (IsFileLevelServerIssue(serverIssue))
            {
                return CheckLocalIssueIsFileLevel(issue);
            }

            // Non-file level issue

            return issue.StartLine == serverIssue.TextRange?.StartLine || CompareHash(issue, serverIssue);
        }

        private static bool CheckLocalIssueIsFileLevel(IFilterableIssue issue)
        {
            return !issue.StartLine.HasValue
                   || (issue is IFilterableRoslynIssue roslynIssue // We don't know the end of the issue location, so this is our best guess.
                       && roslynIssue.RoslynStartLine == 1
                       && roslynIssue.RoslynStartColumn == 1); // This check relies on the fact that a roslyn file-level issue
            // always starts at the beginning of the file
            // and the fact that the rule can't be both file-level and not.
            // See SuppressionChecker.IsSameLine for an example of a solution w/o false-positives
        }

        private static bool IsFileLevelServerIssue(SonarQubeIssue serverIssue)
        {
            return serverIssue.TextRange == null;
        }

        private static bool CompareHash(IFilterableIssue issue, SonarQubeIssue serverIssue) =>
            issue.LineHash != null 
            && serverIssue.Hash != null
            && StringComparer.Ordinal.Equals(issue.LineHash, serverIssue.Hash);
    }
}

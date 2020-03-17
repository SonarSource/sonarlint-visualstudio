/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Linq;
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Integration.Suppression;

namespace SonarLint.VisualStudio.Integration.Vsix.Suppression
{
    internal class RoslynSuppressionHandler : IRoslynSuppressionHandler
    {
        private readonly IRoslynLiveIssueFactory liveIssueFactory;
        private readonly ISonarQubeIssuesProvider serverIssuesProvider;

        public RoslynSuppressionHandler(IRoslynLiveIssueFactory liveIssueFactory, ISonarQubeIssuesProvider serverIssuesProvider)
        {
            if (liveIssueFactory == null)
            {
                throw new ArgumentNullException(nameof(liveIssueFactory));
            }
            if (serverIssuesProvider == null)
            {
                throw new ArgumentNullException(nameof(serverIssuesProvider));
            }

            this.liveIssueFactory = liveIssueFactory;
            this.serverIssuesProvider = serverIssuesProvider;
        }

        public bool ShouldIssueBeReported(SyntaxTree syntaxTree, Diagnostic diagnostic)
        {
            // This method is called for every analyzer issue that is raised so it should be fast.
            if (!diagnostic.Location.IsInSource &&
                diagnostic.Location != Location.None)
            {
                return true;
            }

            LiveIssue liveIssue = liveIssueFactory.Create(syntaxTree, diagnostic);
            if (liveIssue == null)
            {
                return true; // Unable to get the data required to map a Roslyn issue to a SonarQube issue
            }

            // Issues match if:
            // 1. Same component, same file, same error code, same line hash        // tolerant to line number changing
            // 2. Same component, same file, same error code, same line             // tolerant to code on the line changing e.g. var rename

            // Retrieve all issues relating to this file (file level or precise location) or project (for module level issues)
            var potentialMatchingIssues = serverIssuesProvider.GetSuppressedIssues(liveIssue.ProjectGuid, liveIssue.FilePath);

            // Try to find an issue with the same ID and either the same line number or some line hash
            bool matchFound = potentialMatchingIssues
                .Where(i => StringComparer.OrdinalIgnoreCase.Equals(liveIssue.Diagnostic.Id, i.RuleId))
                .Any(i => liveIssue.StartLine == i.Line || StringComparer.Ordinal.Equals(liveIssue.LineHash, i.Hash));

            return !matchFound;
        }
    }
}

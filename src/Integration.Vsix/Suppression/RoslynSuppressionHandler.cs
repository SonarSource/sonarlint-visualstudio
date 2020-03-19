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
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Core.Suppression;

namespace SonarLint.VisualStudio.Integration.Vsix.Suppression
{
    internal class RoslynSuppressionHandler : IRoslynSuppressionHandler
    {
        private readonly IRoslynLiveIssueFactory liveIssueFactory;
        private readonly ISuppressedIssueMatcher issueMatcher;

        public RoslynSuppressionHandler(IRoslynLiveIssueFactory liveIssueFactory, ISuppressedIssueMatcher issueMatcher)
        {

            this.liveIssueFactory = liveIssueFactory ?? throw new ArgumentNullException(nameof(liveIssueFactory));
            this.issueMatcher = issueMatcher ?? throw new ArgumentNullException(nameof(issueMatcher));
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

            var matchFound = !issueMatcher.SuppressionExists(liveIssue);
            return matchFound;
        }
    }
}

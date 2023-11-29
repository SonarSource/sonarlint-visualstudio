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
using SonarLint.VisualStudio.Core.Suppressions;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    public interface ISuppressedIssueMatcher
    {
        bool SuppressionExists(IFilterableIssue issue);
    }

    [Export(typeof(ISuppressedIssueMatcher))]
    internal class SuppressedIssueMatcher : ISuppressedIssueMatcher
    {
        private readonly IServerIssuesStore serverIssuesStore;
        private readonly IIssueMatcher issueMatcher;

        [ImportingConstructor]
        public SuppressedIssueMatcher(IServerIssuesStore serverIssuesStore,
            IIssueMatcher issueMatcher)
        {
            this.serverIssuesStore = serverIssuesStore;
            this.issueMatcher = issueMatcher;
        }

        public bool SuppressionExists(IFilterableIssue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            var serverIssues = serverIssuesStore.Get();

            // Try to find an issue with the same ID and either the same line number or same line hash
            return serverIssues.Any(s => s.IsResolved && issueMatcher.IsGoodMatch(issue, s));
        }
    }
}

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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Sonarlint;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Suppression;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal static class IssueToFilterableIssueConverter
    {
        public static IEnumerable<IFilterableIssue> Convert(IEnumerable<Issue> issues, ITextSnapshot textSnapshot)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            if (textSnapshot == null)
            {
                throw new ArgumentNullException(nameof(textSnapshot));
            }

            return issues.Select(x => CreateFilterableIssue(x, textSnapshot))
                .Where(i => i != null)
                .ToArray();
        }

        internal/* for testing */ static IFilterableIssue CreateFilterableIssue(Issue issue, ITextSnapshot textSnapshot)
        {
            // SonarLint issues line numbers are 1-based, spans lines are 0-based

            if (issue == null)
            {
                return null;
            }

            // A start line of zero means the issue is file-level i.e. not associated with a particular line
            if (issue.StartLine == 0)
            {
                return new DaemonIssueAdapter(issue, null, null);
            }

            if (issue.StartLine > textSnapshot.LineCount)
            {
                // Race condition: the line reported in the diagnostic is beyond the end of the file, so presumably
                // the file has been edited while the analysis was being executed
                return null;
            }

            var text = textSnapshot.GetLineFromLineNumber(issue.StartLine - 1).GetText();
            var lineHash = ChecksumCalculator.Calculate(text);
            return new DaemonIssueAdapter(issue, text, lineHash);
        }
    }
}

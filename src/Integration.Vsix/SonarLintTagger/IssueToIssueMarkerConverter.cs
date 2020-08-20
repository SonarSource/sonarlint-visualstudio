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
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal interface IIssueToIssueMarkerConverter
    {
        IssueMarker Convert(IAnalysisIssue issue, ITextSnapshot textSnapshot);
    }

    internal class IssueToIssueMarkerConverter :  IIssueToIssueMarkerConverter
    {
        private readonly IIssueSpanCalculator issueSpanCalculator;

        public IssueToIssueMarkerConverter()
            : this(new IssueSpanCalculator())
        {
        }

        internal /* for testing */ IssueToIssueMarkerConverter(IIssueSpanCalculator issueSpanCalculator)
        {
            this.issueSpanCalculator = issueSpanCalculator;
        }

        public IssueMarker Convert(IAnalysisIssue issue, ITextSnapshot textSnapshot)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            if (textSnapshot == null)
            {
                throw new ArgumentNullException(nameof(textSnapshot));
            }

            var span = issueSpanCalculator.CalculateSpan(issue, textSnapshot);

            return span == null ? null : new IssueMarker(issue, span.Value);
        }
    }
}

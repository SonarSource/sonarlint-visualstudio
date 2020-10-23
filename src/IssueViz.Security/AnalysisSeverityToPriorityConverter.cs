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
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Security
{
    internal interface IAnalysisSeverityToPriorityConverter
    {
        /// <summary>
        /// Converts <see cref="AnalysisIssueSeverity"/> into high/medium/low priority
        /// </summary>
        string Convert(AnalysisIssueSeverity severity);

        /// <summary>
        /// Converts high/medium/low priority into <see cref="AnalysisIssueSeverity"/>
        /// </summary>
        AnalysisIssueSeverity Convert(string priority);
    }

    internal class AnalysisSeverityToPriorityConverter : IAnalysisSeverityToPriorityConverter
    {
        private readonly IDictionary<string, AnalysisIssueSeverity> map = new Dictionary<string, AnalysisIssueSeverity>
        {
            {"high", AnalysisIssueSeverity.Blocker},
            {"medium", AnalysisIssueSeverity.Major},
            {"low", AnalysisIssueSeverity.Minor}
        };

        public string Convert(AnalysisIssueSeverity severity)
        {
            var priority = map
                .Where(x => x.Value.Equals(severity))
                .Select(x => x.Key)
                .FirstOrDefault();

            if (priority == null)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "No matching priority");
            }

            return priority;
        }

        public AnalysisIssueSeverity Convert(string priority)
        {
            if (priority == null)
            {
                throw new ArgumentNullException(nameof(priority));
            }

            priority = priority.ToLowerInvariant();

            if (!map.TryGetValue(priority, out var severity))
            {
                throw new ArgumentOutOfRangeException(nameof(priority), priority, "No matching severity");
            }

            return severity;
        }
    }
}

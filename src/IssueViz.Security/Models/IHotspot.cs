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
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Models
{
    internal interface IHotspot : IAnalysisIssueBase
    {
        HotspotPriority Priority { get; }
    }

    public enum HotspotPriority
    {
        High,
        Medium,
        Low
    }

    internal class Hotspot : IHotspot
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlow> EmptyFlows = Array.Empty<IAnalysisIssueFlow>();

        public Hotspot(string filePath,
            string message,
            int startLine,
            int endLine,
            int startLineOffset,
            int endLineOffset,
            string lineHash,
            string ruleKey,
            HotspotPriority priority,
            IReadOnlyList<IAnalysisIssueFlow> flows)
        {
            FilePath = filePath;
            Message = message;
            StartLine = startLine;
            EndLine = endLine;
            StartLineOffset = startLineOffset;
            EndLineOffset = endLineOffset;
            LineHash = lineHash;
            RuleKey = ruleKey;
            Priority = priority;
            Flows = flows ?? EmptyFlows;
        }

        public string FilePath { get; }
        public string Message { get; }
        public int StartLine { get; }
        public int EndLine { get; }
        public int StartLineOffset { get; }
        public int EndLineOffset { get; }
        public string LineHash { get; }
        public string RuleKey { get; }
        public IReadOnlyList<IAnalysisIssueFlow> Flows { get; }
        public HotspotPriority Priority { get; }
    }
}

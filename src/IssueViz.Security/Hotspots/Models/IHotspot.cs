﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models
{
    internal interface IHotspot : IAnalysisIssueBase
    {
        string HotspotKey { get; }

        IHotspotRule Rule { get; }

        /// <summary>
        /// File path as received from SQ server
        /// </summary>
        string ServerFilePath { get; }

        DateTimeOffset CreationTimestamp { get; }

        DateTimeOffset LastUpdateTimestamp { get; }
    }

    internal class Hotspot : IHotspot
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlow> EmptyFlows = Array.Empty<IAnalysisIssueFlow>();

        public Hotspot(string hotspotKey,
            string filePath,
            string serverFilePath,
            string message,
            int startLine,
            int endLine,
            int startLineOffset,
            int endLineOffset,
            string lineHash,
            IHotspotRule rule,
            DateTimeOffset createTimestamp,
            DateTimeOffset lastUpdateTimestamp,
            IReadOnlyList<IAnalysisIssueFlow> flows)
        {
            HotspotKey = hotspotKey;
            FilePath = filePath;
            ServerFilePath = serverFilePath;
            Message = message;
            StartLine = startLine;
            EndLine = endLine;
            StartLineOffset = startLineOffset;
            EndLineOffset = endLineOffset;
            LineHash = lineHash;
            Rule = rule;
            CreationTimestamp = createTimestamp;
            LastUpdateTimestamp = lastUpdateTimestamp;
            Flows = flows ?? EmptyFlows;
        }

        public string HotspotKey { get; }
        public string FilePath { get; }
        public string Message { get; }
        public int StartLine { get; }
        public int EndLine { get; }
        public int StartLineOffset { get; }
        public int EndLineOffset { get; }
        public string LineHash { get; }
        public string RuleKey => Rule.RuleKey;
        public IHotspotRule Rule { get; }
        public IReadOnlyList<IAnalysisIssueFlow> Flows { get; }
        public string ServerFilePath { get;  }
        public DateTimeOffset CreationTimestamp { get; }
        public DateTimeOffset LastUpdateTimestamp { get; }
    }
}

/*
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
    }

    internal class Hotspot : IHotspot
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlow> EmptyFlows = Array.Empty<IAnalysisIssueFlow>();

        public Hotspot(string hotspotKey,
            string serverFilePath,
            IAnalysisIssueLocation primaryLocation,
            IHotspotRule rule,
            IReadOnlyList<IAnalysisIssueFlow> flows,
            string context = null)
        {
            HotspotKey = hotspotKey;
            ServerFilePath = serverFilePath;
            PrimaryLocation = primaryLocation ?? throw new ArgumentNullException(nameof(primaryLocation));
            Rule = rule;
            Flows = flows ?? EmptyFlows;
            RuleDescriptionContextKey = context;
        }

        public string HotspotKey { get; }
        public string RuleKey => Rule.RuleKey;
        public IHotspotRule Rule { get; }
        public IReadOnlyList<IAnalysisIssueFlow> Flows { get; }
        public IAnalysisIssueLocation PrimaryLocation { get; }
        public string ServerFilePath { get; }
        public string RuleDescriptionContextKey { get; }
    }
}

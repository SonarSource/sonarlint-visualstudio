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
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    internal interface IHotspotMatcher
    {
        bool IsMatch(IAnalysisIssueVisualization localHotspotVisualization, SonarQubeHotspot serverHotspot);
    }

    [Export(typeof(IHotspotMatcher))]
    internal class HotspotMatcher : IHotspotMatcher
    {
        public bool IsMatch(IAnalysisIssueVisualization localHotspotVisualization, SonarQubeHotspot serverHotspot)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(localHotspotVisualization.RuleId, serverHotspot.Rule.RuleKey)
                || !PathHelper.IsServerFileMatch(localHotspotVisualization.FilePath, serverHotspot.FilePath))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(serverHotspot.LineHash)
                && StringComparer.Ordinal.Equals(localHotspotVisualization.LineHash, serverHotspot.LineHash))
            {
                return true;
            }

            return localHotspotVisualization.StartLine == serverHotspot.TextRange.StartLine 
                   || StringComparer.Ordinal.Equals(localHotspotVisualization.Issue.PrimaryLocation.Message, serverHotspot.Message);
        }
    }
}

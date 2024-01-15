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

namespace SonarQube.Client.Models
{
    public class SonarQubeHotspotSearch
    {
        public SonarQubeHotspotSearch(string hotspotKey,
            string componentKey,
            string filePath,
            string projectKey,
            string status,
            string resolution,
            IssueTextRange textRange,
            string ruleKey,
            string message,
            string vulnerabilityProbability,
            DateTimeOffset creationDate,
            DateTimeOffset updateDate)
        {
            this.HotspotKey = hotspotKey;
            this.ComponentKey = componentKey;
            this.FilePath = filePath;
            this.ProjectKey = projectKey;
            this.Status = status;
            this.Resolution = resolution;
            this.TextRange = textRange;
            this.RuleKey = ruleKey;
            this.Message = message;
            this.VulnerabilityProbability = vulnerabilityProbability;
            this.CreationDate = creationDate;
            this.UpdateDate = updateDate;
        }

        public string HotspotKey { get; }
        public string ComponentKey { get; }
        public string FilePath { get; }
        public string ProjectKey { get; }
        public string Status { get; }
        public string Resolution { get; }
        public IssueTextRange TextRange { get; }
        public string RuleKey { get; }
        public string Message { get; }
        public string VulnerabilityProbability { get; }
        public DateTimeOffset CreationDate { get; }
        public DateTimeOffset UpdateDate { get; }

        public SonarQubeHotspot ToSonarQubeHotspot()
        {
            return new SonarQubeHotspot(HotspotKey,
                Message,
                null,
                null,
                Status,
                null,
                ProjectKey,
                null,
                ComponentKey,
                FilePath,
                CreationDate,
                UpdateDate,
                new SonarQubeHotspotRule(RuleKey,
                    null,
                    null,
                    VulnerabilityProbability,
                    null,
                    null,
                    null),
                TextRange,
                Resolution);
        }
    }
}

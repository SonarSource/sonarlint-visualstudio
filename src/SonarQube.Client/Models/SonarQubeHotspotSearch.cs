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

namespace SonarQube.Client.Models
{
    public class SonarQubeHotspotSearch
    {
        public SonarQubeHotspotSearch(string hotspotKey, string componentKey, string filePath, string projectKey, string status, string resolution, IssueTextRange textRange, string ruleKey)
        {
            this.HotspotKey = hotspotKey;
            this.ComponentKey = componentKey;
            this.FilePath = filePath;
            this.ProjectKey = projectKey;
            this.Status = status;
            this.Resolution = resolution;
            this.TextRange = textRange;
            this.RuleKey = ruleKey;
        }

        public string HotspotKey { get; }
        public string ComponentKey { get; }
        public string FilePath { get; }
        public string ProjectKey { get; }
        public string Status { get; }
        public string Resolution { get; }
        public IssueTextRange TextRange { get; }
        public string RuleKey { get; }

        public SonarQubeHotspot ToSonarQubeHotspot()
        {
            return new SonarQubeHotspot(HotspotKey,
                null, // todo: this field exists in the server response and is needed, add in separate PR
                null,
                null, 
                Status,
                null, 
                ProjectKey,
                null,
                ComponentKey,
                FilePath,
                default, // todo: this field exists in the server response and is needed, add in separate PR
                default, // todo: this field exists in the server response and is needed, add in separate PR
                new SonarQubeHotspotRule(RuleKey, 
                    null,
                    null, 
                    null, 
                    null, 
                    null,
                    null),
                TextRange);
        }
    }
}

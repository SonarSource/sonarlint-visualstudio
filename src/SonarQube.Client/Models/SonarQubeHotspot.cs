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
    public class SonarQubeHotspot
    {
        public SonarQubeHotspot(
            string hotspotKey, string message, string lineHash, string assignee, string status,
            string organization, string projectKey, string projectName,
            string componentKey, string filePath,
            DateTimeOffset creationDate, DateTimeOffset updateDate,
            SonarQubeHotspotRule rule,
            IssueTextRange textRange,
            string resolution)
        {
            HotspotKey = hotspotKey;
            Message = message;
            LineHash = lineHash;
            Assignee = assignee;
            Status = status;
            Resolution = resolution;

            Organization = organization;
            ProjectKey = projectKey;
            ProjectName = projectName;

            ComponentKey = componentKey;
            FilePath = filePath;

            CreationTimestamp = creationDate;
            LastUpdateTimestamp = updateDate;

            Rule = rule;
            TextRange = textRange;
        }

        public string HotspotKey { get; }
        public string Message { get; }
        public string LineHash { get; }
        public string Assignee { get; }
        public string Status { get; }
        public IssueTextRange TextRange { get; }
        public string Organization { get; }
        public string ComponentKey { get; }

        /// <summary>
        /// Relative file path
        /// </summary>
        /// <remarks>
        /// The path is relative to the Sonar project root.
        /// The path is in Windows format i.e. the directory separators are backslashes
        /// </remarks>
        public string FilePath { get; }

        public SonarQubeHotspotRule Rule { get; }
        public string ProjectKey { get; }
        public string ProjectName { get; }
        public DateTimeOffset CreationTimestamp { get; }
        public DateTimeOffset LastUpdateTimestamp { get; }
        public string Resolution { get; }
    }
}

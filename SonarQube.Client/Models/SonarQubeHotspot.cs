/*
 * SonarQube Client
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

namespace SonarQube.Client.Models
{
    public class SonarQubeHotspot
    {
        public SonarQubeHotspot(
            string hotspotKey, string message, string assignee, string status,
            string organization, string projectKey, string projectName,
            string componentKey, string componentPath,
            string ruleKey, string ruleName, string securityCategory, 
            string vulnerabilityProbability, IssueTextRange textRange)
        {
            HotspotKey = hotspotKey;
            Message = message;
            Assignee = assignee;
            Status = status;

            Organization = organization;
            ProjectKey = projectKey;
            ProjectName = projectName;

            ComponentKey = componentKey;
            ComponentPath = componentPath;

            RuleKey = ruleKey;
            RuleName = ruleName;
            SecurityCategory = securityCategory;
            VulnerabilityProbability = vulnerabilityProbability;
            TextRange = textRange;
        }

        public string HotspotKey { get; }
        public string Message { get; }
        public string Assignee { get; }
        public string Status { get; }
        public IssueTextRange TextRange { get; }
        public string Organization { get; } 
        public string ComponentKey { get; }
        public string ComponentPath { get; }
        public string RuleKey { get; }
        public string RuleName { get; }
        public string SecurityCategory { get; }
        public string VulnerabilityProbability { get; }
        public string ProjectKey { get; }
        public string ProjectName { get; }
    }
}

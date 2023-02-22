﻿/*
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
    public class SonarQubeHotspotRule
    {
        public SonarQubeHotspotRule(string ruleKey, string ruleName,
            string securityCategory, string vulnProbability,
            string riskDescription, string vulnDescription, string fixRecommendations)
        {
            RuleKey = ruleKey;
            RuleName = ruleName;
            SecurityCategory = securityCategory;
            VulnerabilityProbability = vulnProbability;
            RiskDescription = riskDescription;
            VulnerabilityDescription = vulnDescription;
            FixRecommendations = fixRecommendations;
        }

        public string RuleKey { get; }
        public string RuleName { get; }
        public string SecurityCategory { get; }
        public string VulnerabilityProbability { get; }
        public string RiskDescription { get; }
        public string VulnerabilityDescription { get; }
        public string FixRecommendations { get; }
    }
}

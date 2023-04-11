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

using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Rules
{
    public static class RuleExtensions
    {
        public static bool IsRichRuleDescription(this IRuleInfo ruleInfo)
        {
            return ruleInfo.DescriptionSections != null && ruleInfo.DescriptionSections.Count > 1;
        }

        internal static string GetCompositeKey(this SonarQubeRule sonarQubeRule) => $"{sonarQubeRule.RepositoryKey}:{sonarQubeRule.Key}";

        internal static RuleIssueSeverity ToRuleIssueSeverity(this SonarQubeIssueSeverity sonarQubeIssueSeverity)
        {
            switch (sonarQubeIssueSeverity)
            {
                case SonarQubeIssueSeverity.Blocker:
                    return RuleIssueSeverity.Blocker;

                case SonarQubeIssueSeverity.Critical:
                    return RuleIssueSeverity.Critical;

                case SonarQubeIssueSeverity.Info:
                    return RuleIssueSeverity.Info;

                case SonarQubeIssueSeverity.Major:
                    return RuleIssueSeverity.Major;

                case SonarQubeIssueSeverity.Minor:
                    return RuleIssueSeverity.Minor;

                default:
                    return RuleIssueSeverity.Unknown;
            }
        }

        internal static RuleIssueType ToRuleIssueType(this SonarQubeIssueType sonarQubeIssueType)
        {
            switch (sonarQubeIssueType)
            {
                case SonarQubeIssueType.CodeSmell:
                    return RuleIssueType.CodeSmell;

                case SonarQubeIssueType.Bug:
                    return RuleIssueType.Bug;

                case SonarQubeIssueType.Vulnerability:
                    return RuleIssueType.Vulnerability;

                case SonarQubeIssueType.SecurityHotspot:
                    return RuleIssueType.Hotspot;

                default:
                    return RuleIssueType.Unknown;
            }
        }

        internal static IDescriptionSection ToDescriptionSection(this SonarQubeDescriptionSection sonarQubeDescriptionSection)
        {
            return new DescriptionSection(sonarQubeDescriptionSection.Key, HtmlXmlCompatibilityHelper.EnsureHtmlIsXml(sonarQubeDescriptionSection.HtmlContent), sonarQubeDescriptionSection.Context?.ToContext());
        }

        private static IContext ToContext(this SonarQubeContext sonarQubeContext)
        {
            return new Context(sonarQubeContext.Key, sonarQubeContext.DisplayName);
        }
    }
}

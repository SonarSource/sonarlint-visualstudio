/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

namespace SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;

// Converts SonarQube issues to SuppressedIssues that can be compared more easily with Roslyn issues
internal static class IssueConverter
{
    public static SuppressedIssue Convert(SonarQubeIssue issue)
    {
        var (repoKey, ruleKey) = GetRepoAndRuleKey(issue.RuleId);
        var language = GetRoslynLanguage(repoKey);

        var line = issue.TextRange == null ? (int?)null : issue.TextRange.StartLine - 1;
        return new SuppressedIssue
        {
            RoslynRuleId = ruleKey,
            FilePath = issue.FilePath,
            Hash = issue.Hash,
            RoslynLanguage = language,
            RoslynIssueLine = line,
            IssueServerKey = issue.IssueKey
        };
    }

    private static (string repoKey, string ruleKey) GetRepoAndRuleKey(string sonarRuleId)
    {
        // Sonar rule ids are in the form "[repo key]:[rule key]"
        var separatorPos = sonarRuleId.IndexOf(":", StringComparison.OrdinalIgnoreCase);
        if (separatorPos > -1)
        {
            var repoKey = sonarRuleId.Substring(0, separatorPos);
            var ruleKey = sonarRuleId.Substring(separatorPos + 1);

            return (repoKey, ruleKey);
        }

        return (null, null); // invalid rule key -> ignore
    }

    private static RoslynLanguage GetRoslynLanguage(string repoKey)
    {
        // Currently the only Sonar repos which contain Roslyn analysis rules are
        // csharpsquid and vbnet. These include "normal" and "hotspot" rules.
        // The taint rules are in a different repo, and the part that is implemented
        // as a Roslyn analyzer won't raise issues anyway.
        switch (repoKey)
        {
            case "csharpsquid": // i.e. the rules in SonarAnalyzer.CSharp
                return RoslynLanguage.CSharp;
            case "vbnet": // i.e. SonarAnalyzer.VisualBasic
                return RoslynLanguage.VB;
            default:
                return RoslynLanguage.Unknown;
        }
    }
}

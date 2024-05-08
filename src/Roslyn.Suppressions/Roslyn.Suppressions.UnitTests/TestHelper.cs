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

using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    internal static class TestHelper
    {
        public static SuppressedIssue CreateIssue(string ruleId = "ruleId",
            string path = "path",
            int? line = 0,
            string hash = "hash",
            RoslynLanguage language = RoslynLanguage.CSharp) => new SuppressedIssue
            {
                FilePath = path,
                Hash = hash,
                RoslynLanguage = language,
                RoslynRuleId = ruleId,
                RoslynIssueLine = line
            };

        public static SonarQubeIssue CreateSonarQubeIssue(string ruleId = "any",
                int? line = null,
                string filePath = "filePath",
                string hash = "hash",
                bool isSuppressed = true)
        { 
            var sonarQubeIssue = new SonarQubeIssue(
                "issuedId",
                filePath,
                hash,
                "message",
                "moduleKey",
                ruleId,
                false, // isResolved
                SonarQubeIssueSeverity.Info,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                line.HasValue ? new IssueTextRange(line.Value, line.Value, 1, 999) : null,
                null
                );

            sonarQubeIssue.IsResolved = isSuppressed;

            return sonarQubeIssue;
        }
    }


}

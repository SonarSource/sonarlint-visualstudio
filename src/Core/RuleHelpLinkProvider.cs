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

using System.Collections.Generic;

namespace SonarLint.VisualStudio.Core
{
    public interface IRuleHelpLinkProvider
    {
        string GetHelpLink(string ruleKey);
    }

    public class RuleHelpLinkProvider : IRuleHelpLinkProvider
    {
        private static readonly IDictionary<string, string> repoKeyToFolderNameMap = new Dictionary<string, string>
        {
            { SonarRuleRepoKeys.CSharpSecurityRules, "csharp" },

            // Support for C# hotspots. No need to special-case the VB.NET hotspots, as their repo name is identical to the one on rules.sonarsource.com
            { SonarRuleRepoKeys.CSharpRules, "csharp" },

            { SonarRuleRepoKeys.JsSecurityRules, "javascript" },
            { SonarRuleRepoKeys.TsSecurityRules, "typescript" },

            // TODO: there may be other repo keys that have different language names on rules.sonarsource.com
            // See https://github.com/SonarSource/sonarlint-visualstudio/issues/4586.
            { SonarRuleRepoKeys.HtmlRules, "html" }
        };

        public string GetHelpLink(string ruleKey)
        {
            // ruleKey is in format "javascript:S1234" (or javascript:SOMETHING for legacy keys)
            // NB: there are some "common" rules that are implemented on the server-side. We do
            // need to handle these case as they will never be raised in the IDE (and don't seem
            // to be documented on the rule site anyway).
            //   e.g. common-c:DuplicatedBlocks, common-cpp:FailedUnitTests

            var colonIndex = ruleKey.IndexOf(':');
            var repoKey = ruleKey.Substring(0, colonIndex);
            var ruleId = ruleKey.Substring(colonIndex + 1);

            var languageFolderName = GetWebsiteFolderName(repoKey);
            var webSiteRuleId = GetWebsiteRuleId(ruleId);

            return $"https://rules.sonarsource.com/{languageFolderName}/{webSiteRuleId}";
        }

        private string GetWebsiteFolderName(string repoKey)
        {
            // The rules for each language are in a separate folder in the rules website.
            // For some languages, the folder name happens to match the repo key.
            // The dictionary provides the mapping to use in cases where they are not the same.
            if (repoKeyToFolderNameMap.TryGetValue(repoKey, out var folderName))
            {
                return folderName;
            }

            // Assume the folder name is the same as the repo key
            return repoKey;
        }

        private static string GetWebsiteRuleId(string ruleId)
        {
            // Website ruleId should be "RSPEC-1234" (or RSPEC-SOMETHING for legacy keys)
            string webSiteId;
            if (ruleId.Length > 1 &&
                ruleId[0] == 'S' &&
                char.IsDigit(ruleId[1]))
            {
                webSiteId = ruleId.Substring(1);
            }
            else
            {
                webSiteId = ruleId; // assume it's a legacy key
            }

            return "RSPEC-" + webSiteId;
        }
    }
}

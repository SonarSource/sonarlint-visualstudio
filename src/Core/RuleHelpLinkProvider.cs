/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

namespace SonarLint.VisualStudio.Core
{
    public interface IRuleHelpLinkProvider
    {
        string GetHelpLink(string ruleKey);
    }

    public class RuleHelpLinkProvider : IRuleHelpLinkProvider
    {
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

        private static string GetWebsiteFolderName(string repoKey)
        {
            var language = LanguageProvider.Instance.AllKnownLanguages.FirstOrDefault(lang => lang.HasRepoKey(repoKey));

            if (language?.SecurityRepoInfo?.Key == repoKey)
            {
                return language?.SecurityRepoInfo?.FolderName;
            }

            return language?.RepoInfo.FolderName ?? repoKey;
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

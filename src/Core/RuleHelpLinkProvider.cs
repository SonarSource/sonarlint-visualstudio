/*
 * SonarLint for Visual Studio
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
            var colonIndex = ruleKey.IndexOf(':');
            // ruleKey is in format "javascript:S1234" (or javascript:SOMETHING for legacy keys)

            // language is "javascript"
            var language = ruleKey.Substring(0, colonIndex);

            // ruleId should be "1234" (or SOMETHING for legacy keys)
            var ruleId = ruleKey.Substring(colonIndex + 1);
            if (ruleId.Length > 1 &&
                ruleId[0] == 'S' &&
                char.IsDigit(ruleId[1]))
            {
                ruleId = ruleId.Substring(1);
            }

            return $"https://rules.sonarsource.com/{language}/RSPEC-{ruleId}";
        }
    }
}

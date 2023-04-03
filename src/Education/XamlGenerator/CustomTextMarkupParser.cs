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

using SonarLint.VisualStudio.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

/* Notes:
 *   
 *   The SonarQube and SonarCloud suppport a special embedded syntax to indicate
 *   cross-references between links
 *   
 *   e.g. https://sonarcloud.io/organizations/sonarsource/rules?languages=cs&open=csharpsquid%3AS4002&q=S4002
 * 
 *   The cross-references are embedded in the rule description text as follows:
 *      {rule:[repoKey]:[ruleKey]}
 * 
 *   This class parses simple text strings looking for the embedded syntax and
 *   calls the appropriate callback so the caller can render the appropriate output.
 */

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    /// <summary>
    /// Processes special internal markup embedded in normal text
    /// </summary>
    internal class CustomTextMarkupParser
    {
        /// <summary>
        /// Processes a text string that might/might not contain rule cross-references
        /// </summary>
        public static IEnumerable<ITextToken> Parse(string text)
        {
            var textTokens = new List<ITextToken>();

            // Matches -   ... text ... {rule:cpp:S123} ... text ... {rule:cpp:S999} ... text
            var matches = Regex.Matches(text, "{rule:(?<repoKey>[A-Za-z]+):(?<ruleKey>[\\w]+)}");

            int endOfLastMatch = 0;

            foreach (Match match in matches)
            {
                Debug.Assert(match.Success, "Expecting match to have succeeded");
                var matchGroup = match.Groups[0];

                var simpleText = text.Substring(endOfLastMatch, matchGroup.Index - endOfLastMatch);
                AddSimpleText(textTokens, simpleText);

                var repoKey = match.Groups["repoKey"].Value;
                var ruleKey = match.Groups["ruleKey"].Value;
                textTokens.Add(new RuleCrossRef(repoKey, ruleKey));

                endOfLastMatch = matchGroup.Index + matchGroup.Length;
            }

            var endText = text.Substring(endOfLastMatch);
            AddSimpleText(textTokens, endText);

            return textTokens;
        }

        private static void AddSimpleText(List<ITextToken> rawTextItems, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            rawTextItems.Add(new SimpleText(text));
        }
    }

    internal interface ITextToken { }
    internal interface ISimpleText : ITextToken { string Text { get; } }
    internal interface IRuleCrossRef : ITextToken { SonarCompositeRuleId CompositeRuleId { get; } }

    internal class SimpleText : ISimpleText
    {
        public string Text { get; }

        public SimpleText(string text)
        {
            Text = text;
        }
    }

    internal class RuleCrossRef : IRuleCrossRef
    {
        public SonarCompositeRuleId CompositeRuleId { get; }

        public RuleCrossRef(string repoKey, string ruleKey)
        {
            CompositeRuleId = new SonarCompositeRuleId(repoKey, ruleKey);
        }
    }
}

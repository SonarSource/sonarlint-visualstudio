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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using SonarLint.VisualStudio.Core;

/* Notes:
 *   
 *   The SonarQube and SonarCloud suppport a special embedded syntax to indicate
 *   cross-references between links
 *   
 *   e.g. https://sonarcloud.io/organizations/sonarsource/rules?languages=cs&open=csharpsquid%3AS4002&q=S4002
 * 
 *   The cross-references are embedded in the rule description text as follows:
 *      {rule:[repoKey]:[ruleKey]}
 */

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    /// <summary>
    /// Processes special internal markup embedded in normal text
    /// </summary>
    internal static class CustomTextMarkupParser
    {
        /// <summary>
        /// Matches - ... text ... {rule:cpp:S123} ... text ... {rule:cpp:S999} ... text
        /// </summary>
        private static readonly Regex RuleCrossRefRegEx = new Regex("{rule:(?<repoKey>[A-Za-z]+):(?<ruleKey>[\\w]+)}",
            RegexOptions.Compiled, RegexConstants.DefaultTimeout);

        /// <summary>
        /// Parse a text string that might/might not contain rule cross-references
        /// </summary>
        public static IEnumerable<ITextToken> Parse(string text)
        {
            var textTokens = new List<ITextToken>();

            var matches = RuleCrossRefRegEx.Matches(text);

            int endOfLastMatch = 0;

            foreach (Match match in matches)
            {
                Debug.Assert(match.Success, "Expecting match to have succeeded");
                var matchGroup = match.Groups[0];

                // Each regex match is a an embedded rule cross reference. There might be simple text before the current group and the previous group, so we need to extract that too.
                var simpleText = text.Substring(endOfLastMatch, matchGroup.Index - endOfLastMatch);
                AddSimpleText(textTokens, simpleText);

                var repoKey = match.Groups["repoKey"].Value;
                var ruleKey = match.Groups["ruleKey"].Value;
                textTokens.Add(new RuleCrossRef(repoKey, ruleKey));

                endOfLastMatch = matchGroup.Index + matchGroup.Length;
            }

            // Capture any simple text after the last rule cross reference.
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
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(nameof(text));
            }

            Text = text;
        }
    }

    internal class RuleCrossRef : IRuleCrossRef
    {
        public SonarCompositeRuleId CompositeRuleId { get; }

        public RuleCrossRef(string repoKey, string ruleKey)
        {
            if (string.IsNullOrEmpty(repoKey))
            {
                throw new ArgumentNullException(nameof(repoKey));
            }

            if (string.IsNullOrEmpty(ruleKey))
            {
                throw new ArgumentNullException(nameof(ruleKey));
            }

            CompositeRuleId = new SonarCompositeRuleId(repoKey, ruleKey);
        }
    }
}

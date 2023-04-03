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

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    /// <summary>
    /// Processes special internal markup embedded in normal text
    /// </summary>
    internal class RawTextProcessor
    {
        /// <summary>
        /// Processes a text string that might/might not contain rule cross-references
        /// </summary>
        public static List<IRawTextItem> ProcessRawText(string rawText)
        {
            var rawTextItems = new List<IRawTextItem>();

            // Matches -   ... text ... {rule:cpp:S123} ... text ... {rule:cpp:S999} ... text
            var matches = Regex.Matches(rawText,
                "{rule:([A-Za-z]+):([\\w]+)}");

            int endOfLastMatch = 0;

            foreach (Match match in matches)
            {
                Debug.Assert(match.Success, "Expecting match to have succeeded");
                var matchGroup = match.Groups[0];

                var simpleText = rawText.Substring(endOfLastMatch, matchGroup.Index - endOfLastMatch);
                AddSimpleText(rawTextItems, simpleText);

                rawTextItems.Add(new RawTextItem(matchGroup.Value, true));

                endOfLastMatch = matchGroup.Index + matchGroup.Length;
            }

            var endText = rawText.Substring(endOfLastMatch);
            AddSimpleText(rawTextItems, endText);

            return rawTextItems;
        }

        private static void AddSimpleText(List<IRawTextItem> rawTextItems, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            rawTextItems.Add(new RawTextItem(text, false));
        }
    }

    public interface IRawTextItem
    {
        string Text { get; }
        bool IsCrossReference { get; }
    }

    public class RawTextItem : IRawTextItem
    {
        public string Text { get; }
        public bool IsCrossReference { get; }

        public RawTextItem(string text, bool isCrossReference)
        {
            Text = text;
            IsCrossReference = isCrossReference;
        }
    }
}

﻿/*
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

using System.Text.RegularExpressions;

namespace SonarLint.VisualStudio.Education.Rule
{
    internal static class HtmlXmlCompatibilityHelper
    {
        // Regular expression that find empty "col"and "br" HTML elements
        // e.g. <br>, <br >, <col>, <col span="123">
        // This is valid HTML, but means we can't parse it as XML. So, we find
        // the empty elements and replace them with elements with closing tags
        // e.g. <br>  =>  <br/>
        // e.g. <col span="123">  =>  <col span="123"/>
        private static readonly Regex CleanCol = new Regex("(?<element>(<\\s*col\\s*)|(<\\s*col\\s+[^/^>]*))>",
            RegexOptions.Compiled,
            Core.RegexConstants.DefaultTimeout);

        private static readonly Regex CleanBr = new Regex("(?<element>(<\\s*br\\s*)|(<\\s*br\\s+[^/^>]*))>",
            RegexOptions.Compiled,
            Core.RegexConstants.DefaultTimeout);

        public static string EnsureHtmlIsXml(string pluginRuleDescription)
        {
            if (pluginRuleDescription == null)
            {
                return null;
            }

            var xml = pluginRuleDescription.Replace("&nbsp;", "&#160;");

            xml = CleanCol.Replace(xml, "${element}/>");
            xml = CleanBr.Replace(xml, "${element}/>");

            return xml;
        }
    }
}

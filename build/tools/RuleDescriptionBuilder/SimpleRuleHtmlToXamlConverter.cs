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

namespace RuleDescriptionBuilder
{
    /// <summary>
    /// Process a simple rule description in HTML format and converts it to XAML
    /// </summary>
    internal class SimpleRuleHtmlToXamlConverter
    {
        public static string Convert(Rule rule)
        {
            // TODO: extract the HTML description for each rule

            var placeholder = @$"
<FlowDocument xml:space=""preserve"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Paragraph>Rule: {rule.Key}</Paragraph>
    <Paragraph>TODO: rule description</Paragraph>
    <Paragraph FontSize=""20pt"">Noncompliant Code Example</Paragraph>
    <Paragraph FontSize=""8pt"" TextAlignment=""Left"" FontFamily=""Courier New"">non-compliant code</Paragraph>
    <Paragraph FontSize=""20pt"">Compliant Solution</Paragraph>
    <Paragraph FontSize=""8pt"" TextAlignment=""Left"" FontFamily=""Courier New"">compliant code</Paragraph>
</FlowDocument>";

            return placeholder;
        }
    }
}

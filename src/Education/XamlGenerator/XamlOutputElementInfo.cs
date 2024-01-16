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

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    /// <summary>
    /// Information about a single XAML output element written to the new stream of XAML.
    /// </summary>
    internal struct XamlOutputElementInfo
    {
        public XamlOutputElementInfo(string htmlElementName, bool supportsInlines)
        {
            HtmlElementName = htmlElementName;
            SupportsInlines = supportsInlines;
        }

        /// <summary>
        /// The HTML element that the XAML element corresponds to
        /// </summary>
        public string HtmlElementName { get; }

        /// <summary>
        /// True if the XAML element supports inlines as children (e.g. Run, text), otherwise false
        /// </summary>
        public bool SupportsInlines { get; }

        /// <summary>
        /// True if the XAML element supports blocks as children, otherwise false
        /// </summary>
        /// <remarks>This is a convenience method - there are no XAML elements that support both Inlines
        /// and Blocks as children, so we just return the negation of SupportsInLines.
        /// Note that there are some classes that don't support either Inlines or Blocks as children such as 
        /// List and the Table classes. However, as long as the HTML document we are parsing is valid it
        /// won't cause us a problem since we won't need to check SupportsInlines/SupportsBlocks for those
        /// classes.
        /// </remarks>
        public bool SupportsBlocks => !SupportsInlines;
    }
}

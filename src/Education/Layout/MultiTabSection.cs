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

using System.Windows.Documents;
using SonarLint.VisualStudio.Education.Layout.Tabs;
using SonarLint.VisualStudio.Education.XamlParser;

namespace SonarLint.VisualStudio.Education.Layout
{
    /// <summary>
    /// Represents section with a header that has a configurable set of content subtabs
    /// </summary>
    internal class MultiTabSection : IAbstractVisualizationTreeNode
    {
        private readonly IXamlBlockContent header;
        private readonly ITabGroup tabs;

        public MultiTabSection(IXamlBlockContent header, ITabGroup tabs)
        {
            this.header = header;
            this.tabs = tabs;
        }

        public Block CreateVisualization()
        {
            var container = new Section();
            container.Blocks.Add(header.GetObjectRepresentation());
            container.Blocks.Add(tabs.CreateVisualization());
            return container;
        }
    }
}

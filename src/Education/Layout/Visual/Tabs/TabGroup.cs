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
using System.Xml;

namespace SonarLint.VisualStudio.Education.Layout.Visual.Tabs
{
    /// <summary>
    /// Represents tab grouping that renders the necessary tab buttons and sets the default active tab
    /// </summary>
    internal class TabGroup : IAbstractVisualizationTreeNode
    {
        internal /* for testing */ readonly List<ITabItem> tabs;
        internal /* for testing */ int selectedTabIndex;

        public TabGroup(List<ITabItem> tabs, int selectedTabIndex)
        {
            this.tabs = tabs;
            this.selectedTabIndex = selectedTabIndex;
        }

        public void ProduceXaml(XmlWriter writer)
        {
            writer.WriteStartElement("BlockUIContainer");
            writer.WriteStartElement("TabControl");
            writer.WriteAttributeString("TabStripPlacement", "Top");
            writer.WriteAttributeString("SelectedIndex", selectedTabIndex.ToString());
            foreach (var tab in tabs)
            {
                tab.ProduceXaml(writer);
            }
            writer.WriteFullEndElement();
            writer.WriteFullEndElement();
        }
    }
}

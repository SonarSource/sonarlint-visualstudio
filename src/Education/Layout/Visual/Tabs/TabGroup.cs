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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using SonarLint.VisualStudio.Education.Layout.Visual;

namespace SonarLint.VisualStudio.Education.Layout.Visual.Tabs
{
    /// <summary>
    /// Represents tab grouping that renders the necessary tab buttons and sets the default active tab
    /// </summary>
    internal class TabGroup : IAbstractVisualizationTreeNode
    {
        private readonly string name;
        private readonly List<TabItem> tabs;
        private readonly ITabsRepository tabsRepository;

        public TabGroup(string name, List<TabItem> tabs, ITabsRepository tabsRepository)
        {
            this.name = name;
            this.tabs = tabs;
            this.tabsRepository = tabsRepository;
        }

        public Block CreateVisualization()
        {
            var container = new Section();
            var buttonsContainer = new StackPanel();
            Block initiallyActiveTab = null;

            for (var i = 0; i < tabs.Count; i++)
            {
                var tabItem = tabs[i];
                buttonsContainer.Children.Add(
                    new ToggleButton() { Name = TabNameProvider.GetTabButtonName(name, tabItem.Name), Content = tabItem.DisplayName });
                var tabVisualization = tabItem.CreateVisualization(name);
                tabsRepository.RegisterTab(tabVisualization);

                if (i == 0)
                {
                    initiallyActiveTab = tabVisualization;
                }
            }

            container.Blocks.Add(new BlockUIContainer(buttonsContainer) { Name = name });
            container.Blocks.Add(initiallyActiveTab);

            return container;
        }
    }
}

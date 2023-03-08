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

using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;
using TabItem = SonarLint.VisualStudio.Education.Layout.Visual.Tabs.TabItem;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout
{
    [TestClass]
    public class TabGroupTests
    {
        [TestMethod]
        public void CreateVisualization_GeneratesCorrectStructure()
        {
            var tabGroupName = "MainTabsGroup";
            var tabNames = new[] { "why", "how_to_fix", "more_info" };
            var tabDisplayNames = new[] { "Why is this an issue", "How to fix it?", "More info" };
            var tabContents = new Block[]{ new Paragraph(), new Section(), new BlockUIContainer()};
            var tabsRepositoryMock = new Mock<ITabsRepository>();

            var testSubject = new TabGroup(tabGroupName,
                tabNames.Select((name, i) => new TabItem(name, tabDisplayNames[i], SetUpTabMock(tabContents[i])))
                    .ToList(), tabsRepositoryMock.Object);

            var visualization = (Section)testSubject.CreateVisualization();

            visualization.Blocks.FirstBlock.Should().BeOfType<BlockUIContainer>();
            visualization.Blocks.Count.Should().Be(2);

            VerifyButtonsGeneratedCorrectly(visualization, tabGroupName, tabNames, tabDisplayNames);
            VerifyActiveTabIsPresent(visualization, tabGroupName, tabNames);
            VerifyAllTabsAreRegistered(tabsRepositoryMock, tabGroupName, tabNames, tabContents);
        }

        private static void VerifyButtonsGeneratedCorrectly(Section visualizationRoot, string tabGroupName, string[] tabNames, string[] tabDisplayNames)
        {
            var buttonsBlock = (BlockUIContainer)visualizationRoot.Blocks.FirstBlock;
            buttonsBlock.Name.Should().Be(tabGroupName);
            buttonsBlock.Child.Should().BeOfType<StackPanel>();
            var buttons = ((StackPanel)buttonsBlock.Child).Children.Cast<ToggleButton>().ToList();
            buttons.Count.Should().Be(tabNames.Length);

            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                button.Name.Should().Be(TabNameProvider.GetTabButtonName(tabGroupName, tabNames[i]));
                button.Content.Should().Be(tabDisplayNames[i]);
            }
        }

        private static void VerifyActiveTabIsPresent(Section visualizationRoot, string tabGroupName, string[] tabNames)
        {
            visualizationRoot.Blocks.LastBlock.Should().BeOfType<Section>();
            var activeTab = (Section)visualizationRoot.Blocks.LastBlock;
            activeTab.Name.Should().Be(TabNameProvider.GetTabSectionName(tabGroupName, tabNames[0]));
        }

        private static void VerifyAllTabsAreRegistered(Mock<ITabsRepository> tabsRepositoryMock, string tabGroupName, string[] tabNames, Block[] tabContents)
        {
            tabsRepositoryMock.Verify(repository => repository.RegisterTab(It.IsAny<Section>()), Times.Exactly(3));

            foreach (var (section, i) in tabsRepositoryMock.Invocations.Select((invocation, index) =>
                         ((Section)invocation.Arguments[0], index)))
            {
                section.Name.Should().Be(TabNameProvider.GetTabSectionName(tabGroupName, tabNames[i]));
                section.Blocks.Single().Should().BeSameAs(tabContents[i]);
            }
        }

        private IAbstractVisualizationTreeNode SetUpTabMock(Block visualization)
        {
            var mock = new Mock<IAbstractVisualizationTreeNode>();
            mock.Setup(x => x.CreateVisualization()).Returns(visualization);
            return mock.Object;
        }
    }
}

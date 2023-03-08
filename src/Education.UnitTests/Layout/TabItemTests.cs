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
using System.Windows.Documents;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout
{
    [TestClass]
    public class TabItemTests
    {
        [TestMethod]
        public void CreateVisualization_ReturnsSectionWithCorrectNameAndContent()
        {
            var tabContentMock = new Mock<IAbstractVisualizationTreeNode>();
            var tabContentVisualization = new Section();
            tabContentMock.Setup(x => x.CreateVisualization()).Returns(tabContentVisualization);
            var tabName = "TabName";
            var tabGroup = "MainTabGroup";

            var testSubject = new TabItem(tabName, "Very nice tab", tabContentMock.Object);

            var visualization = testSubject.CreateVisualization(tabGroup);

            visualization.Should().BeOfType<Section>();
            var section = (Section)visualization;
            section.Name.Should().Be(TabNameProvider.GetTabSectionName(tabGroup, tabName));
            section.Blocks.Single().Should().BeSameAs(tabContentVisualization);
        }
    }
}

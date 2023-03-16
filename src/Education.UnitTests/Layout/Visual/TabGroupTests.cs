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
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout.Visual
{
    [TestClass]
    public class TabGroupTests
    {
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CreateVisualization_GeneratesCorrectStructure(bool isScrollable)
        {
            var sb = new StringBuilder();
            var xmlWriter = RuleHelpXamlTranslator.CreateXmlWriter(sb);
            var order = new MockSequence();
            var tabItems = new Mock<ITabItem>[]
            {
                new Mock<ITabItem>(MockBehavior.Strict),
                new Mock<ITabItem>(MockBehavior.Strict),
                new Mock<ITabItem>(MockBehavior.Strict),
            };
            for (var index = 0; index < tabItems.Length; index++)
            {
                var tabItem = tabItems[index];
                var indexCopy = index;
                tabItem
                    .InSequence(order)
                    .Setup(x => x.ProduceXaml(xmlWriter))
                    .Callback(() => xmlWriter.WriteRaw($"<TabItem>Tab {indexCopy} placeholder</TabItem>"));

            }

            var testSubject = new TabGroup(tabItems.Select(x => x.Object).ToList());

            testSubject.ProduceXaml(xmlWriter);
            xmlWriter.Close();

            sb.ToString().Should().BeEquivalentTo("<BlockUIContainer>\r\n  <TabControl><TabItem>Tab 0 placeholder</TabItem><TabItem>Tab 1 placeholder</TabItem><TabItem>Tab 2 placeholder</TabItem></TabControl>\r\n</BlockUIContainer>");
            foreach (var tabItem in tabItems)
            {
                tabItem.Verify(x => x.ProduceXaml(xmlWriter), Times.Once);
            }
        }
    }
}

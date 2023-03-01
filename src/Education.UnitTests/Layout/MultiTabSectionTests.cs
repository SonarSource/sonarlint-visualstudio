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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Education.Layout;
using SonarLint.VisualStudio.Education.Layout.Tabs;
using SonarLint.VisualStudio.Education.XamlParser;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout
{
    [TestClass]
    public class MultiTabSectionTests
    {
        [TestMethod]
        public void CreateVisualization_ReturnsSectionWithHeaderAndTabGroup()
        {
            var xamlHeaderMock = new Mock<IXamlBlockContent>();
            var header = new Paragraph();
            xamlHeaderMock.Setup(x => x.GetObjectRepresentation()).Returns(header);

            var tabGroupMock = new Mock<ITabGroup>();
            var tabsSectionStub = new Section();
            tabGroupMock.Setup(x => x.CreateVisualization()).Returns(tabsSectionStub);

            var testSubject = new MultiTabSection(xamlHeaderMock.Object, tabGroupMock.Object);

            var visualization = (Section)testSubject.CreateVisualization();
            
            visualization.Blocks.Count.Should().Be(2);
            visualization.Blocks.FirstBlock.Should().BeSameAs(header);
            visualization.Blocks.LastBlock.Should().BeSameAs(tabsSectionStub);
        }
    }
}

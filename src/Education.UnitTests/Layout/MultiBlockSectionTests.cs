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

using System.Collections;
using System.Windows.Documents;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Education.Layout.Visual;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout
{
    [TestClass]
    public class MultiBlockSectionTests
    {
        [TestMethod]
        public void CreateVisualization_ReturnsSectionWithSubsections()
        {
            var headerMock = new Mock<IAbstractVisualizationTreeNode>();
            var header = new Paragraph();
            headerMock.Setup(x => x.CreateVisualization()).Returns(header);
            
            var child1 = new Section();
            var child1Node = new Mock<IAbstractVisualizationTreeNode>();
            child1Node.Setup(x => x.CreateVisualization()).Returns(child1);

            var child2 = new BlockUIContainer();
            var child2Node = new Mock<IAbstractVisualizationTreeNode>();
            child2Node.Setup(x => x.CreateVisualization()).Returns(child2);

            var testSubject =
                new MultiBlockSection(headerMock.Object, child1Node.Object, child2Node.Object);

            var visualization = testSubject.CreateVisualization();

            visualization.Should().BeOfType<Section>();
            var section = visualization as Section;
            section.Blocks.Count.Should().Be(3);
            section.Blocks.FirstBlock.Should().BeSameAs(header);
            var child1Section = ((IList)section.Blocks)[1];
            child1Section.Should().BeSameAs(child1);
            var child2Section = ((IList)section.Blocks)[2];
            child2Section.Should().BeSameAs(child2);
        }
    }
}

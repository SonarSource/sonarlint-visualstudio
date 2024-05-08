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

using System.Xml;
using SonarLint.VisualStudio.Education.Layout.Visual;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout.Visual
{
    [TestClass]
    public class MultiBlockSectionTests
    {
        [TestMethod]
        public void CreateVisualization_ReturnsSectionWithSubsections()
        {
            var callOrder = new MockSequence();
            var headerMock = new Mock<IAbstractVisualizationTreeNode>(MockBehavior.Strict);
            headerMock.InSequence(callOrder).Setup(x => x.ProduceXaml(It.IsAny<XmlWriter>()));
            var child1Node = new Mock<IAbstractVisualizationTreeNode>(MockBehavior.Strict);
            child1Node.InSequence(callOrder).Setup(x => x.ProduceXaml(It.IsAny<XmlWriter>()));
            var child2Node = new Mock<IAbstractVisualizationTreeNode>(MockBehavior.Strict);
            child2Node.InSequence(callOrder).Setup(x => x.ProduceXaml(It.IsAny<XmlWriter>()));

            var testSubject =
                new MultiBlockSection(headerMock.Object, child1Node.Object, child2Node.Object);

            testSubject.ProduceXaml(null);
            
            headerMock.Verify(x => x.ProduceXaml(It.IsAny<XmlWriter>()), Times.Once);
            child1Node.Verify(x => x.ProduceXaml(It.IsAny<XmlWriter>()), Times.Once);
            child2Node.Verify(x => x.ProduceXaml(It.IsAny<XmlWriter>()), Times.Once);
        }
    }
}

﻿/*
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

using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout
{
    [TestClass]
    public class TabItemTests
    {
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void CreateVisualization_ReturnsSectionWithCorrectNameAndContent(bool isScrollable)
        {
            var sb = new StringBuilder();
            var xmlWriter = RuleHelpXamlTranslator.CreateXmlWriter(sb);
            var tabContentMock = new Mock<IAbstractVisualizationTreeNode>();
            tabContentMock.Setup(x => x.ProduceXaml(xmlWriter)).Callback(() => xmlWriter.WriteRaw("<Paragraph>Hi</Paragraph>"));
            const string tabName = "Tab Display Name";

            var testSubject = new TabItem(tabName, tabContentMock.Object);

            testSubject.ProduceXaml(xmlWriter, isScrollable);
            xmlWriter.Close();

            var scrollingAttributes = isScrollable 
                ? string.Empty 
                : " HorizontalScrollBarVisibility=\"Disabled\" VerticalScrollBarVisibility=\"Disabled\"";
            sb.ToString().Should().BeEquivalentTo($"<TabItem Header=\"Tab Display Name\">\r\n  <NestingFlowDocumentScrollViewer{scrollingAttributes}>\r\n    <FlowDocument><Paragraph>Hi</Paragraph></FlowDocument>\r\n  </NestingFlowDocumentScrollViewer>\r\n</TabItem>");
            tabContentMock.Verify(x => x.ProduceXaml(xmlWriter), Times.Once);
        }
    }
}

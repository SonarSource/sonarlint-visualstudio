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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout.Logical;

[TestClass]
public class RichRuleDescriptionTests
{
    [TestMethod]
    public void Ctor_EnsuresHtmlIsXml()
    {
        var testSubject = new RichRuleDescription( "<col>", new List<IRuleDescriptionTab>());

        testSubject.introductionHtml.Should().BeEquivalentTo("<col/>");
    }
    
    [TestMethod]
    public void ProduceVisualNode_ProducesMultiBlockSectionWithIntroAndTabs()
    {
        const string introHtml = "introhtml";
        const string introXaml = "introxaml";
        const string tabTitle = "tabTitle";
        var translatorMock = new Mock<IRuleHelpXamlTranslator>();
        translatorMock.Setup(x => x.TranslateHtmlToXaml(introHtml)).Returns(introXaml);
        var parameters = new VisualizationParameters(translatorMock.Object, "context");
        var tabMock = new Mock<IRuleDescriptionTab>();
        tabMock.SetupGet(x => x.Title).Returns(tabTitle);
        var tabVisualNodeMock = new Mock<IAbstractVisualizationTreeNode>();
        tabMock.Setup(x => x.ProduceVisualNode(parameters)).Returns(tabVisualNodeMock.Object);
        
        var testSubject = new RichRuleDescription(introHtml, new List<IRuleDescriptionTab> { tabMock.Object });


        var visualNode = testSubject.ProduceVisualNode(parameters);
        
        
        visualNode.Should().BeEquivalentTo(
            new MultiBlockSection(
                new ContentSection(introXaml),
                new TabGroup(new List<ITabItem>{new TabItem(tabTitle, tabVisualNodeMock.Object)}, 0)));
        translatorMock.Verify(x => x.TranslateHtmlToXaml(introHtml), Times.Once);
        translatorMock.VerifyNoOtherCalls();
    }
}

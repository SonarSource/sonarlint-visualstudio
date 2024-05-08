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

using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout.Logical;

[TestClass]
public class NonContextualRuleDescriptionTabTests
{
    [TestMethod]
    public void Ctor_EnsuresHtmlIsXml()
    {
        var testSubject = new NonContextualRuleDescriptionTab("title", "<col>");

        testSubject.htmlContent.Should().BeEquivalentTo("<col/>");
    }
    
    [TestMethod]
    public void ProduceVisualNode_ReturnsSingleContentSection()
    {
        const string contentHtml = "contenthtml";
        const string contentXaml = "contentxaml";
        var translatorMock = new Mock<IRuleHelpXamlTranslator>();
        translatorMock.Setup(x => x.TranslateHtmlToXaml(contentHtml)).Returns(contentXaml);
        var parameters = new VisualizationParameters(translatorMock.Object, "context");
        
        var testSubject = new NonContextualRuleDescriptionTab("title", contentHtml);


        var visualNode = testSubject.ProduceVisualNode(parameters);
        
        
        visualNode.Should().BeEquivalentTo(new ContentSection(contentXaml));
        translatorMock.Verify(x => x.TranslateHtmlToXaml(contentHtml), Times.Once);
        translatorMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void Title_ReturnsCorrectTitle()
    {
        const string title = "title";
        
        var testSubject = new NonContextualRuleDescriptionTab(title, "contenthtml");

        testSubject.Title.Should().BeSameAs(title);
    }
}

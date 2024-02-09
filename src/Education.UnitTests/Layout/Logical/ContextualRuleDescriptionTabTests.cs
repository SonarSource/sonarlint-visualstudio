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
public class ContextualRuleDescriptionTabTests
{
    private const string Context1 = "context1";
    private const string ContextTabTitle1 = "contexttitle1";
    private const string ContextTabContent1 = "htmlcontent1";
    private const string ContextTabXamlContent1 = "xamlcontent1";
    private const string DefaultContextTabTitle = "default";
    private const string DefaultContext = "defaultcontext";
    private const string DefaultContextTabContent = "defaultcontent";
    private const string DefaultContextTabXamlContent = "defaultxamlcontent";
    private const string ContextTabTitle2 = "contexttitle2";
    private const string Context2 = "context2";
    private const string ContextTabContent2 = "htmlcontent2";
    private const string ContextTabXamlContent2 = "xamlcontent2";
    
    [TestMethod]
    public void Title_ReturnsCorrectTitle()
    {
        const string title = "title";

        var testSubject = new ContextualRuleDescriptionTab(title, null, null);

        testSubject.Title.Should().BeSameAs(title);
    }
    
    [TestMethod]
    public void ProduceVisualNode_ReturnsCorrectStructure()
    {
        var testSubject = new ContextualRuleDescriptionTab("title", 
            DefaultContext, 
            GetContextTabs());
        var translatorMock = new Mock<IRuleHelpXamlTranslator>();
        SetupHtmlToXamlConversion(translatorMock);
        
        var visualNode = testSubject.ProduceVisualNode(new VisualizationParameters(translatorMock.Object, Context1));
        
        visualNode.Should().BeEquivalentTo(
            new TabGroup(new List<ITabItem>
            {
                new TabItem(ContextTabTitle1, new ContentSection(ContextTabXamlContent1)),
                new TabItem(DefaultContextTabTitle, new ContentSection(DefaultContextTabXamlContent)),
                new TabItem(ContextTabTitle2, new ContentSection(ContextTabXamlContent2)),
            }, 0));
    }
    
    [DataRow(Context1, 0)]
    [DataRow(DefaultContext, 1)]
    [DataRow(Context2, 2)]
    [DataTestMethod]
    public void ProduceVisualNode_PrefersSelectedContext(string context, int expectedIndex)
    {
        var testSubject = new ContextualRuleDescriptionTab("title", 
            DefaultContext, 
            GetContextTabs());
        var translatorMock = new Mock<IRuleHelpXamlTranslator>();
        SetupHtmlToXamlConversion(translatorMock);

        var visualNode = testSubject.ProduceVisualNode(new VisualizationParameters(translatorMock.Object, context));

        visualNode.Should().BeOfType<TabGroup>().Which.selectedTabIndex.Should().Be(expectedIndex);
    }

    [TestMethod]
    public void ProduceVisualNode_NoSelectedContext_FallsBackToDefaultContext()
    {
        var testSubject = new ContextualRuleDescriptionTab("title", 
            DefaultContext, 
            GetContextTabs());
        var translatorMock = new Mock<IRuleHelpXamlTranslator>();
        SetupHtmlToXamlConversion(translatorMock);
        
        var visualNode = testSubject.ProduceVisualNode(new VisualizationParameters(translatorMock.Object, null));

        visualNode.Should().BeOfType<TabGroup>().Which.selectedTabIndex.Should().Be(1);
    }
    
    [TestMethod]
    public void ProduceVisualNode_NoContextProvided_SelectsFirstTab()
    {
        string NOCONTEXT = null;
        
        var testSubject = new ContextualRuleDescriptionTab("title", 
            NOCONTEXT, 
            GetContextTabs());
        var translatorMock = new Mock<IRuleHelpXamlTranslator>();
        SetupHtmlToXamlConversion(translatorMock);
        
        var visualNode = testSubject.ProduceVisualNode(new VisualizationParameters(translatorMock.Object, NOCONTEXT));

        visualNode.Should().BeOfType<TabGroup>().Which.selectedTabIndex.Should().Be(0);
    }

    private static List<ContextualRuleDescriptionTab.ContextContentTab> GetContextTabs()
    {
        return new List<ContextualRuleDescriptionTab.ContextContentTab>
        {
            new(ContextTabTitle1, Context1, ContextTabContent1),
            new(DefaultContextTabTitle, DefaultContext, DefaultContextTabContent),
            new(ContextTabTitle2, Context2, ContextTabContent2),
        };
    }

    private static void SetupHtmlToXamlConversion(Mock<IRuleHelpXamlTranslator> translator)
    {
        translator.Setup(x => x.TranslateHtmlToXaml(ContextTabContent1)).Returns(ContextTabXamlContent1);
        translator.Setup(x => x.TranslateHtmlToXaml(ContextTabContent2)).Returns(ContextTabXamlContent2);
        translator.Setup(x => x.TranslateHtmlToXaml(DefaultContextTabContent)).Returns(DefaultContextTabXamlContent);
    }
}

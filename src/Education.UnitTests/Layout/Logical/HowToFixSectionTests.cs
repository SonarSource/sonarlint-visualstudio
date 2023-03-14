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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout.Logical;

[TestClass]
public class HowToFixSectionTests
{
    [TestMethod]
    public void KeyAndTitle_AreCorrect()
    {
        var testSubject = new HowToFixItSection("");

        testSubject.Key.Should().BeSameAs(HowToFixItSection.RuleInfoKey).And.Be("how_to_fix");
        testSubject.Title.Should().Be("How can I fix it?");
    }

    [TestMethod]
    public void GetVisualizationTreeNode_Contextless_ProducesOneContentSection()
    {
        var partialXaml = "<Paragraph>Just fix it</Paragraph>";
        var testSubject = new HowToFixItSection(partialXaml);

        var visualizationTreeNode = testSubject.GetVisualizationTreeNode(null);

        visualizationTreeNode.Should().BeOfType<ContentSection>().Which.xamlContent.Should().Be(partialXaml);
    }

    [TestMethod]
    public void GetVisualizationTreeNode_ContextAware_ProducesCorrectMultiBlockSection()
    {
        var contexts = new List<HowToFixItSectionContext>
        {
            new HowToFixItSectionContext("aspnetmvc", "asp net mvc", "<Paragraph>rewrite to asp net core</Paragraph>"),
            new HowToFixItSectionContext("aspnetcore","asp net core", "<Paragraph>nothing to worry about, unless...</Paragraph>"),
        };
        var testSubject = new HowToFixItSection(contexts);
        var staticXamlStorage = new StaticXamlStorage(new RuleHelpXamlTranslator());
        
        var visualizationTreeNode = testSubject.GetVisualizationTreeNode(staticXamlStorage);
        
        var multiBlockSection = visualizationTreeNode.Should().BeOfType<MultiBlockSection>().Subject;
        multiBlockSection.blocks.Should().HaveCount(2);
        
        multiBlockSection.blocks[0].Should().BeOfType<ContentSection>()
            .Which.xamlContent.Should().Be(staticXamlStorage.HowToFixItHeader);
        
        var tabGroup = multiBlockSection.blocks[1].Should().BeOfType<TabGroup>().Subject;
        tabGroup.isScrollable.Should().BeFalse();
        tabGroup.tabs.Should().HaveCount(contexts.Count + 1);
        
        for (var i = 0; i < contexts.Count; i++)
        {
            ((TabItem)tabGroup.tabs[i]).content.Should().BeOfType<ContentSection>().Which.xamlContent.Should().Be(contexts[i].PartialXamlContent);
        }

        ((TabItem)tabGroup.tabs.Last()).content.Should().BeOfType<ContentSection>().Which.xamlContent.Should().Be(staticXamlStorage.HowToFixItFallbackContext);
    }
}

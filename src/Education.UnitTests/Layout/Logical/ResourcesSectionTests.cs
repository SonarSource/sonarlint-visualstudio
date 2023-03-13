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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout.Logical
{
    [TestClass]
    public class ResourcesSectionTests
    {
        [TestMethod]
        public void KeyAndTitle_AreCorrect()
        {
            var testSubject = new ResourcesSection("", null);

            testSubject.Key.Should().BeSameAs(ResourcesSection.RuleInfoKey).And.Be("resources");
            testSubject.Title.Should().Be("More info");
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetVisualizationTreeNode_NoEducation_ProducesCorrectMultiBlockSection(bool emptyOrNull)
        {
            var partialXaml = "<Paragraph>please read coding 101</Paragraph>";
            var testSubject = new ResourcesSection(partialXaml,
                emptyOrNull ? new List<string>() : null);
            var staticXamlStorage = new StaticXamlStorage(new RuleHelpXamlTranslator());

            var visualizationTreeNode = testSubject.GetVisualizationTreeNode(staticXamlStorage);

            var multiBlockSection = visualizationTreeNode.Should().BeOfType<MultiBlockSection>().Subject;
            multiBlockSection.blocks.Should().HaveCount(2);
            multiBlockSection.blocks[0].Should().BeOfType<ContentSection>()
                .Which.xamlContent.Should().Be(staticXamlStorage.ResourcesHeader);
            multiBlockSection.blocks[1].Should().BeOfType<ContentSection>()
                .Which.xamlContent.Should().Be(partialXaml);
        }

        [TestMethod]
        public void GetVisualizationTreeNode_WithEducation_ProducesCorrectMultiBlockSection()
        {
            var partialXaml = "<Paragraph>please read coding 101</Paragraph>";
            var testSubject = new ResourcesSection(partialXaml,
                new List<string>{ "defense_in_depth", "unknown_section_to_be_ignored", "never_trust_user_input" });
            var staticXamlStorage = new StaticXamlStorage(new RuleHelpXamlTranslator());

            var visualizationTreeNode = testSubject.GetVisualizationTreeNode(staticXamlStorage);

            var multiBlockSection = visualizationTreeNode.Should().BeOfType<MultiBlockSection>().Subject;
            multiBlockSection.blocks.Should().HaveCount(5);
            multiBlockSection.blocks[2].Should().BeOfType<ContentSection>()
                .Which.xamlContent.Should().Be(staticXamlStorage.EducationPrinciplesHeader);
            multiBlockSection.blocks[3].Should().BeOfType<ContentSection>()
                .Which.xamlContent.Should().Be(staticXamlStorage.EducationPrinciplesDefenseInDepth);
            multiBlockSection.blocks[4].Should().BeOfType<ContentSection>()
                .Which.xamlContent.Should().Be(staticXamlStorage.EducationPrinciplesNeverTrustUserInput);
        }
    }
}

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
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Rules;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout.Logical
{
    [TestClass]
    public class RuleInfoTranslatorTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            MefTestHelpers.CheckTypeCanBeImported<RuleInfoTranslator, IRuleInfoTranslator>(
                    MefTestHelpers.CreateExport<IRuleHelpXamlTranslatorFactory>(),
                    MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void GetRuleDescriptionSections_IgnoresUnknownSection()
        {
            var ruleInfo = GetRuleInfo(new[] { new DescriptionSection(Guid.NewGuid().ToString(), "<span>hello</span>") });
            var ruleHelpXamlTranslatorFactoryMock = new Mock<IRuleHelpXamlTranslatorFactory>();
            var ruleHelpXamlTranslatorMock = new Mock<IRuleHelpXamlTranslator>();
            ruleHelpXamlTranslatorFactoryMock.Setup(x => x.Create()).Returns(ruleHelpXamlTranslatorMock.Object);
            var testSubject = new RuleInfoTranslator(ruleHelpXamlTranslatorFactoryMock.Object, new TestLogger());

            var ruleDescriptionSections = testSubject.GetRuleDescriptionSections(ruleInfo).ToList();

            ruleDescriptionSections.Should().HaveCount(0);
            ruleHelpXamlTranslatorMock.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public void GetRuleDescriptionSections_CorrectlyFormsRootCauseSection()
        {
            GetRuleDescriptionSections_CorrectlyFormsSimpleContentSection<RootCauseSection>("root_cause",
                (section, xamlContent) => section.partialXamlContent.Should().Be(xamlContent));
        }

        [TestMethod]
        public void GetRuleDescriptionSections_CorrectlyFormsAssesTheProblemSection()
        {
            GetRuleDescriptionSections_CorrectlyFormsSimpleContentSection<AssesTheProblemSection>("assess_the_problem",
                (section, xamlContent) => section.partialXamlContent.Should().Be(xamlContent));
        }

        [TestMethod]
        public void GetRuleDescriptionSections_CorrectlyFormsNoContextHowToFixItSection()
        {
            GetRuleDescriptionSections_CorrectlyFormsSimpleContentSection<HowToFixItSection>("how_to_fix",
                (section, xamlContent) =>
                {
                    section.contexts.Should().BeNull();
                    section.partialXamlContent.Should().Be(xamlContent);
                });
        }

        [TestMethod]
        public void GetRuleDescriptionSections_CorrectlyFormsMultiContextHowToFixItSection()
        {
            var content1 = "<span>hello</span>";
            var content2 = "<span>hello2</span>";
            var content3 = "<span>hello3</span>";
            var ruleInfo = GetRuleInfo(new[]
            {
                new DescriptionSection("how_to_fix", content1, new Context("asp", "ASP")),
                new DescriptionSection("how_to_fix", content2, new Context("console", "CLI")),
                new DescriptionSection("how_to_fix", content3, new Context("xamarin", "Xamarin")),
            });
            var ruleHelpXamlTranslatorFactoryMock = new Mock<IRuleHelpXamlTranslatorFactory>();
            var ruleHelpXamlTranslatorMock = new Mock<IRuleHelpXamlTranslator>();
            ruleHelpXamlTranslatorFactoryMock.Setup(x => x.Create()).Returns(ruleHelpXamlTranslatorMock.Object);
            ruleHelpXamlTranslatorMock.Setup(x => x.TranslateHtmlToXaml(content1)).Returns(content1);
            ruleHelpXamlTranslatorMock.Setup(x => x.TranslateHtmlToXaml(content2)).Returns(content2);
            ruleHelpXamlTranslatorMock.Setup(x => x.TranslateHtmlToXaml(content3)).Returns(content3);
            var testSubject = new RuleInfoTranslator(ruleHelpXamlTranslatorFactoryMock.Object, new TestLogger());

            var ruleDescriptionSections = testSubject.GetRuleDescriptionSections(ruleInfo).ToList();

            ruleDescriptionSections.Should().HaveCount(1);
            ruleDescriptionSections[0].Should().BeOfType<HowToFixItSection>();
            var howToFixItSection = (HowToFixItSection)ruleDescriptionSections[0];
            howToFixItSection.partialXamlContent.Should().BeNull();
            howToFixItSection.contexts.Count.Should().Be(3);
            howToFixItSection.contexts.Select(x => x.Key).Should().BeEquivalentTo(ruleInfo.DescriptionSections.Select(x => x.Context.Key));
            howToFixItSection.contexts.Select(x => x.Title).Should().BeEquivalentTo(ruleInfo.DescriptionSections.Select(x => x.Context.DisplayName));
            howToFixItSection.contexts.Select(x => x.PartialXamlContent).Should().BeEquivalentTo(content1, content2, content3);
            ruleHelpXamlTranslatorMock.Verify(x => x.TranslateHtmlToXaml(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void GetRuleDescriptionSections_CorrectlyFormsResourcesSection()
        {
            GetRuleDescriptionSections_CorrectlyFormsSimpleContentSection<ResourcesSection>("resources",
                (section, xamlContent) =>
                {
                    section.partialXamlContent.Should().Be(xamlContent);
                    section.educationPrinciples.Should().HaveCount(2);
                });
        }

        [DataTestMethod]
        [DataRow(RootCauseSection.RuleInfoKey)]
        [DataRow(AssesTheProblemSection.RuleInfoKey)]
        [DataRow(ResourcesSection.RuleInfoKey)]
        public void GetRuleDescriptionSections_RuleInfoHasDuplicateSections_LogsWarning(string key)
        {
            var ruleInfo = GetRuleInfo(new[]
            {
                new DescriptionSection(key, ""),
                new DescriptionSection(key, ""),
                new DescriptionSection(key, "")
            });
            var ruleHelpXamlTranslatorFactoryMock = new Mock<IRuleHelpXamlTranslatorFactory>();
            ruleHelpXamlTranslatorFactoryMock.Setup(x => x.Create()).Returns(Mock.Of<IRuleHelpXamlTranslator>());
            var testLogger = new TestLogger();

            var testSubject = new RuleInfoTranslator(ruleHelpXamlTranslatorFactoryMock.Object, testLogger);

            var sections = testSubject.GetRuleDescriptionSections(ruleInfo).ToList();

            sections.Should().HaveCount(1);
            testLogger.OutputStrings.Single()
                .Should().Contain(key)
                .And.Contain(ruleInfo.FullRuleKey)
                .And.Contain("unexpected number of section items");
        }

        [TestMethod]
        public void GetRuleDescriptionSections_OrdersSectionsCorrectly()
        {
            var ruleInfo = GetRuleInfo(new IDescriptionSection[]
            {
                new DescriptionSection("assess_the_problem", "2"),
                new DescriptionSection("resources", "4"),
                new DescriptionSection("how_to_fix", "3", new Context("1", "1")),
                new DescriptionSection("root_cause", "1"),
                new DescriptionSection("how_to_fix", "3", new Context("2", "2")),
            });
            var ruleHelpXamlTranslatorFactoryMock = new Mock<IRuleHelpXamlTranslatorFactory>();
            ruleHelpXamlTranslatorFactoryMock.Setup(x => x.Create()).Returns(Mock.Of<IRuleHelpXamlTranslator>());

            var testSubject = new RuleInfoTranslator(ruleHelpXamlTranslatorFactoryMock.Object, new TestLogger());

            var sections = testSubject.GetRuleDescriptionSections(ruleInfo).ToList();

            sections.Should().HaveCount(4);
            sections[0].Should().BeOfType<RootCauseSection>();
            sections[1].Should().BeOfType<AssesTheProblemSection>();
            sections[2].Should().BeOfType<HowToFixItSection>();
            var contexts = ((HowToFixItSection)sections[2]).contexts;
            contexts.Should().HaveCount(2);
            contexts[0].Key.Should().Be("1");
            contexts[1].Key.Should().Be("2");
            sections[3].Should().BeOfType<ResourcesSection>();
        }

        private void GetRuleDescriptionSections_CorrectlyFormsSimpleContentSection<T>(string key, Action<T, string> verifyXamlContent)
        {
            var htmlContent = "<h3>hello</h3>";
            var xamlContent = "<Paragraph>hello</Paragraph>";
            var ruleInfo = GetRuleInfo(new[] { new DescriptionSection(key, htmlContent) });
            var ruleHelpXamlTranslatorFactoryMock = new Mock<IRuleHelpXamlTranslatorFactory>();
            var ruleHelpXamlTranslatorMock = new Mock<IRuleHelpXamlTranslator>();
            ruleHelpXamlTranslatorFactoryMock.Setup(x => x.Create()).Returns(ruleHelpXamlTranslatorMock.Object);
            ruleHelpXamlTranslatorMock.Setup(x => x.TranslateHtmlToXaml(htmlContent)).Returns(xamlContent);

            var testSubject = new RuleInfoTranslator(ruleHelpXamlTranslatorFactoryMock.Object, new TestLogger());

            var ruleDescriptionSections = testSubject.GetRuleDescriptionSections(ruleInfo).ToList();

            ruleDescriptionSections.Should().HaveCount(1);
            ruleDescriptionSections[0].Should().BeOfType<T>();
            verifyXamlContent((T)ruleDescriptionSections[0], xamlContent);
            ruleHelpXamlTranslatorMock.Verify(x => x.TranslateHtmlToXaml(htmlContent), Times.Once);
        }

        private IRuleInfo GetRuleInfo(IReadOnlyList<IDescriptionSection> sections)
        {
            return new RuleInfo(
                Language.CSharp.ServerLanguage.Key,
                "xxx:S123",
                "a description",
                "the rule name",
                RuleIssueSeverity.Blocker,
                RuleIssueType.Vulnerability,
                isActiveByDefault: true,
                new List<string> { "veryimportantissue" },
                sections,
                new List<string> { "think before you do something", "think again" },
                null);
        }
    }
}

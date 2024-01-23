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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Rules.UnitTests
{
    [TestClass]
    public class ServerRuleMetadataProviderTests
    {
        [TestMethod]
        public async Task GetRuleInfoAsync_RuleFound_ReturnRuleInfo()
        {
            var service = new Mock<ISonarQubeService>();

            var parameters = new Dictionary<string, string> { { "parameter Key 1", "parameter Value 1" }, { "parameter Key 2", "parameter Value 2" } };

            var descriptionSection1 = new SonarQubeDescriptionSection("key1", "Content1<br>", null);
            var descriptionSection2 = new SonarQubeDescriptionSection("key2", "Content2", new SonarQubeContext("Display1", "contextKey1"));
            var descriptionSection3 = new SonarQubeDescriptionSection("key2", "Content3", new SonarQubeContext("Display1", "contextKey1"));

            var descriptionSections = new[] { descriptionSection1, descriptionSection2, descriptionSection3 };

            var educationPrinciples = new[] { "principle1", "principle2" };

            var tags = new[] { "tag1", "tag2" };

            var sqRule = new SonarQubeRule("Key",
                "repoKey",
                true,
                SonarQubeIssueSeverity.Info,
                SonarQubeCleanCodeAttribute.Clear,
                new Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity>{{ SonarQubeSoftwareQuality.Reliability, SonarQubeSoftwareQualitySeverity.Low}},
                parameters,
                SonarQubeIssueType.Vulnerability,
                "<p>html Description</p><br>",
                descriptionSections,
                educationPrinciples,
                "RuleName",
                tags,
                "htmlNote<br>");

            service.Setup(s => s.GetRuleByKeyAsync("repoKey:Key", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(sqRule);

            var testSubject = CreateTestSubject(service.Object);

            var ruleKey = new SonarCompositeRuleId("repoKey", "Key");

            var result = await testSubject.GetRuleInfoAsync(ruleKey, "qpKey", CancellationToken.None);

            result.Should().NotBeNull();

            result.LanguageKey.Should().Be("repoKey");
            result.FullRuleKey.Should().Be("repoKey:Key");
            result.Description.Should().Be("<p>html Description</p><br/>");
            result.Name.Should().Be("RuleName");
            result.Severity.Should().Be(RuleIssueSeverity.Info);
            result.IssueType.Should().Be(RuleIssueType.Vulnerability);
            result.IsActiveByDefault.Should().BeTrue();
            result.Tags.Should().BeEquivalentTo(tags);
            result.EducationPrinciples.Should().BeEquivalentTo(educationPrinciples);
            result.HtmlNote.Should().Be("htmlNote<br/>");
            result.CleanCodeAttribute.Should().Be(CleanCodeAttribute.Clear);
            result.DefaultImpacts.Should().BeEquivalentTo(new Dictionary<SoftwareQuality, SoftwareQualitySeverity>{
                { SoftwareQuality.Reliability, SoftwareQualitySeverity.Low}});

            result.DescriptionSections.Count.Should().Be(3);

            for (int i = 0; i < 3; i++)
            {
                result.DescriptionSections[i].Key.Should().Be(descriptionSections[i].Key);
                result.DescriptionSections[i].HtmlContent.Should().Be(descriptionSections[i].HtmlContent.Replace("<br>", "<br/>"));

                if (result.DescriptionSections[i].Context == null)
                {
                    descriptionSections[i].Context.Should().BeNull();
                    continue;
                }

                result.DescriptionSections[i].Context.Key.Should().Be(descriptionSections[i].Context.Key);
                result.DescriptionSections[i].Context.DisplayName.Should().Be(descriptionSections[i].Context.DisplayName);
            }
        }

        [TestMethod] // Regression test for part of #3973
        public async Task GetRuleInfoAsync_RuleFound_ServerCollectionsFieldsAreNull_RuleInfoFieldsAreNotNull()
        {
            var service = new Mock<ISonarQubeService>();
            var sqRule = new SonarQubeRule("Key",
                "repoKey",
                true,
                SonarQubeIssueSeverity.Info,
                SonarQubeCleanCodeAttribute.Clear,
                new Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity>{{ SonarQubeSoftwareQuality.Reliability, SonarQubeSoftwareQualitySeverity.Low}},
                null, // parameters,
                SonarQubeIssueType.Vulnerability,
                "<p>html Description</p><br>",
                null, // descriptionSections
                null, // educationPrinciples
                "RuleName",
                null, // tags
                "htmlNote<br>");

            service.Setup(s => s.GetRuleByKeyAsync("repoKey:Key", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(sqRule);

            var testSubject = CreateTestSubject(service.Object);

            var ruleKey = new SonarCompositeRuleId("repoKey", "Key");

            var result = await testSubject.GetRuleInfoAsync(ruleKey, "qpKey", CancellationToken.None);

            result.Should().NotBeNull();

            result.Tags.Should().NotBeNull();
            result.DescriptionSections.Should().NotBeNull();
            result.EducationPrinciples.Should().NotBeNull();

            result.Tags.Should().HaveCount(0);
            result.DescriptionSections.Should().HaveCount(0);
            result.EducationPrinciples.Should().HaveCount(0);
        }

        [TestMethod]
        public async Task GetRuleInfoAsync_RuleNotFound_ReturnNull()
        {
            var service = new Mock<ISonarQubeService>();
            service.Setup(s => s.GetRuleByKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((SonarQubeRule)null);

            var testSubject = CreateTestSubject(service.Object);

            var ruleKey = new SonarCompositeRuleId("repoKey", "Key");

            var result = await testSubject.GetRuleInfoAsync(ruleKey, "qpKey", CancellationToken.None);

            result.Should().BeNull();
        }

        [TestMethod]
        public async Task GetRuleInfoAsync_GotError_ReturnNull()
        {
            var service = new Mock<ISonarQubeService>();
            service.Setup(s => s.GetRuleByKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Some server error"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(service.Object, logger);

            var ruleKey = new SonarCompositeRuleId("repoKey", "Key");

            var result = await testSubject.GetRuleInfoAsync(ruleKey, "qpKey", CancellationToken.None);

            result.Should().BeNull();
            logger.AssertPartialOutputStrings("Some server error");
        }

        private static ServerRuleMetadataProvider CreateTestSubject(ISonarQubeService service, ILogger logger = null)
        {
            logger ??= new TestLogger();

            return new ServerRuleMetadataProvider(service, logger);
        }
    }
}

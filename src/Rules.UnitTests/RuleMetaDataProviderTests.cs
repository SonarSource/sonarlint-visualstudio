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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Rules.UnitTests
{
    [TestClass]
    public class RuleMetaDataProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<RuleMetaDataProvider, IRuleMetaDataProvider>(
                MefTestHelpers.CreateExport<ILocalRuleMetadataProvider>(),
                MefTestHelpers.CreateExport<IServerRuleMetadataProvider>(),
                MefTestHelpers.CreateExport<IConfigurationProvider>()
                );
        }

        [TestMethod]
        public async Task GetRuleInfoAsync_UnknownLanguage_ReturnsNull()
        {
            var ruleId = new SonarCompositeRuleId("unknown", "S1000");
            var localMetaDataProvider = CreateLocalRuleMetaDataProvider(ruleId, null);

            var serverMetaDataProvider = new Mock<IServerRuleMetadataProvider>();

            var configurationProvider = CreateConfigurationProvider();

            RuleMetaDataProvider testSubject = CreateTestSubject(localMetaDataProvider, serverMetaDataProvider, configurationProvider);

            var result = await testSubject.GetRuleInfoAsync(ruleId, CancellationToken.None);

            result.Should().Be(null);

            localMetaDataProvider.Verify(l => l.GetRuleInfo(ruleId), Times.Once);
            serverMetaDataProvider.Invocations.Should().BeEmpty();
            configurationProvider.Verify(cp => cp.GetConfiguration(), Times.Once);
        }

        [TestMethod]
        public async Task GetRuleInfoAsync_NotInConnectedMode_ReturnsLocal()
        {
            var ruleId = new SonarCompositeRuleId("csharpsquid", "S1000");
            var localRule = CreateRuleInfo("csharpsquid", "csharpsquid:S1000");
            var localMetaDataProvider = CreateLocalRuleMetaDataProvider(ruleId, localRule);

            var serverMetaDataProvider = CreateServerRuleMetaDataProvider();

            var configurationProvider = CreateConfigurationProvider();

            RuleMetaDataProvider testSubject = CreateTestSubject(localMetaDataProvider, serverMetaDataProvider, configurationProvider);

            var result = await testSubject.GetRuleInfoAsync(ruleId, CancellationToken.None);

            result.Should().Be(localRule);

            localMetaDataProvider.Verify(l => l.GetRuleInfo(ruleId), Times.Once);
            serverMetaDataProvider.VerifyNoOtherCalls();
            configurationProvider.Verify(cp => cp.GetConfiguration(), Times.Once);
        }

        [TestMethod]
        public async Task GetRuleInfoAsync_QualityProfileNotFound_ReturnsLocal()
        {
            var ruleId = new SonarCompositeRuleId("csharpsquid", "S1000");
            var localRule = CreateRuleInfo("csharpsquid", "csharpsquid:S1000");
            var localMetaDataProvider = CreateLocalRuleMetaDataProvider(ruleId, localRule);

            var serverMetaDataProvider = CreateServerRuleMetaDataProvider();

            var profiles = new Dictionary<Language, ApplicableQualityProfile> { { Language.Js, new ApplicableQualityProfile { ProfileKey = "Profile Key 1" } } };
            var configurationProvider = CreateConfigurationProvider(profiles);

            RuleMetaDataProvider testSubject = CreateTestSubject(localMetaDataProvider, serverMetaDataProvider, configurationProvider);

            var result = await testSubject.GetRuleInfoAsync(ruleId, CancellationToken.None);

            result.Should().Be(localRule);

            localMetaDataProvider.Verify(l => l.GetRuleInfo(ruleId), Times.Once);
            serverMetaDataProvider.VerifyNoOtherCalls();
            configurationProvider.Verify(cp => cp.GetConfiguration(), Times.Once);
        }

        [TestMethod]
        public async Task GetRuleInfoAsync_RemoteRuleExists_OverrideSeverityAndHtmlNote()
        {
            var ruleId = new SonarCompositeRuleId("csharpsquid", "S1000");
            var localRule = CreateRuleInfo("csharpsquid", "csharpsquid:S1000", description: "Local Description");
            var localMetaDataProvider = CreateLocalRuleMetaDataProvider(ruleId, localRule);

            var serverRule = CreateRuleInfo("csharpsquid", "csharpsquid:S1000", description: "Server Description", htmlNote: "Extended Rule Description", defaultSeverity: RuleIssueSeverity.Critical);
            var serverMetaDataProvider = CreateServerRuleMetaDataProvider(ruleId, serverRule, "CSharp Profile Key");

            var profiles = new Dictionary<Language, ApplicableQualityProfile> { { Language.Js, new ApplicableQualityProfile { ProfileKey = "Profile Key 1" } }, { Language.CSharp, new ApplicableQualityProfile { ProfileKey = "CSharp Profile Key" } } };
            var configurationProvider = CreateConfigurationProvider(profiles);

            RuleMetaDataProvider testSubject = CreateTestSubject(localMetaDataProvider, serverMetaDataProvider, configurationProvider);

            var result = await testSubject.GetRuleInfoAsync(ruleId, CancellationToken.None);

            result.Should().NotBe(localRule);
            result.Severity.Should().Be(RuleIssueSeverity.Critical);
            result.HtmlNote.Should().Be("Extended Rule Description");
            result.Description.Should().Be("Local Description");

            localMetaDataProvider.Verify(l => l.GetRuleInfo(ruleId), Times.Once);
            serverMetaDataProvider.Verify(s => s.GetRuleInfoAsync(ruleId, "CSharp Profile Key", CancellationToken.None), Times.Once);
            serverMetaDataProvider.VerifyNoOtherCalls();
            configurationProvider.Verify(cp => cp.GetConfiguration(), Times.Once);
        }

        [TestMethod]
        public async Task GetRuleInfoAsync_LocalRuleDoesNotExistRemoteRuleExists_ReturnServerRule()
        {
            var ruleId = new SonarCompositeRuleId("csharpsquid", "S1000");

            var localMetaDataProvider = CreateLocalRuleMetaDataProvider();

            var serverRule = CreateRuleInfo("csharpsquid", "csharpsquid:S1000", description: "Server Description", htmlNote: "Extended Rule Description", defaultSeverity: RuleIssueSeverity.Critical);
            var serverMetaDataProvider = CreateServerRuleMetaDataProvider(ruleId, serverRule, "CSharp Profile Key");

            var profiles = new Dictionary<Language, ApplicableQualityProfile> { { Language.Js, new ApplicableQualityProfile { ProfileKey = "Profile Key 1" } }, { Language.CSharp, new ApplicableQualityProfile { ProfileKey = "CSharp Profile Key" } } };
            var configurationProvider = CreateConfigurationProvider(profiles);

            RuleMetaDataProvider testSubject = CreateTestSubject(localMetaDataProvider, serverMetaDataProvider, configurationProvider);

            var result = await testSubject.GetRuleInfoAsync(ruleId, CancellationToken.None);

            result.Should().Be(serverRule);
            result.Severity.Should().Be(RuleIssueSeverity.Critical);
            result.HtmlNote.Should().Be("Extended Rule Description");
            result.Description.Should().Be("Server Description");

            localMetaDataProvider.Verify(l => l.GetRuleInfo(ruleId), Times.Once);
            serverMetaDataProvider.Verify(s => s.GetRuleInfoAsync(ruleId, "CSharp Profile Key", CancellationToken.None), Times.Once);
            serverMetaDataProvider.VerifyNoOtherCalls();
            configurationProvider.Verify(cp => cp.GetConfiguration(), Times.Once);
        }

        #region Helper Methods

        private static Mock<IServerRuleMetadataProvider> CreateServerRuleMetaDataProvider(SonarCompositeRuleId ruleId = null, IRuleInfo serverRule = null, string qualityProfile = null)
        {
            var serverMetaDataProvider = new Mock<IServerRuleMetadataProvider>();
            serverMetaDataProvider.Setup(s => s.GetRuleInfoAsync(It.IsAny<SonarCompositeRuleId>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((IRuleInfo)null);
            serverMetaDataProvider.Setup(s => s.GetRuleInfoAsync(ruleId, qualityProfile, It.IsAny<CancellationToken>())).ReturnsAsync(serverRule);
            return serverMetaDataProvider;
        }

        private static Mock<IConfigurationProvider> CreateConfigurationProvider(Dictionary<Language, ApplicableQualityProfile> profiles = null)
        {
            var configurationProvider = new Mock<IConfigurationProvider>();

            if (profiles == null)
            {
                configurationProvider.Setup(cp => cp.GetConfiguration()).Returns(BindingConfiguration.Standalone);
            }
            else
            {
                var project = new BoundSonarQubeProject { Profiles = profiles };
                var config = new BindingConfiguration(project, SonarLintMode.Connected, "dir");
                configurationProvider.Setup(cp => cp.GetConfiguration()).Returns(config);
            }
            return configurationProvider;
        }

        private static RuleMetaDataProvider CreateTestSubject(Mock<ILocalRuleMetadataProvider> localMetaDataProvider, Mock<IServerRuleMetadataProvider> serverMetaDataProvider, Mock<IConfigurationProvider> configurationProvider)
        {
            return new RuleMetaDataProvider(localMetaDataProvider.Object, serverMetaDataProvider.Object, configurationProvider.Object);
        }

        private static Mock<ILocalRuleMetadataProvider> CreateLocalRuleMetaDataProvider(SonarCompositeRuleId ruleId = null, IRuleInfo localRule = null)
        {
            var localMetaDataProvider = new Mock<ILocalRuleMetadataProvider>();
            localMetaDataProvider.Setup(l => l.GetRuleInfo(It.IsAny<SonarCompositeRuleId>())).Returns<IRuleInfo>(null);
            localMetaDataProvider.Setup(l => l.GetRuleInfo(ruleId)).Returns(localRule);
            return localMetaDataProvider;
        }

        private static IRuleInfo CreateRuleInfo(
            string languageKey,
            string fullRuleKey,
            string description = "Description",
            string name = "Name",
            RuleIssueSeverity defaultSeverity = RuleIssueSeverity.Unknown,
            RuleIssueType issueType = RuleIssueType.Unknown,
            bool isActiveByDefault = true,
            IReadOnlyList<string> tags = null,
            IReadOnlyList<IDescriptionSection> descriptionSections = null,
            IReadOnlyList<string> educationPrinciples = null,
            string htmlNote = null)
        {
            return new RuleInfo(languageKey, fullRuleKey, description, name, defaultSeverity, issueType, isActiveByDefault, tags, descriptionSections, educationPrinciples, htmlNote);
        }

        #endregion
    }
}

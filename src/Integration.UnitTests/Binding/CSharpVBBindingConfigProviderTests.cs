/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;
using NuGetPackageInfo = SonarLint.VisualStudio.Core.CSharpVB.NuGetPackageInfo;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class CSharpVBBindingConfigProviderTests
    {
        private const string OriginalValidRuleSetDescription = "my ruleset";

        private IList<SonarQubeRule> emptyRules;
        private IList<SonarQubeRule> validRules;
        private IList<SonarQubeProperty> anyProperties;
        private SonarQubeQualityProfile validQualityProfile;
        private IList<NuGetPackageInfo> validNugetPackages;
        private Core.CSharpVB.RuleSet validRuleSet;

        private static readonly SonarQubeRule ActiveRuleWithUnsupportedSeverity = new SonarQubeRule("activeHotspot", "any1",
            true, SonarQubeIssueSeverity.Blocker, null, SonarQubeIssueType.SecurityHotspot);

        private static readonly SonarQubeRule InactiveRuleWithUnsupportedSeverity = new SonarQubeRule("inactiveHotspot", "any2",
            false, SonarQubeIssueSeverity.Blocker, null, SonarQubeIssueType.SecurityHotspot);

        private static readonly SonarQubeRule ActiveTaintAnalysisRule = new SonarQubeRule("activeTaint", "roslyn.sonaranalyzer.security.foo",
            true, SonarQubeIssueSeverity.Blocker, null, SonarQubeIssueType.CodeSmell);

        private static readonly SonarQubeRule InactiveTaintAnalysisRule = new SonarQubeRule("inactiveTaint", "roslyn.sonaranalyzer.security.bar",
            false, SonarQubeIssueSeverity.Blocker, null, SonarQubeIssueType.CodeSmell);


        [TestInitialize]
        public void TestInitialize()
        {
            emptyRules = Array.Empty<SonarQubeRule>();

            validRules = new List<SonarQubeRule>
            {
                new SonarQubeRule("key", "repoKey", true, SonarQubeIssueSeverity.Blocker, null, SonarQubeIssueType.Bug)
            };

            anyProperties = Array.Empty<SonarQubeProperty>();

            validQualityProfile = new SonarQubeQualityProfile("qpkey1", "qp name", "any", false, DateTime.UtcNow);

            validNugetPackages = new List<NuGetPackageInfo> {new NuGetPackageInfo("package.id", "1.2")};

            validRuleSet = new Core.CSharpVB.RuleSet
            {
                Description = OriginalValidRuleSetDescription,
                ToolsVersion = "12.0",
                Rules = new List<Core.CSharpVB.Rules>
                {
                    new Core.CSharpVB.Rules
                    {
                        AnalyzerId = "my analyzer", RuleNamespace = "my namespace"
                    }
                }
            };
        }

        [TestMethod]
        public void GetRules_UnsupportedLanguage_Throws()
        {
            var builder = new TestEnvironmentBuilder(validQualityProfile, Language.Cpp);
            var testSubject = builder.CreateTestSubject();

            // Act
            Action act = () => testSubject.GetConfigurationAsync(validQualityProfile, Language.Cpp, BindingConfiguration.Standalone, CancellationToken.None).Wait();

            // Assert
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("language");
        }

        [TestMethod]
        public void IsLanguageSupported()
        {
            // Arrange
            var builder = new TestEnvironmentBuilder(validQualityProfile, Language.Cpp);
            var testSubject = builder.CreateTestSubject();

            // 1. Supported languages
            testSubject.IsLanguageSupported(Language.CSharp).Should().BeTrue();
            testSubject.IsLanguageSupported(Language.VBNET).Should().BeTrue();

            // 2. Not supported
            testSubject.IsLanguageSupported(Language.C).Should().BeFalse();
            testSubject.IsLanguageSupported(Language.Cpp).Should().BeFalse();

            testSubject.IsLanguageSupported(Language.Unknown).Should().BeFalse();
        }

        [TestMethod]
        public async Task GetConfig_NoSupportedActiveRules_ReturnsNull()
        {
            // Arrange
            var builder = new TestEnvironmentBuilder(validQualityProfile, Language.VBNET)
            {
                ActiveRulesResponse = new List<SonarQubeRule> { ActiveRuleWithUnsupportedSeverity },
                InactiveRulesResponse = validRules,
                PropertiesResponse = anyProperties
            };
            var testSubject = builder.CreateTestSubject();

            // Act
            var result = await testSubject.GetConfigurationAsync(validQualityProfile, Language.VBNET, builder.BindingConfiguration, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().BeNull();

            builder.Logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.NoSonarAnalyzerActiveRulesForQualityProfile, validQualityProfile.Name, Language.VBNET.Name));
            builder.Logger.AssertOutputStrings(expectedOutput);

            builder.AssertRuleSetGeneratorNotCalled();
            builder.AssertNuGetGeneratorNotCalled();
            builder.AssertNuGetBindingWasNotCalled();
        }

        [TestMethod]
        public async Task GetConfig_ReturnsCorrectRuleset()
        {
            var builder = new TestEnvironmentBuilder(validQualityProfile, Language.VBNET)
            {
                ActiveRulesResponse = validRules,
                InactiveRulesResponse = emptyRules,
                PropertiesResponse = anyProperties,
                NuGetBindingOperationResponse = true,
                RuleSetGeneratorResponse = validRuleSet,
            };
            var testSubject = builder.CreateTestSubject();

            var expectedRulesetFilePath = builder.BindingConfiguration.BuildPathUnderConfigDirectory(Language.VBNET.FileSuffixAndExtension);

            var response = await testSubject.GetConfigurationAsync(validQualityProfile, Language.VBNET, builder.BindingConfiguration, CancellationToken.None);
            (response as ICSharpVBBindingConfig).RuleSet.Path.Should().Be(expectedRulesetFilePath);
            (response as ICSharpVBBindingConfig).RuleSet.Content.Description.Should().Be(validRuleSet.Description);
            (response as ICSharpVBBindingConfig).RuleSet.Content.Rules.Should().AllBeEquivalentTo(validRuleSet.Rules);
            (response as ICSharpVBBindingConfig).RuleSet.Content.ToolsVersion.ToString().Should().Be(validRuleSet.ToolsVersion);
        }

        [TestMethod]
        public async Task GetConfig_ReturnsCorrectAdditionalFile()
        {
            var expectedConfiguration = new SonarLintConfiguration
            {
                Rules = new List<SonarLintRule>
                {
                    new SonarLintRule
                    {
                        Key = "test",
                        Parameters = new List<SonarLintKeyValuePair> {new SonarLintKeyValuePair {Key = "ruleid", Value = "value"}}
                    }
                }
            };

            var builder = new TestEnvironmentBuilder(validQualityProfile, Language.VBNET)
            {
                ActiveRulesResponse = validRules,
                InactiveRulesResponse = emptyRules,
                PropertiesResponse = anyProperties,
                NuGetBindingOperationResponse = true,
                RuleSetGeneratorResponse = validRuleSet,
                SonarLintConfigurationResponse = expectedConfiguration
            };
            var testSubject = builder.CreateTestSubject();

            var response = await testSubject.GetConfigurationAsync(validQualityProfile, Language.VBNET, builder.BindingConfiguration, CancellationToken.None);
            (response as ICSharpVBBindingConfig).AdditionalFile.Path.Should().Be("expected_additional_file_directory\\VB\\SonarLint.xml");
            (response as ICSharpVBBindingConfig).AdditionalFile.Content.Should().Be(expectedConfiguration);
        }

        [TestMethod]
        public async Task GetConfig_NuGetBindingOperationFails_ReturnsNull()
        {
            var builder = new TestEnvironmentBuilder(validQualityProfile, Language.VBNET)
            {
                ActiveRulesResponse = validRules,
                InactiveRulesResponse = validRules,
                PropertiesResponse = anyProperties,
                NuGetGeneratorResponse = validNugetPackages,
                NuGetBindingOperationResponse = false
            };

            var testSubject = builder.CreateTestSubject();

            var result = await testSubject.GetConfigurationAsync(validQualityProfile, Language.VBNET, builder.BindingConfiguration, CancellationToken.None)
                .ConfigureAwait(false);

            result.Should().BeNull();

            builder.AssertNuGetGeneratorWasCalled();
            builder.AssertNuGetBindingOpWasCalled();

            builder.AssertRuleSetGeneratorNotCalled();
        }

        [TestMethod]
        public async Task GetConfig_HasActiveInactiveAndUnsupportedRules_ReturnsValidBindingConfig()
        {
            // Arrange
            const string expectedProjectName = "my project";
            const string expectedServerUrl = "http://myhost:123/";

            var properties = new SonarQubeProperty[]
            {
                new SonarQubeProperty("propertyAAA", "111"), new SonarQubeProperty("propertyBBB", "222")
            };

            var activeRules = new SonarQubeRule[]
                {
                    CreateRule("activeRuleKey", "repoKey1", true),
                    ActiveTaintAnalysisRule,
                    ActiveRuleWithUnsupportedSeverity
                };

            var inactiveRules = new SonarQubeRule[]
                {
                    CreateRule("inactiveRuleKey", "repoKey2", false),
                    InactiveTaintAnalysisRule,
                    InactiveRuleWithUnsupportedSeverity
                };

            var builder = new TestEnvironmentBuilder(validQualityProfile, Language.CSharp, expectedProjectName, expectedServerUrl)
            {
                ActiveRulesResponse = activeRules,
                InactiveRulesResponse = inactiveRules,
                PropertiesResponse = properties,
                NuGetBindingOperationResponse = true,
                RuleSetGeneratorResponse = validRuleSet
            };

            var testSubject = builder.CreateTestSubject();

            // Act
            var result = await testSubject.GetConfigurationAsync(validQualityProfile, Language.CSharp, builder.BindingConfiguration, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<CSharpVBBindingConfig>();
            var dotNetResult = (CSharpVBBindingConfig)result;
            dotNetResult.RuleSet.Should().NotBeNull();
            dotNetResult.RuleSet.Content.ToolsVersion.Should().Be(new Version(validRuleSet.ToolsVersion));

            var expectedName = string.Format(Strings.SonarQubeRuleSetNameFormat, expectedProjectName, validQualityProfile.Name);
            dotNetResult.RuleSet.Content.DisplayName.Should().Be(expectedName);

            var expectedDescription = $"{OriginalValidRuleSetDescription} {string.Format(Strings.SonarQubeQualityProfilePageUrlFormat, expectedServerUrl, validQualityProfile.Key)}";
            dotNetResult.RuleSet.Content.Description.Should().Be(expectedDescription);

            // Check properties passed to the ruleset generator
            builder.CapturedPropertiesPassedToRuleSetGenerator.Should().NotBeNull();
            var capturedProperties = builder.CapturedPropertiesPassedToRuleSetGenerator.ToList();
            capturedProperties.Count.Should().Be(2);
            capturedProperties[0].Key.Should().Be("propertyAAA");
            capturedProperties[0].Value.Should().Be("111");
            capturedProperties[1].Key.Should().Be("propertyBBB");
            capturedProperties[1].Value.Should().Be("222");

            // Check both active and inactive rules were passed to the ruleset generator.
            // The unsupported rules should have been removed.
            builder.CapturedRulesPassedToRuleSetGenerator.Should().NotBeNull();
            var capturedRules = builder.CapturedRulesPassedToRuleSetGenerator.ToList();
            capturedRules.Count.Should().Be(2);
            capturedRules[0].Key.Should().Be("activeRuleKey");
            capturedRules[0].RepositoryKey.Should().Be("repoKey1");
            capturedRules[1].Key.Should().Be("inactiveRuleKey");
            capturedRules[1].RepositoryKey.Should().Be("repoKey2");

            builder.AssertNuGetBindingOpWasCalled();
            builder.Logger.AssertOutputStrings(0); // not expecting anything in the case of success
        }

        [TestMethod]
        [DataRow("roslyn.sonaranalyzer.security.cs", false)]
        [DataRow("roslyn.sonaranalyzer.security.vb", false)]
        [DataRow("ROSLYN.SONARANALYZER.SECURITY.X", false)]
        [DataRow("roslyn.wintellect", true)]
        [DataRow("sonaranalyzer-cs", true)]
        [DataRow("sonaranalyzer-vbnet", true)]
        public void IsSupportedRule_TaintRules(string repositoryKey, bool expected)
        {
            var rule = CreateRule("any", repositoryKey, true);

            CSharpVBBindingConfigProvider.IsSupportedRule(rule).Should().Be(expected);
        }

        [TestMethod]
        [DataRow(SonarQubeIssueType.Unknown, false)]
        [DataRow(SonarQubeIssueType.SecurityHotspot, false)]
        [DataRow(SonarQubeIssueType.CodeSmell, true)]
        [DataRow(SonarQubeIssueType.Bug, true)]
        [DataRow(SonarQubeIssueType.Vulnerability, true)]
        public void IsSupportedRule_Severity(SonarQubeIssueType issueType, bool expected)
        {
            var rule = new SonarQubeRule("any", "any", true, SonarQubeIssueSeverity.Blocker, null, issueType);

            CSharpVBBindingConfigProvider.IsSupportedRule(rule).Should().Be(expected);
        }

        private static SonarQubeRule CreateRule(string ruleKey, string repoKey, bool isActive) =>
            new SonarQubeRule(ruleKey, repoKey, isActive, SonarQubeIssueSeverity.Blocker, null, SonarQubeIssueType.CodeSmell);

        private class TestEnvironmentBuilder
        {
            private Mock<ISonarLintConfigGenerator> sonarLintConfigGeneratorMock;
            private Mock<ISonarQubeService> sonarQubeServiceMock;
            private Mock<Core.CSharpVB.IRuleSetGenerator> ruleGenMock;
            private Mock<Core.CSharpVB.INuGetPackageInfoGenerator> nugetGenMock;
            private Mock<INuGetBindingOperation> nugetBindingMock;

            private readonly SonarQubeQualityProfile profile;
            private readonly Language language;
            private readonly string projectName;
            private readonly string serverUrl;

            private const string ExpectedProjectKey = "fixed.project.key";

            public TestEnvironmentBuilder(SonarQubeQualityProfile profile, Language language,
                string projectName = "any", string serverUrl = "http://any")
            {
                this.profile = profile;
                this.language = language;
                this.projectName = projectName;
                this.serverUrl = serverUrl;

                Logger = new TestLogger();
                SonarLintConfigurationResponse = new SonarLintConfiguration();
                PropertiesResponse = new List<SonarQubeProperty>();
            }

            public BindingConfiguration BindingConfiguration { get; set; }
            public SonarLintConfiguration SonarLintConfigurationResponse { get; set; }
            public IList<SonarQubeRule> ActiveRulesResponse { get; set; }
            public IList<SonarQubeRule> InactiveRulesResponse { get; set; }
            public IList<SonarQubeProperty> PropertiesResponse { get; set; }
            public IList<NuGetPackageInfo> NuGetGeneratorResponse { get; set; }
            public bool NuGetBindingOperationResponse { get; set; }
            public Core.CSharpVB.RuleSet RuleSetGeneratorResponse { get; set; }
            public TestLogger Logger { get; private set; }
            public IEnumerable<SonarQubeRule> CapturedRulesPassedToRuleSetGenerator { get; private set; }
            public IDictionary<string, string> CapturedPropertiesPassedToRuleSetGenerator { get; private set; }

            public CSharpVBBindingConfigProvider CreateTestSubject()
            {
                // Note: where possible, the mocked methods are set up with the expected
                // parameter values i.e. they will only be called if the correct values
                // are passed in.
                Logger = new TestLogger();

                sonarQubeServiceMock = new Mock<ISonarQubeService>();
                sonarQubeServiceMock
                    .Setup(x => x.GetRulesAsync(true, profile.Key, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ActiveRulesResponse);

                sonarQubeServiceMock
                    .Setup(x => x.GetRulesAsync(false, profile.Key, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(InactiveRulesResponse);

                sonarQubeServiceMock
                    .Setup(x => x.GetAllPropertiesAsync(ExpectedProjectKey, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(PropertiesResponse);

                ruleGenMock = new Mock<Core.CSharpVB.IRuleSetGenerator>();
                ruleGenMock.Setup(x => x.Generate(language.ServerLanguage.Key, It.IsAny<IEnumerable<SonarQubeRule>>(), It.IsAny<IDictionary<string, string>>()))
                    .Returns(RuleSetGeneratorResponse)
                    .Callback((string lang, IEnumerable<SonarQubeRule> rules, IDictionary<string, string> properties) =>
                    {
                        CapturedRulesPassedToRuleSetGenerator = rules;
                        CapturedPropertiesPassedToRuleSetGenerator = properties;
                    });

                nugetGenMock = new Mock<Core.CSharpVB.INuGetPackageInfoGenerator>();
                nugetGenMock.Setup(x => x.GetNuGetPackageInfos(It.IsAny<IList<SonarQubeRule>>(), It.IsAny<IDictionary<string, string>>()))
                    .Returns(NuGetGeneratorResponse);

                nugetBindingMock = new Mock<INuGetBindingOperation>();
                nugetBindingMock.Setup(x => x.ProcessExport(language, NuGetGeneratorResponse))
                    .Returns(NuGetBindingOperationResponse);

                var bindingRootFolder = "c:\\test\\";

                BindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(new Uri(serverUrl), ExpectedProjectKey, projectName),
                    SonarLintMode.Connected, bindingRootFolder);

                var sonarProperties = PropertiesResponse.ToDictionary(x => x.Key, y => y.Value);
                sonarLintConfigGeneratorMock
                    .Setup(x => x.Generate(ActiveRulesResponse, sonarProperties, language))
                    .Returns(SonarLintConfigurationResponse);

                return new CSharpVBBindingConfigProvider(sonarQubeServiceMock.Object, nugetBindingMock.Object, Logger,
                    // inject the generator mocks
                    ruleGenMock.Object,
                    nugetGenMock.Object,
                    sonarLintConfigGeneratorMock.Object);
            }

            public void AssertRuleSetGeneratorNotCalled()
            {
                ruleGenMock.Verify(x => x.Generate(It.IsAny<string>(), It.IsAny<IEnumerable<SonarQubeRule>>(), It.IsAny<IDictionary<string, string>>()),
                    Times.Never);
            }

            public void AssertNuGetGeneratorNotCalled()
            {
                nugetGenMock.Verify(x => x.GetNuGetPackageInfos(It.IsAny<IEnumerable<SonarQubeRule>>(), It.IsAny<IDictionary<string, string>>()),
                    Times.Never);
            }

            public void AssertNuGetGeneratorWasCalled()
            {
                nugetGenMock.Verify(x => x.GetNuGetPackageInfos(It.IsAny<IEnumerable<SonarQubeRule>>(), It.IsAny<IDictionary<string, string>>()),
                    Times.Once);
            }

            public void AssertNuGetBindingWasNotCalled()
            {
                nugetBindingMock.Verify(x => x.ProcessExport(It.IsAny<Language>(), It.IsAny<IEnumerable<NuGetPackageInfo>>()),
                    Times.Never);
            }
            public void AssertNuGetBindingOpWasCalled()
            {
                nugetBindingMock.Verify(x => x.ProcessExport(It.IsAny<Language>(), It.IsAny<IEnumerable<NuGetPackageInfo>>()),
                    Times.Once);
            }
        }
    }
}

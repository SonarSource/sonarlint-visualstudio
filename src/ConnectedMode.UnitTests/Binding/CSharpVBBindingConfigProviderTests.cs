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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarQube.Client;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.UnitTests
{
    [TestClass]
    public class CSharpVBBindingConfigProviderTests
    {
        private IList<SonarQubeRule> emptyRules;
        private IList<SonarQubeRule> validRules;
        private IList<SonarQubeProperty> anyProperties;
        private SonarQubeQualityProfile validQualityProfile;

        private static readonly SonarQubeRule ActiveRuleWithUnsupportedSeverity = new SonarQubeRule("activeHotspot", "any1",
            true, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.SecurityHotspot, null, null, null, null, null, null);

        private static readonly SonarQubeRule InactiveRuleWithUnsupportedSeverity = new SonarQubeRule("inactiveHotspot", "any2",
            false, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.SecurityHotspot, null, null, null, null, null, null);

        private static readonly SonarQubeRule ActiveTaintAnalysisRule = new SonarQubeRule("activeTaint", "roslyn.sonaranalyzer.security.foo",
            true, SonarQubeIssueSeverity.Blocker,  null, null,null, SonarQubeIssueType.CodeSmell, null, null, null, null, null, null);

        private static readonly SonarQubeRule InactiveTaintAnalysisRule = new SonarQubeRule("inactiveTaint", "roslyn.sonaranalyzer.security.bar",
            false, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.CodeSmell, null, null, null, null, null, null);

        [TestInitialize]
        public void TestInitialize()
        {
            emptyRules = Array.Empty<SonarQubeRule>();

            validRules = new List<SonarQubeRule>
            {
                new SonarQubeRule("key", "repoKey", true, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.Bug, null, null, null, null, null, null)
            };

            anyProperties = Array.Empty<SonarQubeProperty>();

            validQualityProfile = new SonarQubeQualityProfile("qpkey1", "qp name", "any", false, DateTime.UtcNow);
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
            var expectedOutput = string.Format(BindingStrings.SubTextPaddingFormat,
                string.Format(BindingStrings.NoSonarAnalyzerActiveRulesForQualityProfile, validQualityProfile.Name, Language.VBNET.Name));
            builder.Logger.AssertOutputStrings(expectedOutput);

            builder.AssertGlobalConfigGeneratorNotCalled();
        }

        [TestMethod]
        public async Task GetConfig_ReturnsCorrectGlobalConfig()
        {
            var builder = new TestEnvironmentBuilder(validQualityProfile, Language.VBNET)
            {
                ActiveRulesResponse = validRules,
                InactiveRulesResponse = emptyRules,
                PropertiesResponse = anyProperties,
                GlobalConfigGeneratorResponse = "globalConfig"
            };
            var testSubject = builder.CreateTestSubject();

            var expectedGlobalConfigFilePath = builder.BindingConfiguration.BuildPathUnderConfigDirectory(Language.VBNET.FileSuffixAndExtension);

            var response = await testSubject.GetConfigurationAsync(validQualityProfile, Language.VBNET, builder.BindingConfiguration, CancellationToken.None);

            response.Should().BeAssignableTo<ICSharpVBBindingConfig>();

            var actualGlobalConfig = ((ICSharpVBBindingConfig)response).GlobalConfig;
            actualGlobalConfig.Path.Should().Be(expectedGlobalConfigFilePath);
            actualGlobalConfig.Content.Should().Be("globalConfig");
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
                GlobalConfigGeneratorResponse = "globalConfig",
                SonarLintConfigurationResponse = expectedConfiguration
            };
            var testSubject = builder.CreateTestSubject();

            var expectedAdditionalFilePath = builder.BindingConfiguration.BuildPathUnderConfigDirectory() + "VB\\SonarLint.xml";

            var response = await testSubject.GetConfigurationAsync(validQualityProfile, Language.VBNET, builder.BindingConfiguration, CancellationToken.None);
            (response as ICSharpVBBindingConfig).AdditionalFile.Path.Should().Be(expectedAdditionalFilePath);
            (response as ICSharpVBBindingConfig).AdditionalFile.Content.Should().Be(expectedConfiguration);
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
                GlobalConfigGeneratorResponse = "globalConfig"
            };

            var testSubject = builder.CreateTestSubject();

            // Act
            var result = await testSubject.GetConfigurationAsync(validQualityProfile, Language.CSharp, builder.BindingConfiguration, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<CSharpVBBindingConfig>();
            var dotNetResult = (CSharpVBBindingConfig)result;
            dotNetResult.GlobalConfig.Should().NotBeNull();

            // Check both active and inactive rules were passed to the GlobalConfig generator.
            // The unsupported rules should have been removed.
            builder.CapturedRulesPassedToGlobalConfigGenerator.Should().NotBeNull();
            var capturedRules = builder.CapturedRulesPassedToGlobalConfigGenerator.ToList();
            capturedRules.Should().HaveCount(2);
            capturedRules[0].Key.Should().Be("activeRuleKey");
            capturedRules[0].RepositoryKey.Should().Be("repoKey1");
            capturedRules[1].Key.Should().Be("inactiveRuleKey");
            capturedRules[1].RepositoryKey.Should().Be("repoKey2");

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
            var rule = new SonarQubeRule("any", "any", true, SonarQubeIssueSeverity.Blocker, null, null, null, issueType, null, null, null, null, null, null);

            CSharpVBBindingConfigProvider.IsSupportedRule(rule).Should().Be(expected);
        }

        private static SonarQubeRule CreateRule(string ruleKey, string repoKey, bool isActive) =>
            new SonarQubeRule(ruleKey, repoKey, isActive, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.CodeSmell, null, null, null, null, null, null);

        private class TestEnvironmentBuilder
        {
            private readonly Mock<ISonarLintConfigGenerator> sonarLintConfigGeneratorMock = new Mock<ISonarLintConfigGenerator>();
            private Mock<ISonarQubeService> sonarQubeServiceMock;
            private Mock<IGlobalConfigGenerator> globalConfigGenMock;

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

            public BindingConfiguration BindingConfiguration { get; private set; }
            public SonarLintConfiguration SonarLintConfigurationResponse { get; set; }
            public IList<SonarQubeRule> ActiveRulesResponse { get; set; }
            public IList<SonarQubeRule> InactiveRulesResponse { get; set; }
            public IList<SonarQubeProperty> PropertiesResponse { get; set; }
            public string GlobalConfigGeneratorResponse { get; set; }
            public TestLogger Logger { get; private set; }
            public IEnumerable<SonarQubeRule> CapturedRulesPassedToGlobalConfigGenerator { get; private set; }

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

                var serverExclusionsResponse = new ServerExclusions(
                    exclusions: new[] { "path1" },
                    globalExclusions: new[] { "path2" },
                    inclusions: new[] { "path3" });

                sonarQubeServiceMock
                    .Setup(x => x.GetServerExclusions(ExpectedProjectKey, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(serverExclusionsResponse);

                globalConfigGenMock = new Mock<IGlobalConfigGenerator>();
                globalConfigGenMock.Setup(x => x.Generate(It.IsAny<IEnumerable<SonarQubeRule>>()))
                    .Returns(GlobalConfigGeneratorResponse)
                    .Callback((IEnumerable<SonarQubeRule> rules) =>
                    {
                        CapturedRulesPassedToGlobalConfigGenerator = rules;
                    });

                BindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(new Uri(serverUrl), ExpectedProjectKey, projectName),
                    SonarLintMode.Connected, "c:\\test\\");

                var sonarProperties = PropertiesResponse.ToDictionary(x => x.Key, y => y.Value);
                sonarLintConfigGeneratorMock
                    .Setup(x => x.Generate(It.IsAny<IEnumerable<SonarQubeRule>>(), sonarProperties, serverExclusionsResponse, language))
                    .Returns(SonarLintConfigurationResponse);

                return new CSharpVBBindingConfigProvider(sonarQubeServiceMock.Object, Logger,
                    // inject the generator mocks
                    globalConfigGenMock.Object,
                    sonarLintConfigGeneratorMock.Object);
            }

            public void AssertGlobalConfigGeneratorNotCalled()
            {
                globalConfigGenMock.Verify(x => x.Generate(It.IsAny<IEnumerable<SonarQubeRule>>()), Times.Never);
            }
        }
    }
}

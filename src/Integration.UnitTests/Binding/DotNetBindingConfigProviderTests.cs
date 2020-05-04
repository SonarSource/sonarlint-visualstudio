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
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;
using NuGetPackageInfo = SonarLint.VisualStudio.Core.CSharpVB.NuGetPackageInfo;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class DotNetBindingConfigProviderTests
    {
        private static readonly IList<SonarQubeRule> EmptyRules = Array.Empty<SonarQubeRule>();
        private static readonly IList<SonarQubeRule> ValidRules = new List<SonarQubeRule>
            { new SonarQubeRule("key", "repoKey", true, SonarQubeIssueSeverity.Blocker, null) };

        private static readonly IList<SonarQubeProperty> AnyProperties = Array.Empty<SonarQubeProperty>();

        private static readonly SonarQubeQualityProfile ValidQualityProfile
            = new SonarQubeQualityProfile("qpkey1", "qp name", "any", false, DateTime.UtcNow);

        private static readonly IList<NuGetPackageInfo> ValidNugetPackages
            = new List<NuGetPackageInfo> { new NuGetPackageInfo("package.id", "1.2") };

        private const string OriginalValidRuleSetDescription = "my ruleset";
        private static readonly Core.CSharpVB.RuleSet ValidRuleSet
            = new Core.CSharpVB.RuleSet()
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

        [TestMethod]
        public void GetRules_UnsupportedLanguage_Throws()
        {
            var builder = new TestEnvironmentBuilder(ValidQualityProfile, Language.Cpp);
            var testSubject = builder.CreateTestSubject();

            // Act
            Action act = () => testSubject.GetConfigurationAsync(ValidQualityProfile, Language.Cpp, CancellationToken.None).Wait();

            // Assert
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("language");
        }

        [TestMethod]
        public void IsSupported()
        {
            // Arrange
            var builder = new TestEnvironmentBuilder(ValidQualityProfile, Language.Cpp);
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
        public async Task GetConfig_NoActiveRules_ReturnsNull()
        {
            // Arrange
            var builder = new TestEnvironmentBuilder(ValidQualityProfile, Language.VBNET)
            {
                ActiveRulesResponse = EmptyRules,
                InactiveRulesResponse = ValidRules,
                PropertiesResponse = AnyProperties
            };
            var testSubject = builder.CreateTestSubject();

            // Act
            var result = await testSubject.GetConfigurationAsync(ValidQualityProfile, Language.VBNET, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().BeNull();

            builder.Logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.NoSonarAnalyzerActiveRulesForQualityProfile, ValidQualityProfile.Name, Language.VBNET.Name));
            builder.Logger.AssertOutputStrings(expectedOutput);

            builder.AssertRuleSetGeneratorNotCalled();
            builder.AssertNuGetGeneratorNotCalled();
            builder.AssertNuGetBindingWasNotCalled();
        }

        [TestMethod]
        public async Task GetConfig_ReturnsCorrectFilePath()
        {
            var builder = new TestEnvironmentBuilder(ValidQualityProfile, Language.VBNET)
            {
                ActiveRulesResponse = ValidRules,
                InactiveRulesResponse = EmptyRules,
                PropertiesResponse = AnyProperties,
                NuGetBindingOperationResponse = true,
                RuleSetGeneratorResponse = ValidRuleSet,
                FilePathResponse = "expected file path"
            };
            var testSubject = builder.CreateTestSubject();
           
            var response = await testSubject.GetConfigurationAsync(ValidQualityProfile, Language.VBNET, CancellationToken.None);
            response.FilePath.Should().Be("expected file path");
        }

        [TestMethod]
        public async Task GetConfig_NuGetBindingOperationFails_ReturnsNull()
        {
            var builder = new TestEnvironmentBuilder(ValidQualityProfile, Language.VBNET)
            {
                ActiveRulesResponse = ValidRules,
                InactiveRulesResponse = ValidRules,
                PropertiesResponse = AnyProperties,
                NuGetGeneratorResponse = ValidNugetPackages,
                NuGetBindingOperationResponse = false
            };

            var testSubject = builder.CreateTestSubject();

            var result = await testSubject.GetConfigurationAsync(ValidQualityProfile, Language.VBNET, CancellationToken.None)
                .ConfigureAwait(false);

            result.Should().BeNull();

            builder.AssertNuGetGeneratorWasCalled();
            builder.AssertNuGetBindingOpWasCalled();

            builder.AssertRuleSetGeneratorNotCalled();
        }

        [TestMethod]
        public async Task GetConfig_HasActiveAndInactiveRules_ReturnsValidBindingConfig()
        {
            // Arrange
            const string expectedProjectName = "my project";
            const string expectedServerUrl = "http://myhost:123/";

            var properties = new SonarQubeProperty[]
            {
                new SonarQubeProperty("propertyAAA", "111"), new SonarQubeProperty("propertyBBB", "222")
            };

            var activeRules = new SonarQubeRule[] { CreateRule("activeRuleKey", "repoKey1", true) };
            var inactiveRules = new SonarQubeRule[] { CreateRule("inactiveRuleKey", "repoKey2", false) };

            var builder = new TestEnvironmentBuilder(ValidQualityProfile, Language.CSharp, expectedProjectName, expectedServerUrl)
            {
                ActiveRulesResponse = activeRules,
                InactiveRulesResponse = inactiveRules,
                PropertiesResponse = properties,
                NuGetBindingOperationResponse = true,
                RuleSetGeneratorResponse = ValidRuleSet
            };

            var testSubject = builder.CreateTestSubject();

            // Act
            var result = await testSubject.GetConfigurationAsync(ValidQualityProfile, Language.CSharp, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<DotNetBindingConfigFile>();
            var dotNetResult = (DotNetBindingConfigFile)result;
            dotNetResult.RuleSet.Should().NotBeNull();
            dotNetResult.RuleSet.ToolsVersion.Should().Be(new Version(ValidRuleSet.ToolsVersion));

            var expectedName = string.Format(Strings.SonarQubeRuleSetNameFormat, expectedProjectName, ValidQualityProfile.Name);
            dotNetResult.RuleSet.DisplayName.Should().Be(expectedName);

            var expectedDescription = $"{OriginalValidRuleSetDescription} {string.Format(Strings.SonarQubeQualityProfilePageUrlFormat, expectedServerUrl, ValidQualityProfile.Key)}";
            dotNetResult.RuleSet.Description.Should().Be(expectedDescription);

            // Check properties passed to the ruleset generator
            builder.CapturedPropertiesPassedToRuleSetGenerator.Should().NotBeNull();
            var capturedProperties = builder.CapturedPropertiesPassedToRuleSetGenerator.ToList();
            capturedProperties.Count.Should().Be(2);
            capturedProperties[0].Key.Should().Be("propertyAAA");
            capturedProperties[0].Value.Should().Be("111");
            capturedProperties[1].Key.Should().Be("propertyBBB");
            capturedProperties[1].Value.Should().Be("222");

            // Check both active and inactive rules were passed to the ruleset generator
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

        private static SonarQubeRule CreateRule(string ruleKey, string repoKey, bool isActive) =>
            new SonarQubeRule(ruleKey, repoKey, isActive, SonarQubeIssueSeverity.Blocker, null);

        private class TestEnvironmentBuilder
        {
            private Mock<ISonarQubeService> sonarQubeServiceMock;

            private Mock<Core.CSharpVB.IRuleSetGenerator> ruleGenMock;
            private Mock<Core.CSharpVB.INuGetPackageInfoGenerator> nugetGenMock;

            private Mock<INuGetBindingOperation> nugetBindingMock;

            private readonly SonarQubeQualityProfile profile;
            private readonly Language language;
            private readonly string projectName;
            private readonly string serverUrl;


            public TestEnvironmentBuilder(SonarQubeQualityProfile profile, Language language,
                string projectName = "any", string serverUrl = "http://any")
            {
                this.profile = profile;
                this.language = language;
                this.projectName = projectName;
                this.serverUrl = serverUrl;

                Logger = new TestLogger();
                FilePathResponse = "test";
            }

            public string FilePathResponse { get; set; }

            public IList<SonarQubeRule> ActiveRulesResponse { get; set; }

            public IList<SonarQubeRule> InactiveRulesResponse { get; set; }

            public IList<SonarQubeProperty> PropertiesResponse { get; set; }

            public IList<NuGetPackageInfo> NuGetGeneratorResponse { get; set; }

            public bool NuGetBindingOperationResponse { get; set; }

            public Core.CSharpVB.RuleSet RuleSetGeneratorResponse { get; set; }

            public TestLogger Logger { get; private set; }

            public IEnumerable<SonarQubeRule> CapturedRulesPassedToRuleSetGenerator { get; private set; }
            public IDictionary<string, string> CapturedPropertiesPassedToRuleSetGenerator { get; private set; }

            public DotNetBindingConfigProvider CreateTestSubject()
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
                    .Setup(x => x.GetAllPropertiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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

                var bindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(new Uri(serverUrl), projectName, projectName),
                    SonarLintMode.Connected, bindingRootFolder);

                var solutionBindingFilePathGeneratorMock = new Mock<ISolutionBindingFilePathGenerator>();
                solutionBindingFilePathGeneratorMock
                    .Setup(x => x.Generate(bindingRootFolder, projectName, language.FileSuffixAndExtension))
                    .Returns(FilePathResponse);

                return new DotNetBindingConfigProvider(sonarQubeServiceMock.Object, nugetBindingMock.Object,
                    bindingConfiguration, Logger,
                    // inject the generator mocks
                    ruleGenMock.Object,
                    nugetGenMock.Object,
                    solutionBindingFilePathGeneratorMock.Object);
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

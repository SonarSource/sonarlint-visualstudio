/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Integration.TestInfrastructure.Helpers;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding;

[TestClass]
public class RoslynBindingConfigProviderTests
{
    private IList<SonarQubeRule> validRules;
    private IList<SonarQubeProperty> anyProperties;
    private SonarQubeQualityProfile validQualityProfile;

    private static readonly SonarQubeRule ActiveRuleWithUnsupportedSeverity = new SonarQubeRule("activeHotspot", "any1",
        true, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.SecurityHotspot);

    private static readonly SonarQubeRule InactiveRuleWithUnsupportedSeverity = new SonarQubeRule("inactiveHotspot", "any2",
        false, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.SecurityHotspot);

    private static readonly SonarQubeRule ActiveTaintAnalysisRule = new SonarQubeRule("activeTaint", "roslyn.sonaranalyzer.security.foo",
        true, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.CodeSmell);

    private static readonly SonarQubeRule InactiveTaintAnalysisRule = new SonarQubeRule("inactiveTaint", "roslyn.sonaranalyzer.security.bar",
        false, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.CodeSmell);

    [TestInitialize]
    public void TestInitialize()
    {
        validRules = new List<SonarQubeRule> { new SonarQubeRule("key", "repoKey", true, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.Bug) };

        anyProperties = Array.Empty<SonarQubeProperty>();

        validQualityProfile = new SonarQubeQualityProfile("qpkey1", "qp name", "any", false, DateTime.UtcNow);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynBindingConfigProvider, IBindingConfigProvider>(
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IRoslynConfigGenerator>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<ILanguageProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<RoslynBindingConfigProvider>();

    [TestMethod]
    public void GetRules_UnsupportedLanguage_Throws()
    {
        var builder = new TestEnvironmentBuilder(validQualityProfile, Language.Cpp);
        var testSubject = builder.CreateTestSubject();

        // Act
        Action act = () => testSubject.SaveConfigurationAsync(validQualityProfile, FakeRoslynLanguage.Instance, BindingConfiguration.Standalone, CancellationToken.None).Wait();

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
    public async Task GetConfig_NoSupportedActiveRules_Throws()
    {
        // Arrange
        var builder = new TestEnvironmentBuilder(validQualityProfile, Language.VBNET)
        {
            ActiveRulesResponse = new List<SonarQubeRule> { ActiveRuleWithUnsupportedSeverity }, InactiveRulesResponse = validRules, PropertiesResponse = anyProperties
        };
        var testSubject = builder.CreateTestSubject();

        // Act
        var act = () => testSubject.SaveConfigurationAsync(validQualityProfile, Language.VBNET, builder.BindingConfiguration, CancellationToken.None);

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage(string.Format(QualityProfilesStrings.FailedToCreateBindingConfigForLanguage, Language.VBNET.Name));

        builder.Logger.AssertOutputStrings(1);
        var expectedOutput = string.Format(BindingStrings.SubTextPaddingFormat,
            string.Format(BindingStrings.NoSonarAnalyzerActiveRulesForQualityProfile, validQualityProfile.Name, Language.VBNET.Name));
        builder.Logger.AssertOutputStrings(expectedOutput);
        builder.RoslynConfigGenerator.DidNotReceiveWithAnyArgs().GenerateAndSaveConfiguration(Arg.Any<RoslynLanguage>(), Arg.Any<string>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<IFileExclusions>(), Arg.Any<IReadOnlyCollection<IRoslynRuleStatus>>(), Arg.Any<IReadOnlyCollection<IRuleParameters>>());
    }

    [TestMethod]
    public async Task GetConfig_HasActiveInactiveAndUnsupportedRules_ReturnsValidBindingConfig()
    {
        // Arrange
        const string expectedServerUrl = "http://myhost:123/";

        var properties = new SonarQubeProperty[] { new("propertyAAA", "111"), new("propertyBBB", "222") };

        var activeSupportedRule = CreateRule("activeRuleKey", "repoKey1", true);
        var activeRules = new[] { activeSupportedRule, ActiveTaintAnalysisRule, ActiveRuleWithUnsupportedSeverity };
        var inactiveSupportedRule = CreateRule("inactiveRuleKey", "repoKey2", false);
        var inactiveRules = new[] { inactiveSupportedRule, InactiveTaintAnalysisRule, InactiveRuleWithUnsupportedSeverity };

        var builder = new TestEnvironmentBuilder(validQualityProfile, Language.CSharp, expectedServerUrl)
        {
            ActiveRulesResponse = activeRules, InactiveRulesResponse = inactiveRules, PropertiesResponse = properties
        };

        var testSubject = builder.CreateTestSubject();

        // Act
        await testSubject.SaveConfigurationAsync(validQualityProfile, Language.CSharp, builder.BindingConfiguration, CancellationToken.None);

        // Assert
        builder.RoslynConfigGenerator
            .Received()
            .GenerateAndSaveConfiguration(
                Language.CSharp,
                builder.BindingConfiguration.BindingConfigDirectory,
                Arg.Is<IDictionary<string, string>>(x => x.SequenceEqual(builder.SonarProperties)),
                builder.ServerExclusionsResponse,
                Arg.Is((IReadOnlyCollection<SonarQubeRoslynRuleStatus> x) =>
                    x.Select(y => y.Key).SequenceEqual(new []{activeSupportedRule.Key, inactiveSupportedRule.Key})),
                Arg.Is<IReadOnlyCollection<IRuleParameters>>(x => x.SequenceEqual(new []{activeSupportedRule})));
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

        RoslynBindingConfigProvider.IsSupportedRule(rule).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(SonarQubeIssueType.Unknown, false)]
    [DataRow(SonarQubeIssueType.SecurityHotspot, false)]
    [DataRow(SonarQubeIssueType.CodeSmell, true)]
    [DataRow(SonarQubeIssueType.Bug, true)]
    [DataRow(SonarQubeIssueType.Vulnerability, true)]
    public void IsSupportedRule_Severity(SonarQubeIssueType issueType, bool expected)
    {
        var rule = new SonarQubeRule("any", "any", true, SonarQubeIssueSeverity.Blocker, null, null, null, issueType);

        RoslynBindingConfigProvider.IsSupportedRule(rule).Should().Be(expected);
    }

    private static SonarQubeRule CreateRule(string ruleKey, string repoKey, bool isActive) =>
        new SonarQubeRule(ruleKey, repoKey, isActive, SonarQubeIssueSeverity.Blocker, null, null, null, SonarQubeIssueType.CodeSmell);

    private class TestEnvironmentBuilder
    {
        private Mock<ISonarQubeService> sonarQubeServiceMock;

        private readonly SonarQubeQualityProfile profile;
        private readonly Language language;
        private readonly string serverUrl;

        private const string ExpectedProjectKey = "fixed.project.key";

        public TestEnvironmentBuilder(SonarQubeQualityProfile profile, Language language, string serverUrl = "http://any")
        {
            this.profile = profile;
            this.language = language;
            this.serverUrl = serverUrl;

            Logger = new TestLogger();
            PropertiesResponse = new List<SonarQubeProperty>();
        }

        public BindingConfiguration BindingConfiguration { get; private set; }
        public IRoslynConfigGenerator RoslynConfigGenerator { get; private set; }
        public IList<SonarQubeRule> ActiveRulesResponse { get; set; }
        public IList<SonarQubeRule> InactiveRulesResponse { get; set; }
        public IList<SonarQubeProperty> PropertiesResponse { get; set; }

        public ServerExclusions ServerExclusionsResponse { get; set; }
        public Dictionary<string, string> SonarProperties { get; set; }
        public TestLogger Logger { get; private set; }

        public RoslynBindingConfigProvider CreateTestSubject()
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

            ServerExclusionsResponse = new ServerExclusions(
                exclusions: ["path1"],
                globalExclusions: ["path2"],
                inclusions: ["path3"]);

            sonarQubeServiceMock
                .Setup(x => x.GetServerExclusions(ExpectedProjectKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ServerExclusionsResponse);

            BindingConfiguration = new BindingConfiguration(
                new BoundServerProject("solution", ExpectedProjectKey, new ServerConnection.SonarQube(new Uri(serverUrl))),
                SonarLintMode.Connected,
                "c:\\test\\");

            SonarProperties = PropertiesResponse.ToDictionary(x => x.Key, y => y.Value);

            RoslynConfigGenerator = Substitute.For<IRoslynConfigGenerator>();

            return new RoslynBindingConfigProvider(sonarQubeServiceMock.Object, Logger,
                RoslynConfigGenerator,
                LanguageProvider.Instance);
        }
    }
}

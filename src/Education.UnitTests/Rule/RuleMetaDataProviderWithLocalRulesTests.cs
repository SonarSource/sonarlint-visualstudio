/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Education.UnitTests.Rule;

[TestClass]
public class RuleMetaDataProviderWithLocalRulesTests
{
    private ISLCoreRuleMetaDataProvider slCoreRuleMetaDataProvider = null!;
    private ISonarLintSettings sonarLintSettings = null!;
    private RuleMetaDataProviderWithLocalRules testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreRuleMetaDataProvider = Substitute.For<ISLCoreRuleMetaDataProvider>();
        sonarLintSettings = Substitute.For<ISonarLintSettings>();

        testSubject = new RuleMetaDataProviderWithLocalRules(slCoreRuleMetaDataProvider, sonarLintSettings);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RuleMetaDataProviderWithLocalRules, IRuleMetaDataProvider>(
            MefTestHelpers.CreateExport<ISLCoreRuleMetaDataProvider>(),
            MefTestHelpers.CreateExport<ISonarLintSettings>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<RuleMetaDataProviderWithLocalRules>();

    [TestMethod]
    public async Task GetRuleInfoAsync_Sq0079_ReturnsHardcodedRuleInfo()
    {
        var ruleId = new SonarCompositeRuleId("csharpsquid", "SQ0079");

        var result = await testSubject.GetRuleInfoAsync(ruleId);

        result.Should().NotBeNull();
        result.FullRuleKey.Should().Be("csharpsquid:SQ0079");
        result.Name.Should().Be(Resources.UnusedPragmaRuleName);
        result.Description.Should().Be(Resources.UnusedPragmaRuleDescription);
        result.CleanCodeAttribute.Should().Be(CleanCodeAttribute.Clear);
        result.Severity.Should().BeNull();
        result.IssueType.Should().BeNull();
        result.RichRuleDescriptionDto.Should().BeNull();
        result.SelectedContextKey.Should().BeNull();
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_Sq0079_DoesNotDelegateToWrappedProvider()
    {
        var ruleId = new SonarCompositeRuleId("csharpsquid", "SQ0079");

        await testSubject.GetRuleInfoAsync(ruleId);

        await slCoreRuleMetaDataProvider.DidNotReceiveWithAnyArgs().GetRuleInfoAsync(default, default);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_Sq0079_PragmaRuleSeverityInfo_ReturnsLowImpact()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.Info);
        var ruleId = new SonarCompositeRuleId("csharpsquid", "SQ0079");

        var result = await testSubject.GetRuleInfoAsync(ruleId);

        result.DefaultImpacts.Should().HaveCount(1)
            .And.Contain(SoftwareQuality.Maintainability, SoftwareQualitySeverity.Low);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_Sq0079_PragmaRuleSeverityWarn_ReturnsMediumImpact()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.Warn);
        var ruleId = new SonarCompositeRuleId("csharpsquid", "SQ0079");

        var result = await testSubject.GetRuleInfoAsync(ruleId);

        result.DefaultImpacts.Should().HaveCount(1)
            .And.Contain(SoftwareQuality.Maintainability, SoftwareQualitySeverity.Medium);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_NonSq0079_DelegatesToWrappedProvider()
    {
        var ruleId = new SonarCompositeRuleId("any", "any");
        var issueId = Guid.NewGuid();
        var expectedRuleInfo = Substitute.For<IRuleInfo>();
        slCoreRuleMetaDataProvider.GetRuleInfoAsync(ruleId, issueId).Returns(expectedRuleInfo);

        var result = await testSubject.GetRuleInfoAsync(ruleId, issueId);

        result.Should().BeSameAs(expectedRuleInfo);
        await slCoreRuleMetaDataProvider.Received(1).GetRuleInfoAsync(ruleId, issueId);
    }
}

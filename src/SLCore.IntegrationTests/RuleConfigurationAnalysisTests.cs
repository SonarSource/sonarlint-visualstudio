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

using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class RuleConfigurationAnalysisTests
{
    private static FileAnalysisTestsRunner sharedFileAnalysisTestsRunner;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        sharedFileAnalysisTestsRunner = await FileAnalysisTestsRunner.CreateInstance(nameof(RuleConfigurationAnalysisTests));
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        sharedFileAnalysisTestsRunner.Dispose();
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_JavaScriptAnalysisShouldIgnoreOneIssueOfInactiveRule()
    {
        var ruleToDisable = FileAnalysisTestsRunner.JavaScriptIssues.ExpectedIssues[0];
        var ruleConfig = CreateInactiveRuleConfig(ruleToDisable.ruleKey);
        sharedFileAnalysisTestsRunner.SetRuleConfiguration(ruleConfig);

        var issuesByFileUri = await sharedFileAnalysisTestsRunner.RunAutomaticFileAnalysis(FileAnalysisTestsRunner.JavaScriptIssues, TestContext.TestName);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(FileAnalysisTestsRunner.JavaScriptIssues.GetFullPath())].Should().HaveCount(FileAnalysisTestsRunner.JavaScriptIssues.ExpectedIssues.Count - 1);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_SecretsAnalysisShouldIgnoreTwoIssuesOfInactiveRule()
    {
        var multipleIssuesRule = FileAnalysisTestsRunner.SecretsIssues.RuleWithMultipleIssues;
        var secretsRuleConfig = CreateInactiveRuleConfig(multipleIssuesRule.ruleKey);
        sharedFileAnalysisTestsRunner.SetRuleConfiguration(secretsRuleConfig);

        var issuesByFileUri = await sharedFileAnalysisTestsRunner.RunAutomaticFileAnalysis(FileAnalysisTestsRunner.SecretsIssues, TestContext.TestName);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(FileAnalysisTestsRunner.SecretsIssues.GetFullPath())].Should()
            .HaveCount(FileAnalysisTestsRunner.SecretsIssues.ExpectedIssues.Count - multipleIssuesRule.issuesCount);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_JsLetRuleIsDisableInSettingsFile_JavaScriptAnalysisShouldIgnoreIssueOnInitialization()
    {
        var ruleToDisable = FileAnalysisTestsRunner.JavaScriptIssues.ExpectedIssues[0];
        var ruleConfig = CreateInactiveRuleConfig(ruleToDisable.ruleKey);
        using var customTestRunner = await FileAnalysisTestsRunner.CreateInstance(TestContext.TestName, ruleConfig);

        var issuesByFileUri = await customTestRunner.RunAutomaticFileAnalysis(FileAnalysisTestsRunner.JavaScriptIssues, TestContext.TestName);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(FileAnalysisTestsRunner.JavaScriptIssues.GetFullPath())].Should().HaveCount(FileAnalysisTestsRunner.JavaScriptIssues.ExpectedIssues.Count - 1);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_CloudSecretsRuleIsDisabledInSettingsFile_SecretsAnalysisShouldIgnoreIssueOnInitialization()
    {
        var multipleIssuesRule = FileAnalysisTestsRunner.SecretsIssues.RuleWithMultipleIssues;
        var secretsRuleConfig = CreateInactiveRuleConfig(multipleIssuesRule.ruleKey);
        using var customTestRunner = await FileAnalysisTestsRunner.CreateInstance(TestContext.TestName, secretsRuleConfig);

        var issuesByFileUri = await customTestRunner.RunAutomaticFileAnalysis(FileAnalysisTestsRunner.SecretsIssues, TestContext.TestName);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(FileAnalysisTestsRunner.SecretsIssues.GetFullPath())].Should()
            .HaveCount(FileAnalysisTestsRunner.SecretsIssues.ExpectedIssues.Count - multipleIssuesRule.issuesCount);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_CtorParamsUnderThreshold_JavaScriptActiveRuleShouldHaveNoIssue()
    {
        var ctorParamsRuleConfig = CreateActiveCtorParamRuleConfig(threshold: OneIssueRuleWithParamFile.ActualCtorParams + 1);
        sharedFileAnalysisTestsRunner.SetRuleConfiguration(ctorParamsRuleConfig);

        var issuesByFileUri = await sharedFileAnalysisTestsRunner.RunAutomaticFileAnalysis(FileAnalysisTestsRunner.OneIssueRuleWithParam, TestContext.TestName);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(FileAnalysisTestsRunner.OneIssueRuleWithParam.GetFullPath())].Should().HaveCount(0);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_CtorParamsAboveThreshold_JavaScriptActiveRuleShouldHaveOneIssue()
    {
        var ctorParamsRuleConfig = CreateActiveCtorParamRuleConfig(threshold: OneIssueRuleWithParamFile.ActualCtorParams - 1);
        sharedFileAnalysisTestsRunner.SetRuleConfiguration(ctorParamsRuleConfig);

        var issuesByFileUri = await sharedFileAnalysisTestsRunner.RunAutomaticFileAnalysis(FileAnalysisTestsRunner.OneIssueRuleWithParam, TestContext.TestName);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(FileAnalysisTestsRunner.OneIssueRuleWithParam.GetFullPath())].Should().HaveCount(1);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_HtmlAnalysisShouldIgnoreThreeIssuesOfInactiveRule()
    {
        var multipleIssuesRule = FileAnalysisTestsRunner.HtmlIssues.RuleWithMultipleIssues;
        var htmlRuleConfig = CreateInactiveRuleConfig(multipleIssuesRule.ruleKey);
        sharedFileAnalysisTestsRunner.SetRuleConfiguration(htmlRuleConfig);

        var issuesByFileUri = await sharedFileAnalysisTestsRunner.RunAutomaticFileAnalysis(FileAnalysisTestsRunner.HtmlIssues, TestContext.TestName);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(FileAnalysisTestsRunner.HtmlIssues.GetFullPath())].Should().HaveCount(FileAnalysisTestsRunner.HtmlIssues.ExpectedIssues.Count - multipleIssuesRule.issuesCount);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_HtmlRuleIsDisabledInSettingsFile_HtmlAnalysisShouldIgnoreIssueOnInitialization()
    {
        var multipleIssuesRule = FileAnalysisTestsRunner.HtmlIssues.RuleWithMultipleIssues;
        var htmlRuleConfig = CreateInactiveRuleConfig(multipleIssuesRule.ruleKey);
        using var customTestRunner = await FileAnalysisTestsRunner.CreateInstance(TestContext.TestName, htmlRuleConfig);

        var issuesByFileUri = await customTestRunner.RunAutomaticFileAnalysis(FileAnalysisTestsRunner.HtmlIssues, TestContext.TestName);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(FileAnalysisTestsRunner.HtmlIssues.GetFullPath())].Should().HaveCount(FileAnalysisTestsRunner.HtmlIssues.ExpectedIssues.Count - multipleIssuesRule.issuesCount);
    }

    private static Dictionary<string, StandaloneRuleConfigDto> CreateActiveCtorParamRuleConfig(int threshold) =>
        new() { { OneIssueRuleWithParamFile.CtorParamRuleId, new StandaloneRuleConfigDto(isActive: true, new() { { OneIssueRuleWithParamFile.CtorParamName, threshold.ToString() } }) } };

    private static Dictionary<string, StandaloneRuleConfigDto> CreateInactiveRuleConfig(string ruleId) => new() { { ruleId, new StandaloneRuleConfigDto(isActive: false, []) } };
}

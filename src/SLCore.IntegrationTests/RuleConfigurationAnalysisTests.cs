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

using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class RuleConfigurationAnalysisTests : FileAnalysisTestsBase
{
    private const string CtorParamRuleId = "javascript:S107";
    private const int ActualCtorParams = 4;
    private const string CtorParamName = "maximumFunctionParameters";

    [TestMethod]
    public async Task StandaloneRuleConfig_JavaScriptAnalysisShouldIgnoreOneIssueOfInactiveRule()
    {
        var ruleToDisable = JavaScriptIssues.ExpectedIssues[0];
        var ruleConfig = CreateInactiveRuleConfig(ruleToDisable.ruleKey);

        var issuesByFileUri = await RunFileAnalysisWithUpdatedRulesConfiguration(JavaScriptIssues.Path, ruleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(JavaScriptIssues.Path))].Should().HaveCount(JavaScriptIssues.ExpectedIssues.Count - 1);
    }
    
    [TestMethod]
    public async Task StandaloneRuleConfig_SecretsAnalysisShouldIgnoreTwoIssuesOfInactiveRule()
    {
        var multipleIssuesRule = SecretsIssues.RuleWithMultipleIssues;
        var secretsRuleConfig = CreateInactiveRuleConfig(multipleIssuesRule.ruleKey);

        var issuesByFileUri = await RunFileAnalysisWithUpdatedRulesConfiguration(SecretsIssues.Path, secretsRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(SecretsIssues.Path))].Should().HaveCount(SecretsIssues.ExpectedIssues.Count - multipleIssuesRule.issuesCount);
    }
    
    [TestMethod]
    public async Task StandaloneRuleConfig_CtorParamsUnderThreshold_JavaScriptActiveRuleShouldHaveNoIssue()
    {
        var ctorParamsRuleConfig = CreateActiveCtorParamRuleConfig(threshold: ActualCtorParams + 1);

        var issuesByFileUri = await RunFileAnalysisWithUpdatedRulesConfiguration(OneIssueRuleWithParamPath, ctorParamsRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(OneIssueRuleWithParamPath))].Should().HaveCount(0);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_CtorParamsAboveThreshold_JavaScriptActiveRuleShouldHaveOneIssue()
    {
        var ctorParamsRuleConfig = CreateActiveCtorParamRuleConfig(threshold: ActualCtorParams - 1);

        var issuesByFileUri = await RunFileAnalysisWithUpdatedRulesConfiguration(OneIssueRuleWithParamPath, ctorParamsRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(OneIssueRuleWithParamPath))].Should().HaveCount(1);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_JsLetRuleIsDisableInSettingsFile_JavaScriptAnalysisShouldIgnoreIssueOnInitialization()
    {
        var ruleToDisable = JavaScriptIssues.ExpectedIssues[0];
        var ruleConfig = CreateInactiveRuleConfig(ruleToDisable.ruleKey);

        var issuesByFileUri = await RunFileAnalysisWithInitialRulesConfiguration(JavaScriptIssues.Path, ruleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(JavaScriptIssues.Path))].Should().HaveCount(JavaScriptIssues.ExpectedIssues.Count - 1);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_CloudSecretsRuleIsDisabledInSettingsFile_SecretsAnalysisShouldIgnoreIssueOnInitialization()
    {
        var multipleIssuesRule = SecretsIssues.RuleWithMultipleIssues;
        var secretsRuleConfig = CreateInactiveRuleConfig(multipleIssuesRule.ruleKey);

        var issuesByFileUri = await RunFileAnalysisWithInitialRulesConfiguration(SecretsIssues.Path, secretsRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(SecretsIssues.Path))].Should().HaveCount(SecretsIssues.ExpectedIssues.Count - multipleIssuesRule.issuesCount);
    }

    private static Dictionary<string, StandaloneRuleConfigDto> CreateActiveCtorParamRuleConfig(int threshold)
    {
        return new()
        {
            { CtorParamRuleId, new StandaloneRuleConfigDto(isActive: true, new() { { CtorParamName, threshold.ToString() } }) }
        };
    }

    private static Dictionary<string, StandaloneRuleConfigDto> CreateInactiveRuleConfig(string ruleId)
    {
        return new()
        {
            { ruleId, new StandaloneRuleConfigDto(isActive: false, []) }
        };
    }
}

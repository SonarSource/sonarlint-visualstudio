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
    private const string LetRuleId = "javascript:S3504";
    private const string CloudSecretsRuleId = "secrets:S6336";
    private const string CtorParamRuleId = "javascript:S107";
    private const int ActualCtorParams = 4;
    private const string CtorParamName = "maximumFunctionParameters";

    [TestMethod]
    public async Task StandaloneRuleConfig_JavaScriptAnalysisShouldIgnoreOneIssueOfInactiveRule()
    {
        var letRuleConfig = CreateInactiveRuleConfig(LetRuleId);

        var issuesByFileUri = await RunFileAnalysisWithUpdatedRulesConfiguration(TwoJsIssuesPath, letRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(TwoJsIssuesPath))].Should().HaveCount(1);
    }
    
    [TestMethod]
    public async Task StandaloneRuleConfig_SecretsAnalysisShouldIgnoreTwoIssuesOfInactiveRule()
    {
        var secretsRuleConfig = CreateInactiveRuleConfig(CloudSecretsRuleId);

        var issuesByFileUri = await RunFileAnalysisWithUpdatedRulesConfiguration(ThreeSecretsIssuesPath, secretsRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(ThreeSecretsIssuesPath))].Should().HaveCount(1);
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
        var letRuleConfig = CreateInactiveRuleConfig(LetRuleId);

        var issuesByFileUri = await RunFileAnalysisWithInitialRulesConfiguration(TwoJsIssuesPath, letRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(TwoJsIssuesPath))].Should().HaveCount(1);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_CloudSecretsRuleIsDisabledInSettingsFile_SecretsAnalysisShouldIgnoreIssueOnInitialization()
    {
        var secretsRuleConfig = CreateInactiveRuleConfig(CloudSecretsRuleId);

        var issuesByFileUri = await RunFileAnalysisWithInitialRulesConfiguration(ThreeSecretsIssuesPath, secretsRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(ThreeSecretsIssuesPath))].Should().HaveCount(1);
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

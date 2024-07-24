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

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class SimpleAnalysisTests : FileAnalysisTestsBase
{
    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_JavaScriptAnalysisProducesExpectedNumberOfIssues() 
        => DefaultRuleConfig_JavaScriptAnalysisProducesExpectedNumberOfIssues(false);
    
    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_JavaScriptAnalysisProducesExpectedNumberOfIssues() 
        => DefaultRuleConfig_JavaScriptAnalysisProducesExpectedNumberOfIssues(true);

    private async Task DefaultRuleConfig_JavaScriptAnalysisProducesExpectedNumberOfIssues(bool sendContent)
    {
        var issuesByFileUri = await RunFileAnalysis(ThreeSecretsIssuesPath, sendContent: sendContent);
        
        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(ThreeSecretsIssuesPath))].Should().HaveCount(3);
    }

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_SecretsAnalysisProducesExpectedNumberOfIssues() 
        => DefaultRuleConfig_SecretsAnalysisProducesExpectedNumberOfIssues(false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_SecretsAnalysisProducesExpectedNumberOfIssues() 
        => DefaultRuleConfig_SecretsAnalysisProducesExpectedNumberOfIssues(true);

    private async Task DefaultRuleConfig_SecretsAnalysisProducesExpectedNumberOfIssues(bool sendContent)
    {
        var issuesByFileUri = await RunFileAnalysis(TwoJsIssuesPath, sendContent: sendContent);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(TwoJsIssuesPath))].Should().HaveCount(2);
    }
}

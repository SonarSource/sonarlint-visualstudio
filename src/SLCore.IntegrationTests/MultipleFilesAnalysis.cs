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
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class MultipleFilesAnalysis
{
    private static FileAnalysisTestsRunner _sharedFileAnalysisTestsRunner;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context) => _sharedFileAnalysisTestsRunner = await FileAnalysisTestsRunner.CreateInstance(nameof(MultipleFilesAnalysis));

    [ClassCleanup]
    public static void ClassCleanup() => _sharedFileAnalysisTestsRunner.Dispose();

    [TestMethod]
    public async Task MultipleFilesAnalysis_ProducesExpectedResult()
    {
        List<ITestingFile> testingFiles = [FileAnalysisTestsRunner.SecretsIssues, FileAnalysisTestsRunner.HtmlIssues];

        var issuesByFileUri = await _sharedFileAnalysisTestsRunner.RunAutomaticMultipleFileAnalysis(
            testingFiles, TestContext.TestName);

        issuesByFileUri.Should().HaveCount(testingFiles.Count);
        foreach (var testingFile in testingFiles)
        {
            VerifyExpectedIssues(issuesByFileUri, testingFile);
        }
    }

    private static void VerifyExpectedIssues(Dictionary<FileUri, List<RaisedIssueDto>> actualIssuesByFileUri, ITestingFile testingFile)
    {
        var receivedIssues = actualIssuesByFileUri[new FileUri(testingFile.GetFullPath())];
        var receivedTestIssues = receivedIssues.Select(x => new TestIssue(x.ruleKey, x.textRange, x.severityMode.Right?.cleanCodeAttribute, x.flows.Count));
        receivedTestIssues.Should().BeEquivalentTo(testingFile.ExpectedIssues);
    }
}

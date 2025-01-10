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

using System.IO;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class SimpleAnalysisTests
{
    private static FileAnalysisTestsRunner sharedFileAnalysisTestsRunner;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        sharedFileAnalysisTestsRunner = new FileAnalysisTestsRunner(nameof(SimpleAnalysisTests));
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        sharedFileAnalysisTestsRunner.Dispose();
    }

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_JavaScriptAnalysisProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.JavaScriptIssues,false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_JavaScriptAnalysisProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.JavaScriptIssues,true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_SecretsAnalysisProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.SecretsIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_SecretsAnalysisProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.SecretsIssues, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_TypeScriptAnalysisProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.TypeScriptIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_TypeScriptAnalysisProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.TypeScriptIssues, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_CFamilyAnalysisProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.CFamilyIssues, false, GenerateTestCompilationDatabase());

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_CFamilyAnalysisProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.CFamilyIssues, true, GenerateTestCompilationDatabase());

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_CssAnalysisProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.CssIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_CssProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.CssIssues, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_CssAnalysisInVueProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.VueIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_CssAnalysisInVyeProducesExpectedIssues()
        => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.VueIssues, true);

    private async Task DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(ITestingFile testingFile, bool sendContent, Dictionary<string, string> extraProperties = null)
    {
        var issuesByFileUri = await sharedFileAnalysisTestsRunner.RunFileAnalysis(testingFile, TestContext.TestName, sendContent: sendContent, extraProperties: extraProperties);

        issuesByFileUri.Should().HaveCount(1);
        var receivedIssues = issuesByFileUri[new FileUri(testingFile.GetFullPath())];
        var receivedTestIssues = receivedIssues.Select(x => new TestIssue(x.ruleKey, x.textRange, x.severityMode.Right?.cleanCodeAttribute, x.flows.Count));
        receivedTestIssues.Should().BeEquivalentTo(testingFile.ExpectedIssues);
    }

    private static Dictionary<string, string> GenerateTestCompilationDatabase()
    {
        /* The CFamily analysis apart from the source code file requires also the compilation database file.
           The compilation database file must contain the absolute path to the source code file the compilation database json file and the compiler path.
           For the compiler we use the MSVC which is set as an environment variable. Make sure the environment variable is set to point to the compiler path
           (the absolute path to cl.exe). */
        var compilerPath = NormalizePath(Environment.GetEnvironmentVariable("MSVC"));
        var cFamilyIssuesFileAbsolutePath = NormalizePath(FileAnalysisTestsRunner.CFamilyIssues.GetFullPath());
        var analysisDirectory = NormalizePath(Path.GetDirectoryName(cFamilyIssuesFileAbsolutePath));
        var jsonContent = $$"""
                            [
                            {
                              "directory": "{{analysisDirectory}}",
                              "command": "\"{{compilerPath}}\" /nologo /TP /DWIN32 /D_WINDOWS /W3 /GR /EHsc /MDd /Ob0 /Od /RTC1 -std:c++20 -ZI /FoCFamilyIssues.cpp.obj /FS -c {{cFamilyIssuesFileAbsolutePath}}",
                              "file": "{{cFamilyIssuesFileAbsolutePath}}"
                            }
                            ]
                            """;
        var tempCompilationDatabase = Path.ChangeExtension(Path.GetTempFileName(), ".json");
        File.WriteAllText(tempCompilationDatabase, jsonContent);

        var compilationDatabase = new Dictionary<string, string>
        {
            { "sonar.cfamily.compile-commands", tempCompilationDatabase }
        };
        return compilationDatabase;
    }

    private static string NormalizePath(string path)
    {
        var singleDirectorySeparator = Path.DirectorySeparatorChar.ToString();
        var doubleDirectorySeparator = singleDirectorySeparator + singleDirectorySeparator;
        return path?.Replace(singleDirectorySeparator, doubleDirectorySeparator);
    }
}

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
using System.Reflection;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class FileExclusionsTests
{
    private const string someOtherFileExclusion = "**/someotherpath/**";
    private static FileAnalysisTestsRunner sharedFileAnalysisTestsRunner;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context) => sharedFileAnalysisTestsRunner = await FileAnalysisTestsRunner.CreateInstance(nameof(FileExclusionsTests));

    [ClassCleanup]
    public static void ClassCleanup() => sharedFileAnalysisTestsRunner.Dispose();

    [DataTestMethod]
    [DynamicData(nameof(TestingFiles))]
    public async Task CurrentFileExcluded_CurrentFileNotAnalyzed(ITestingFile testingFile)
    {
        var configScope = GetConfigurationScopeName(testingFile);
        var analysisSettings = new AnalysisSettings([], [someOtherFileExclusion, testingFile.RelativePath]);
        sharedFileAnalysisTestsRunner.SetFileExclusions(configScope, analysisSettings.NormalizedFileExclusions);

        await sharedFileAnalysisTestsRunner.VerifyAnalysisSkipped(testingFile, configScope, extraProperties: (testingFile as ITestingFileWithProperties)?.GetAnalysisProperties());
    }

    [DataTestMethod]
    [DynamicData(nameof(TestingFiles))]
    public async Task OtherFileExcluded_RunsAnalysisOnCurrentFile(ITestingFile testingFile)
    {
        var configScope = GetConfigurationScopeName(testingFile);
        var analysisSettings = new AnalysisSettings([], [someOtherFileExclusion]);
        sharedFileAnalysisTestsRunner.SetFileExclusions(configScope, analysisSettings.NormalizedFileExclusions);

        var fileAnalysisResults = await sharedFileAnalysisTestsRunner.RunFileAnalysis(testingFile, configScope, extraProperties: (testingFile as ITestingFileWithProperties)?.GetAnalysisProperties());

        fileAnalysisResults.Count.Should().Be(1);
        fileAnalysisResults.Single().Value.Should().HaveCount(testingFile.ExpectedIssues.Count);
    }

    private string GetConfigurationScopeName(ITestingFile testingFile) => TestContext.TestName + "_" + Path.GetFileNameWithoutExtension(testingFile.RelativePath);

    public static object[][] TestingFiles
    {
        get
        {
            var testingFileType = typeof(ITestingFile);
            var testingFiles = Assembly.GetAssembly(testingFileType).GetTypes()
                .Where(p => testingFileType.IsAssignableFrom(p) && !p.IsInterface)
                .Select(Activator.CreateInstance)
                .Where(instance => instance is ITestingFile { ExpectedIssues.Count: > 0 })
                .Select(instance => (object[]) [instance]).ToArray();

            return testingFiles;
        }
    }
}

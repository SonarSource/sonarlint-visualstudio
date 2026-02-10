/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.SLCore.Listener.Files;

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

    [TestMethod]
    [DynamicData(nameof(TestingFiles))]
    public async Task CurrentFileExcluded_CurrentFileNotAnalyzed(ITestingFile testingFile)
    {
        var configScope = GetConfigurationScopeName(testingFile);
        var analysisSettings = new AnalysisSettings([], [someOtherFileExclusion, testingFile.RelativePath]);
        var fileExclusionsListener = sharedFileAnalysisTestsRunner.SetFileExclusionsInMockedListener(configScope, analysisSettings.NormalizedFileExclusions);

        await sharedFileAnalysisTestsRunner.VerifyAnalysisSkippedForExclusions(testingFile, configScope);

        await fileExclusionsListener.Received().GetFileExclusionsAsync(Arg.Is<GetFileExclusionsParams>(p => p.configurationScopeId == configScope));
    }

    [TestMethod]
    [DynamicData(nameof(TestingFiles))]
    public async Task OtherFileExcluded_RunsAnalysisOnCurrentFile(ITestingFile testingFile)
    {
        var configScope = GetConfigurationScopeName(testingFile);
        var analysisSettings = new AnalysisSettings([], [someOtherFileExclusion]);
        var fileExclusionsListener = sharedFileAnalysisTestsRunner.SetFileExclusionsInMockedListener(configScope, analysisSettings.NormalizedFileExclusions);

        var fileAnalysisResults
            = await sharedFileAnalysisTestsRunner.RunAnalysisOnOpenFile(testingFile, configScope, compilationDatabasePath: (testingFile as ITestingCFamily)?.GetCompilationDatabasePath());

        await fileExclusionsListener.Received().GetFileExclusionsAsync(Arg.Is<GetFileExclusionsParams>(p => p.configurationScopeId == configScope));
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

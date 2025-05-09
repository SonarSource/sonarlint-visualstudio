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

using System.Collections.Immutable;
using System.IO;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Core.UnitTests.UserRuleSettings;

[TestClass]
public class AnalysisSettingsTests
{
    [TestMethod]
    public void AnalysisSettings_GlobalFileExclusions_WithNull_ReturnsEmptyArray()
    {
        var analysisSettings = new AnalysisSettings();

        analysisSettings.GlobalFileExclusions.Should().BeEmpty();
    }

    [TestMethod]
    public void AnalysisSettings_GlobalFileExclusions_DiscardsEmptyOrNullPaths()
    {
        var analysisSettings = new AnalysisSettings(globalFileExclusions: ImmutableArray.Create("", " ", null));

        analysisSettings.GlobalFileExclusions.Should().BeEmpty();
    }

    [TestMethod]
    public void AnalysisSettings_SolutionFileExclusions_WithNull_ReturnsEmptyArray()
    {
        var analysisSettings = new AnalysisSettings();

        analysisSettings.SolutionFileExclusions.Should().BeEmpty();
    }

    [TestMethod]
    public void AnalysisSettings_SolutionFileExclusions_DiscardsEmptyOrNullPaths()
    {
        var analysisSettings = new AnalysisSettings(solutionFileExclusions: ImmutableArray.Create("", " ", null));

        analysisSettings.SolutionFileExclusions.Should().BeEmpty();
    }

    [TestMethod]
    public void AnalysisSettings_NormalizedFileExclusions_WithGlobalFileExclusions_UsesGlobalExclusions()
    {
        var analysisSettings = new AnalysisSettings(globalFileExclusions: ImmutableArray.Create("file1.cpp", "file2.cpp"));

        analysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo(["**/file1.cpp", "**/file2.cpp"]);
    }

    [TestMethod]
    public void AnalysisSettings_NormalizedFileExclusions_WithSolutionFileExclusions_UsesSolutionExclusions()
    {
        var analysisSettings = new AnalysisSettings(solutionFileExclusions: ImmutableArray.Create("file1.cpp", "file2.cpp"));

        analysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo(["**/file1.cpp", "**/file2.cpp"]);
    }

    [TestMethod]
    public void AnalysisSettings_NormalizedFileExclusions_WithGlobalAndSolutionFileExclusions_UsesSolutionExclusions()
    {
        var analysisSettings = new AnalysisSettings(globalFileExclusions: ImmutableArray.Create("file1.cpp", "file2.cpp"), solutionFileExclusions: ImmutableArray.Create("file3.cpp", "file4.cpp"));

        analysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo(["**/file3.cpp", "**/file4.cpp"]);
    }

    [TestMethod]
    public void AnalysisSettings_NormalizedFileExclusions_WithBackslashes_NormalizesToForwardSlashes()
    {
        var globalAnalysisSettings = new AnalysisSettings(globalFileExclusions: ImmutableArray.Create("**\\obj\\*", "a\\file1.cpp", "file2.cpp"));
        globalAnalysisSettings.GlobalFileExclusions.Should().BeEquivalentTo("**\\obj\\*", "a\\file1.cpp", "file2.cpp");
        globalAnalysisSettings.SolutionFileExclusions.Should().BeEmpty();
        globalAnalysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo("**/a/file1.cpp", "**/obj/*", "**/file2.cpp");

        var solutionAnalysisSettings = new AnalysisSettings(solutionFileExclusions: ImmutableArray.Create("**\\obj\\*", "a\\file1.cpp", "file2.cpp"));
        solutionAnalysisSettings.GlobalFileExclusions.Should().BeEmpty();
        solutionAnalysisSettings.SolutionFileExclusions.Should().BeEquivalentTo("**\\obj\\*", "a\\file1.cpp", "file2.cpp");
        solutionAnalysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo("**/a/file1.cpp", "**/obj/*", "**/file2.cpp");
    }

    [DataTestMethod]
    [DataRow(@"*", @"**/*")]
    [DataRow(@"?", @"**/?")]
    [DataRow(@"path", @"**/path")]
    [DataRow(@"p?th", @"**/p?th")]
    [DataRow(@"p*th", @"**/p*th")]
    [DataRow(@"*path", @"**/*path")]
    [DataRow(@"**path", @"**/**path")]
    [DataRow(@"**\path", @"**/path")]
    [DataRow(@"**/path", @"**/path")]
    [DataRow(@"file/path", @"**/file/path")]
    [DataRow(@"file\path", @"**/file/path")]
    [DataRow(@"C:\file\path", @"C:/file/path")] // rooted path
    [DataRow(@"file/*/p?th.*", @"**/file/*/p?th.*")]
    [DataRow(@"file\*\p?th.*", @"**/file/*/p?th.*")]
    public void AnalysisSettings_NormalizedFileExclusions_TransformsPathCorrectly(string original, string expected)
    {
        var globalAnalysisSettings = new AnalysisSettings(globalFileExclusions: ImmutableArray.Create(original));
        globalAnalysisSettings.GlobalFileExclusions.Should().BeEquivalentTo(original);
        globalAnalysisSettings.SolutionFileExclusions.Should().BeEmpty();
        globalAnalysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo(expected);

        var solutionAnalysisSettings = new AnalysisSettings(solutionFileExclusions: ImmutableArray.Create(original));
        solutionAnalysisSettings.GlobalFileExclusions.Should().BeEmpty();
        solutionAnalysisSettings.SolutionFileExclusions.Should().BeEquivalentTo(original);
        solutionAnalysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo(expected);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetInvalidPaths))]
    public void AnalysisSettings_NormalizedFileExclusions_ContainsInvalidPathCharacters_DoesNotCrashAndDoesNotNormalize(string invalidPath)
    {
        var globalAnalysisSettings = new AnalysisSettings(globalFileExclusions: ImmutableArray.Create(invalidPath));
        globalAnalysisSettings.GlobalFileExclusions.Should().BeEquivalentTo(invalidPath);
        globalAnalysisSettings.SolutionFileExclusions.Should().BeEmpty();
        globalAnalysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo(invalidPath?.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        var solutionAnalysisSettings = new AnalysisSettings(solutionFileExclusions: ImmutableArray.Create(invalidPath));
        solutionAnalysisSettings.GlobalFileExclusions.Should().BeEmpty();
        solutionAnalysisSettings.SolutionFileExclusions.Should().BeEquivalentTo(invalidPath);
        solutionAnalysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo(invalidPath?.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    public static object[][] GetInvalidPaths =>
        Path.GetInvalidPathChars().Cast<object>()
            .Select(invalidChar => new[] { $"C:\\file{invalidChar}.cs" })
            .ToArray<object[]>();
}

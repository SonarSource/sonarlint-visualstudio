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
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Core.UnitTests.UserRuleSettings;

[TestClass]
public class AnalysisSettingsTests
{
    [TestMethod]
    public void AnalysisSettings_FileExclusions_WithNull_ReturnsEmptyArray()
    {
        var analysisSettings = new AnalysisSettings();

        analysisSettings.UserDefinedFileExclusions.Should().BeEmpty();
        analysisSettings.NormalizedFileExclusions.Should().BeEmpty();
    }

    [TestMethod]
    public void AnalysisSettings_FileExclusions_DiscardsEmptyOrNullPaths()
    {
        var analysisSettings = new AnalysisSettings(fileExclusions: ["", " ", null]);

        analysisSettings.UserDefinedFileExclusions.Should().BeEmpty();
        analysisSettings.NormalizedFileExclusions.Should().BeEmpty();
    }

    [TestMethod]
    public void AnalysisSettings_FileExclusions_WithBackslashes_NormalizesToForwardSlashes()
    {
        var analysisSettings = new AnalysisSettings(fileExclusions: ["**\\obj\\*", "a\\file1.cpp", "file2.cpp"]);

        analysisSettings.UserDefinedFileExclusions.Should().BeEquivalentTo("**\\obj\\*", "a\\file1.cpp", "file2.cpp");
        analysisSettings.NormalizedFileExclusions.Should().BeEquivalentTo("**/a/file1.cpp", "**/obj/*", "**/file2.cpp");
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
    public void AnalysisSettings_FileExclusions_TransformsPathCorrectly(string original, string expected)
    {
        var testSubject = new AnalysisSettings(fileExclusions: [original]);

        testSubject.UserDefinedFileExclusions.Should().BeEquivalentTo(original);
        testSubject.NormalizedFileExclusions.Should().BeEquivalentTo(expected);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetInvalidPaths))]
    public void AnalysisSettings_FileExclusions_ContainsInvalidPathCharacters_DoesNotCrashAndDoesNotNormalize(string invalidPath)
    {
        var testSubject = new AnalysisSettings(fileExclusions: [invalidPath]);

        testSubject.UserDefinedFileExclusions.Should().BeEquivalentTo(invalidPath);
        testSubject.NormalizedFileExclusions.Should().BeEquivalentTo(invalidPath?.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    public static object[][] GetInvalidPaths =>
        Path.GetInvalidPathChars().Cast<object>()
            .Select(invalidChar => new[] { $"C:\\file{invalidChar}.cs" })
            .ToArray<object[]>();
}

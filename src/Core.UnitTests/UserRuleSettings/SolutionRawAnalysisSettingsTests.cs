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

using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Core.UnitTests.UserRuleSettings;

[TestClass]
public class SolutionRawAnalysisSettingsTests
{
    [TestMethod]
    public void SolutionAnalysisSettings_SerializesCorrectly()
    {
        var settings = new SolutionRawAnalysisSettings(
            new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } },
            ["file1.cpp", "**/obj/*", "file2.cpp"]);
        const string expectedJson =
            """
            {
              "sonarlint.analyzerProperties": {
                "key2": "value2",
                "key1": "value1"
              },
              "sonarlint.analysisExcludesStandalone": "file1.cpp,**/obj/*,file2.cpp"
            }
            """;

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        json.Should().Be(expectedJson);
    }

    [TestMethod]
    public void SolutionAnalysisSettings_DeserializesCorrectly()
    {
        const string json = """
                            {
                              "sonarlint.analyzerProperties": {
                                "key1": "value1",
                                "key2": "value2"
                              },
                              "sonarlint.analysisExcludesStandalone": "file1.cpp,**/obj/*,file2.cpp"
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<SolutionRawAnalysisSettings>(json);

        settings.AnalysisProperties.Should().BeEquivalentTo(
            new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } });
        settings.UserDefinedFileExclusions.Should().BeEquivalentTo(["file1.cpp", "**/obj/*", "file2.cpp"]);
    }

    [TestMethod]
    public void SolutionAnalysisSettings_FileExclusions_SerializesCorrectly()
    {
        var settings = new SolutionRawAnalysisSettings([], ["file1.cpp", "**/obj/*", "file2.cpp"]);
        const string expectedJson =
            """
            {
              "sonarlint.analyzerProperties": {},
              "sonarlint.analysisExcludesStandalone": "file1.cpp,**/obj/*,file2.cpp"
            }
            """;

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        json.Should().Be(expectedJson);
    }

    [TestMethod]
    public void SolutionAnalysisSettings_FileExclusionsWithSpaces_SerializesCorrectlyAndTrims()
    {
        var settings = new SolutionRawAnalysisSettings([], ["file1.cpp ", " **/My Folder/*", "file2.cpp "]);
        const string expectedJson =
            """
            {
              "sonarlint.analyzerProperties": {},
              "sonarlint.analysisExcludesStandalone": "file1.cpp,**/My Folder/*,file2.cpp"
            }
            """;

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        json.Should().Be(expectedJson);
    }

    [TestMethod]
    public void SolutionAnalysisSettings_FileExclusions_Serializes()
    {
        var settings = new SolutionRawAnalysisSettings();
        const string expectedJson =
            """
            {
              "sonarlint.analyzerProperties": {},
              "sonarlint.analysisExcludesStandalone": ""
            }
            """;

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        json.Should().Be(expectedJson);
    }

    [TestMethod]
    public void SolutionAnalysisSettings_FileExclusionsWithBackslashes_DeserializesCorrectly()
    {
        const string json = """
                            {
                              "sonarlint.analyzerProperties": {},
                              "sonarlint.analysisExcludesStandalone": "a\\file1.cpp,**\\obj\\*,,file2.cpp"
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<SolutionRawAnalysisSettings>(json);

        settings.UserDefinedFileExclusions.Should().BeEquivalentTo("a\\file1.cpp", "**\\obj\\*", "file2.cpp");
    }

    [TestMethod]
    public void SolutionAnalysisSettings_FileExclusionsWithSpaces_DeserializesCorrectly()
    {
        const string json = """
                            {
                              "sonarlint.analyzerProperties": {},
                              "sonarlint.analysisExcludesStandalone": " file1.cpp, **/My Folder/*, file2.cpp "
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<SolutionRawAnalysisSettings>(json);

        settings.UserDefinedFileExclusions.Should().BeEquivalentTo("file1.cpp", "**/My Folder/*", "file2.cpp");
    }

    [TestMethod]
    public void SolutionAnalysisSettings_NullExclusions_DeserializesWithDefaultValue()
    {
        const string json = """
                            {
                              "sonarlint.analyzerProperties": {},
                              "sonarlint.analysisExcludesStandalone": null
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<SolutionRawAnalysisSettings>(json);

        settings.UserDefinedFileExclusions.Should().BeEmpty();
    }

    [TestMethod]
    public void SolutionAnalysisSettings_DeserializesAndIgnoresIfNotString()
    {
        const string json = """
                            {
                              "sonarlint.analyzerProperties": {},
                              "sonarlint.analysisExcludesStandalone": 12
                            }
                            """;

        var act = () => JsonConvert.DeserializeObject<SolutionRawAnalysisSettings>(json);

        act.Should().ThrowExactly<JsonException>().WithMessage(
            string.Format(
                CoreStrings.CommaSeparatedStringArrayConverter_UnexpectedType,
                "System.Int64",
                "System.String",
                "['sonarlint.analysisExcludesStandalone']"));
    }
}

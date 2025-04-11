using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Core.UnitTests.UserRuleSettings;

[TestClass]
public class AnalysisSettingsTests
{
    [TestMethod]
    public void AnalysisSettings_SerializesCorrectly()
    {
        var settings = new AnalysisSettings
        {
            Rules = { { "typescript:S2685", new RuleConfig { Level = RuleLevel.On, Parameters = new Dictionary<string, string> { { "key1", "value1" } } } } },
            FileExclusions = ["file1.cpp", "**/obj/*", "file2.cpp"]
        };
        const string expectedJson =
            """
            {
              "sonarlint.rules": {
                "typescript:S2685": {
                  "level": "On",
                  "parameters": {
                    "key1": "value1"
                  }
                }
              },
              "sonarlint.analysisExcludesStandalone": "file1.cpp,**/obj/*,file2.cpp"
            }
            """;

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        json.Should().Be(expectedJson);
    }

    [TestMethod]
    public void AnalysisSettings_DeserializesCorrectly()
    {
        const string json = """
                            {
                              "sonarlint.rules": {
                                "typescript:S2685": {
                                  "level": "On",
                                  "parameters": {
                                    "key1": "value1"
                                  }
                                }
                              },
                              "sonarlint.analysisExcludesStandalone": "file1.cpp,**/obj/*,file2.cpp"
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<AnalysisSettings>(json);

        settings.Rules.Should().BeEquivalentTo(
            new Dictionary<string, RuleConfig> { { "typescript:S2685", new RuleConfig { Level = RuleLevel.On, Parameters = new Dictionary<string, string> { { "key1", "value1" } } } } });
        settings.FileExclusions.Should().BeEquivalentTo("file1.cpp", "**/obj/*", "file2.cpp");
    }

    [TestMethod]
    public void AnalysisSettings_FileExclusions_SerializesCorrectly()
    {
        var settings = new AnalysisSettings { FileExclusions = ["file1.cpp", "**/obj/*", "file2.cpp"] };
        const string expectedJson =
            """
            {
              "sonarlint.rules": {},
              "sonarlint.analysisExcludesStandalone": "file1.cpp,**/obj/*,file2.cpp"
            }
            """;

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        json.Should().Be(expectedJson);
    }

    [TestMethod]
    public void AnalysisSettings_FileExclusionsWithSpaces_SerializesCorrectlyAndTrims()
    {
        var settings = new AnalysisSettings { FileExclusions = ["file1.cpp ", " **/My Folder/*", "file2.cpp "] };
        const string expectedJson =
            """
            {
              "sonarlint.rules": {},
              "sonarlint.analysisExcludesStandalone": "file1.cpp,**/My Folder/*,file2.cpp"
            }
            """;

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        json.Should().Be(expectedJson);
    }

    [TestMethod]
    public void AnalysisSettings_FileExclusions_Serializes()
    {
        var settings = new AnalysisSettings();
        const string expectedJson =
            """
            {
              "sonarlint.rules": {},
              "sonarlint.analysisExcludesStandalone": ""
            }
            """;

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        json.Should().Be(expectedJson);
    }

    [TestMethod]
    public void AnalysisSettings_FileExclusions_DeserializesCorrectly()
    {
        const string json = """
                            {
                              "sonarlint.rules": {},
                              "sonarlint.analysisExcludesStandalone": "file1.cpp,**/obj/*,,file2.cpp"
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<AnalysisSettings>(json);

        settings.FileExclusions.Should().BeEquivalentTo("file1.cpp", "**/obj/*", "file2.cpp");
    }

    [TestMethod]
    public void AnalysisSettings_FileExclusionsWithSpaces_DeserializesCorrectly()
    {
        const string json = """
                            {
                              "sonarlint.rules": {},
                              "sonarlint.analysisExcludesStandalone": " file1.cpp, **/My Folder/*, file2.cpp "
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<AnalysisSettings>(json);

        settings.FileExclusions.Should().BeEquivalentTo("file1.cpp", "**/My Folder/*", "file2.cpp");
    }


    [TestMethod]
    public void AnalysisSettings_NullExclusions_DeserializesWithDefaultValue()
    {
        const string json = """
                            {
                              "sonarlint.rules": {},
                              "sonarlint.analysisExcludesStandalone": null
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<AnalysisSettings>(json);

        settings.FileExclusions.Should().NotBeNull().And.BeEmpty();
    }

    [TestMethod]
    public void AnalysisSettings_DeserializesAndIgnoresIfNotString()
    {
        const string json = """
                            {
                              "sonarlint.rules": {},
                              "sonarlint.analysisExcludesStandalone": 12
                            }
                            """;

        var act = () => JsonConvert.DeserializeObject<AnalysisSettings>(json);

        act.Should().ThrowExactly<JsonException>().WithMessage(
            string.Format(
                CoreStrings.CommaSeparatedStringArrayConverter_UnexpectedType,
                "System.Int64",
                "System.String",
                "['sonarlint.analysisExcludesStandalone']"));
    }
}

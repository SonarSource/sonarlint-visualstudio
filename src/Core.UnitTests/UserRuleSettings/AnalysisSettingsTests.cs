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
            UserDefinedFileExclusions = ["file1.cpp", "**/obj/*", "file2.cpp"]
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
        settings.UserDefinedFileExclusions.Should().BeEquivalentTo("file1.cpp", "**/obj/*", "file2.cpp");
        settings.NormalizedFileExclusions.Should().BeEquivalentTo("**/file1.cpp", "**/obj/*", "**/file2.cpp");
    }

    [TestMethod]
    public void AnalysisSettings_FileExclusions_SerializesCorrectly()
    {
        var settings = new AnalysisSettings { UserDefinedFileExclusions = ["file1.cpp", "**/obj/*", "file2.cpp"] };
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
        var settings = new AnalysisSettings { UserDefinedFileExclusions = ["file1.cpp ", " **/My Folder/*", "file2.cpp "] };
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
    public void AnalysisSettings_FileExclusionsWithBackslashes_DeserializesCorrectly()
    {
        const string json = """
                            {
                              "sonarlint.rules": {},
                              "sonarlint.analysisExcludesStandalone": "a\\file1.cpp,**\\obj\\*,,file2.cpp"
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<AnalysisSettings>(json);

        settings.UserDefinedFileExclusions.Should().BeEquivalentTo("a\\file1.cpp", "**\\obj\\*", "file2.cpp");
        settings.NormalizedFileExclusions.Should().BeEquivalentTo("**/a/file1.cpp", "**/obj/*", "**/file2.cpp");
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

        settings.UserDefinedFileExclusions.Should().BeEquivalentTo("file1.cpp", "**/My Folder/*", "file2.cpp");
        settings.NormalizedFileExclusions.Should().BeEquivalentTo("**/file1.cpp", "**/My Folder/*", "**/file2.cpp");
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

        settings.UserDefinedFileExclusions.Should().BeEmpty();
        settings.NormalizedFileExclusions.Should().BeEmpty();
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
    public void TransformsPathCorrectly(string original, string expected)
    {
        var testSubject =  new AnalysisSettings{UserDefinedFileExclusions = [original]};

        testSubject.UserDefinedFileExclusions.Should().BeEquivalentTo(original);
        testSubject.NormalizedFileExclusions.Should().BeEquivalentTo(expected);
    }
}

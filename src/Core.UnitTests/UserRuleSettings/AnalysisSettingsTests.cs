using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Core.UnitTests.UserRuleSettings;

[TestClass]
public class AnalysisSettingsTests
{
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
}

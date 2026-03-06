
using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Service.Plugin;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Plugin;

[TestClass]
public class GetPluginStatusesResponseTests
{
    [TestMethod]
    public void Deserialized_AsExpected()
    {
        var expected = new GetPluginStatusesResponse(
            pluginStatuses:
            [
                new PluginStatusDto(
                    pluginName: "Java",
                    state: PluginStateDto.ACTIVE,
                    source: ArtifactSourceDto.EMBEDDED,
                    actualVersion: "1.2.3",
                    overriddenVersion: null),
                new PluginStatusDto(
                    pluginName: "C/C++/Objective-C",
                    state: PluginStateDto.SYNCED,
                    source: ArtifactSourceDto.SONARQUBE_SERVER,
                    actualVersion: "4.5.6",
                    overriddenVersion: "3.0.0")
            ]);

        const string serialized = """
                                  {
                                    pluginStatuses: [
                                      {
                                        pluginName: "Java",
                                        state: "ACTIVE",
                                        source: "EMBEDDED",
                                        actualVersion: "1.2.3",
                                        overriddenVersion: null
                                      },
                                      {
                                        pluginName: "C/C++/Objective-C",
                                        state: "SYNCED",
                                        source: "SONARQUBE_SERVER",
                                        actualVersion: "4.5.6",
                                        overriddenVersion: "3.0.0"
                                      }
                                    ]
                                  }
                                  """;

        var actual = JsonConvert.DeserializeObject<GetPluginStatusesResponse>(serialized);

        actual
            .Should()
            .BeEquivalentTo(expected,
                options =>
                    options
                        .ComparingByMembers<GetPluginStatusesResponse>());
    }
}

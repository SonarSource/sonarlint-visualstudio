using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Service.Hotspot;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Hotspot;

[TestClass]
public class CheckStatusChangePermittedParamsTests
{
    [TestMethod]
    public void Serialized_AsExpected()
    {
        const string expected = """
                                {
                                  "connectionId": "CONNECTION_ID",
                                  "hotspotKey": "hotspotKey"
                                }
                                """;

        var checkStatusChangePermittedParams = new CheckStatusChangePermittedParams("CONNECTION_ID", "hotspotKey");

        JsonConvert.SerializeObject(checkStatusChangePermittedParams, Formatting.Indented).Should().Be(expected);
    }
}

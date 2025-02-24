using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Visualization;

[TestClass]
public class OpenUrlInBrowserParamsTests
{
    [TestMethod]
    public void Serialize_AsExpected()
    {
        var openUrlInBrowserParams = new OpenUrlInBrowserParams("http://localhost:9000/home");
        var expected = """
                       {
                         "url": "http://localhost:9000/home"
                       }
                       """;

        var actual = JsonConvert.SerializeObject(openUrlInBrowserParams, Formatting.Indented);

        actual.Should().Be(expected);
    }

    [TestMethod]
    public void Deserialize_AsExpected()
    {
        var expected = new OpenUrlInBrowserParams("http://localhost:9000/home");
        var serialized = """
                         {
                           "url": "http://localhost:9000/home"
                         }
                         """;

        var actual = JsonConvert.DeserializeObject<OpenUrlInBrowserParams>(serialized);

        actual.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<OpenUrlInBrowserParams>());
    }
}

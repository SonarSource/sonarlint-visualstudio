using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Service.Plugin;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Plugin;

[TestClass]
public class GetPluginStatusesParamsTests
{
    [TestMethod]
    public void Serialized_WithConfigurationScopeId_AsExpected()
    {
        var param = new GetPluginStatusesParams(configurationScopeId: "my-scope");

        var actual = JsonConvert.SerializeObject(param);

        actual.Should().Be("""{"configurationScopeId":"my-scope"}""");
    }

    [TestMethod]
    public void Serialized_WithNullConfigurationScopeId_AsExpected()
    {
        var param = new GetPluginStatusesParams(configurationScopeId: null);

        var actual = JsonConvert.SerializeObject(param);

        actual.Should().Be("""{"configurationScopeId":null}""");
    }
}

namespace SonarLint.VisualStudio.Core.UnitTests;

[TestClass]
public class PluginInfoTests
{
    [TestMethod]
    public void PluginInfo_NullKey_ThrowsException()
    {
        Action act = () => new PluginInfo(null, "pattern");
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("pluginKey");
    }

    [TestMethod]
    public void PluginInfo_NullFilePattern_DoesNotThrow()
    {
        Action act = () => new PluginInfo("csharp", null);
        act.Should().NotThrow<ArgumentNullException>();
    }

    [TestMethod]
    [DataRow("csharp", "csharp-plugin-(\\d+\\).jar", true)]
    [DataRow("vbnet", "vbnet-plugin-(\\d+\\).jar", false)]
    public void PluginInfo_InitializesParameters(string key, string pattern, bool isEnabled)
    {
        var pluginInfo = new PluginInfo(key, pattern, isEnabled);

        pluginInfo.Key.Should().Be(key);
        pluginInfo.FilePattern.Should().Be(pattern);
        pluginInfo.IsEnabledForAnalysis.Should().Be(isEnabled);
    }

    [TestMethod]
    public void PluginInfo_IsEnabledDefaultsToTrue()
    {
        var pluginInfo = new PluginInfo("csharp", "csharp-plugin-(\\d+\\).jar");

        pluginInfo.IsEnabledForAnalysis.Should().Be(true);
    }
}

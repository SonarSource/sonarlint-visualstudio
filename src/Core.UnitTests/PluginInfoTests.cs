namespace SonarLint.VisualStudio.Core.UnitTests;

[TestClass]
public class PluginInfoTests
{
    [TestMethod]
    public void PluginInfo_NullPluginKey_ThrowsException()
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
}

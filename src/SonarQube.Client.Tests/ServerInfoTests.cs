namespace SonarQube.Client.Tests;

[TestClass]
public class ServerInfoTests
{
    [TestMethod]
    public void ServerInfo_Constructor_ValidParameters()
    {
        var version = new Version(9, 0);
        var serverType = ServerType.SonarQube;

        var serverInfo = new ServerInfo(version, serverType);

        serverInfo.Version.Should().Be(version);
        serverInfo.ServerType.Should().Be(serverType);
    }

    [TestMethod]
    [DataRow(ServerType.SonarCloud, "SonarQube Cloud")]
    [DataRow(ServerType.SonarQube, "SonarQube Server")]
    public void ServerTypeExtensions_ToProductName_SonarQube(ServerType serverType, string expectedProductName) => serverType.ToProductName().Should().Be(expectedProductName);
}

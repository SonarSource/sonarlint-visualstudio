/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Persistence;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.Core.Binding.ServerConnection;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class ServerConnectionsRepositoryTests
{
    private ServerConnectionsRepository testSubject;
    private IJsonFileHandler jsonFileHandler;
    private ILogger logger;
    private IEnvironmentVariableProvider environmentVariableProvider;
    private IServerConnectionModelMapper serverConnectionModelMapper;
    private ISolutionBindingCredentialsLoader credentialsLoader;
    private readonly SonarCloud sonarCloudServerConnection = new("myOrganization", new ServerConnectionSettings(true), Substitute.For<ICredentials>());
    private readonly ServerConnection.SonarQube sonarQubeServerConnection = new(new Uri("http://localhost"), new ServerConnectionSettings(true), Substitute.For<ICredentials>());
    private IFileSystem fileSystem;

    [TestInitialize]
    public void TestInitialize()
    { 
        jsonFileHandler = Substitute.For<IJsonFileHandler>();
        serverConnectionModelMapper = Substitute.For<IServerConnectionModelMapper>();
        credentialsLoader = Substitute.For<ISolutionBindingCredentialsLoader>();
        environmentVariableProvider = Substitute.For<IEnvironmentVariableProvider>();
        logger = Substitute.For<ILogger>();
        fileSystem = Substitute.For<IFileSystem>();

        testSubject = new ServerConnectionsRepository(jsonFileHandler, serverConnectionModelMapper, credentialsLoader, environmentVariableProvider, fileSystem, logger);
    }

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<ServerConnectionsRepository, IServerConnectionsRepository>(
            MefTestHelpers.CreateExport<IJsonFileHandler>(),
            MefTestHelpers.CreateExport<IServerConnectionModelMapper>(),
            MefTestHelpers.CreateExport<ICredentialStoreService>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void Mef_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ServerConnectionsRepository>();
    }

    [TestMethod]
    public void TryGet_FileDoesNotExist_ReturnsFalse()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.When(x => x.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>())).Do(x => throw new FileNotFoundException());

        var succeeded = testSubject.TryGet("myId", out ServerConnection serverConnection);

        succeeded.Should().BeFalse();
        serverConnection.Should().BeNull();
        jsonFileHandler.Received(1).ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>());
    }

    [TestMethod]
    public void TryGet_FileCanNotBeRead_ReturnsFalse()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.When(x => x.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>())).Do(x => throw new Exception());

        var succeeded = testSubject.TryGet("myId", out ServerConnection serverConnection);

        succeeded.Should().BeFalse();
        serverConnection.Should().BeNull();
        jsonFileHandler.Received(1).ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>());
    }

    [TestMethod]
    public void TryGet_FileExistsAndConnectionDoesNotExist_ReturnsFalse()
    {
        MockFileWithOneSonarCloudConnection();

        var succeeded = testSubject.TryGet("non-existing connectionId", out ServerConnection serverConnection);

        succeeded.Should().BeFalse();
        serverConnection.Should().BeNull();
    }

    [TestMethod]
    public void TryGet_FileExistsAndConnectionIsSonarCloud_ReturnsSonarCloudConnection()
    {
        var sonarCloudModel = GetSonarCloudJsonModel("myOrg");
        var expectedConnection = new SonarCloud(sonarCloudModel.Id);
        MockReadingFile(new ServerConnectionsListJsonModel { ServerConnections = [sonarCloudModel] });
        serverConnectionModelMapper.GetServerConnection(sonarCloudModel).Returns(expectedConnection);

        var succeeded = testSubject.TryGet(sonarCloudModel.Id, out ServerConnection serverConnection);

        succeeded.Should().BeTrue();
        serverConnection.Should().Be(expectedConnection);
    }

    [TestMethod]
    public void TryGet_FileExistsAndConnectionIsSonarCloud_FillsCredentials()
    {
        var expectedConnection = MockFileWithOneSonarCloudConnection();
        var credentials = Substitute.For<ICredentials>();
        credentialsLoader.Load(expectedConnection.CredentialsUri).Returns(credentials);

        var succeeded = testSubject.TryGet(expectedConnection.Id, out ServerConnection serverConnection);

        succeeded.Should().BeTrue();
        serverConnection.Should().Be(expectedConnection);
        serverConnection.Credentials.Should().Be(credentials);
        credentialsLoader.Received(1).Load(expectedConnection.CredentialsUri);
    }

    [TestMethod]
    public void TryGet_FileExistsAndConnectionIsSonarQube_ReturnsSonarQubeConnection()
    {
        var sonarQubeModel = GetSonarQubeJsonModel(new Uri("http://localhost:9000"));
        var expectedConnection = new ServerConnection.SonarQube(new Uri(sonarQubeModel.ServerUri));
        MockReadingFile(new ServerConnectionsListJsonModel { ServerConnections = [sonarQubeModel] });
        serverConnectionModelMapper.GetServerConnection(sonarQubeModel).Returns(expectedConnection);

        var succeeded = testSubject.TryGet(sonarQubeModel.Id, out ServerConnection serverConnection);

        succeeded.Should().BeTrue();
        serverConnection.Should().Be(expectedConnection);
    }

    [TestMethod]
    public void TryGet_FileExistsAndConnectionIsSonarQube_FillsCredentials()
    {
        var expectedConnection = MockFileWithOneSonarQubeConnection();
        var credentials = Substitute.For<ICredentials>();
        credentialsLoader.Load(expectedConnection.CredentialsUri).Returns(credentials);

        var succeeded = testSubject.TryGet(expectedConnection.Id, out ServerConnection serverConnection);

        succeeded.Should().BeTrue();
        serverConnection.Should().Be(expectedConnection);
        serverConnection.Credentials.Should().Be(credentials);
        credentialsLoader.Received(1).Load(expectedConnection.CredentialsUri);
    }

    [TestMethod]
    public void TryGetAll_FileDoesNotExist_ReturnsEmptyList()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.When(x => x.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>())).Do(x => throw new FileNotFoundException());

       testSubject.TryGetAll(out var connections);

        connections.Should().BeEmpty();
    }

    [TestMethod]
    public void TryGetAll_FileCouldNotBeRead_ThrowsException()
    {
        var exceptionMsg = "failed";
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.When(x => x.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>())).Do(x => throw new Exception(exceptionMsg));

        var succeeded = testSubject.TryGetAll(out var connections);

        succeeded.Should().BeFalse();
    }

    [TestMethod]
    public void TryGetAll_FileExistsAndIsEmpty_ReturnsEmptyList()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());

       testSubject.TryGetAll(out var connections);

        connections.Should().BeEmpty();
    }

    [TestMethod]
    public void TryGetAll_FileExistsAndHasConnection_MapsModel()
    {
        var cloudModel = GetSonarCloudJsonModel("myOrg");
        MockReadingFile(new ServerConnectionsListJsonModel { ServerConnections = [cloudModel] });

        testSubject.TryGetAll(out _);

        serverConnectionModelMapper.Received(1).GetServerConnection(cloudModel);
    }

    [TestMethod]
    public void TryGetAll_ConnectionsExist_DoesNotFillCredentials()
    {
        MockFileWithOneSonarCloudConnection();

        testSubject.TryGetAll(out _);

        credentialsLoader.DidNotReceive().Load(Arg.Any<Uri>());
    }

    [TestMethod]
    public void TryAdd_FileCouldNotBeRead_DoesNotAddConnection()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());

        var succeeded = testSubject.TryAdd(sonarCloudServerConnection);

        succeeded.Should().BeFalse();
        Received.InOrder(() =>
        {
            jsonFileHandler.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>());
            serverConnectionModelMapper.GetServerConnectionsListJsonModel(Arg.Is<IEnumerable<ServerConnection>>(x => x.Contains(sonarCloudServerConnection)));
            jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>());
        });
    }

    [TestMethod]
    public void TryAdd_FileExistsAndConnectionIsNew_AddsConnection()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(true);

        var succeeded = testSubject.TryAdd(sonarCloudServerConnection);

        succeeded.Should().BeTrue();
        Received.InOrder(() =>
        {
            jsonFileHandler.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>());
            serverConnectionModelMapper.GetServerConnectionsListJsonModel(Arg.Is<IEnumerable<ServerConnection>>(x => x.Contains(sonarCloudServerConnection)));
            jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>());
        });
    }

    [TestMethod]
    public void TryAdd_SonarCloudConnectionIsAddedAndCredentialsAreNotNull_SavesCredentials()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(true);

        var succeeded = testSubject.TryAdd(sonarCloudServerConnection);

        succeeded.Should().BeTrue();
        credentialsLoader.Received(1).Save(sonarCloudServerConnection.Credentials, sonarCloudServerConnection.CredentialsUri);
    }

    [TestMethod]
    public void TryAdd_SonarQubeConnectionIsAddedAndCredentialsAreNotNull_SavesCredentials()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(true);

        var succeeded = testSubject.TryAdd(sonarQubeServerConnection);

        succeeded.Should().BeTrue();
        credentialsLoader.Received(1).Save(sonarQubeServerConnection.Credentials, sonarQubeServerConnection.CredentialsUri);
    }

    [TestMethod]
    public void TryAdd_ConnectionIsAddedAndCredentialsAreNull_ReturnsFalse()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(true);
        sonarCloudServerConnection.Credentials = null;

        var succeeded = testSubject.TryAdd(sonarCloudServerConnection);

        succeeded.Should().BeFalse();
        credentialsLoader.DidNotReceive().Save(Arg.Any<ICredentials>(), Arg.Any<Uri>());
    }

    [TestMethod]
    public void TryAdd_ConnectionIsNotAdded_DoesNotSaveCredentials()
    {
        var sonarCloud = MockFileWithOneSonarCloudConnection();

        var succeeded = testSubject.TryAdd(sonarCloud);

        succeeded.Should().BeFalse();
        credentialsLoader.DidNotReceive().Save(Arg.Any<ICredentials>(), Arg.Any<Uri>());
    }

    [TestMethod]
    public void TryAdd_FileExistsAndConnectionIsDuplicate_DoesNotAddConnection()
    {
        var sonarCloud = MockFileWithOneSonarCloudConnection();

        var succeeded = testSubject.TryAdd(sonarCloud);

        succeeded.Should().BeFalse();
        jsonFileHandler.DidNotReceive().TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>());
    }

    [TestMethod]
    public void TryAdd_WritingToFileFails_DoesNotAddConnection()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(false);
        var sonarCloud = new SonarCloud("myOrg");

        var succeeded = testSubject.TryAdd(sonarCloud);

        succeeded.Should().BeFalse();
    }

    [TestMethod]
    public void TryAdd_WritingThrowsException_DoesNotUpdateConnectionAndWritesLog()
    {
        var exceptionMsg = "IO exception";
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.When(x => x.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>())).Do(x => throw new Exception(exceptionMsg));

        var succeeded = testSubject.TryAdd(sonarCloudServerConnection);

        succeeded.Should().BeFalse();
        logger.Received(1).WriteLine($"Failed updating the {ServerConnectionsRepository.ConnectionsFileName}: {exceptionMsg}");
    }

    [TestMethod]
    public void TryDelete_FileCouldNotBeRead_ReturnsFalse()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.When(x => x.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>())).Do(x => throw new Exception());

        var succeeded = testSubject.TryDelete("myOrg");

        succeeded.Should().BeFalse();
        jsonFileHandler.DidNotReceive().TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>());
    }

    [TestMethod]
    public void TryDelete_FileExistsAndHasNoConnection_ReturnsFalse()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());

        var succeeded = testSubject.TryDelete("myOrg");

        succeeded.Should().BeFalse();
        jsonFileHandler.DidNotReceive().TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>());
    }

    [TestMethod]
    public void TryDelete_FileExistsAndConnectionExists_RemovesConnection()
    {
        var sonarCloud = MockFileWithOneSonarCloudConnection();
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(true);

        var succeeded = testSubject.TryDelete(sonarCloud.Id);

        succeeded.Should().BeTrue();
        Received.InOrder(() =>
        {
            jsonFileHandler.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>());
            serverConnectionModelMapper.GetServerConnectionsListJsonModel(Arg.Is<IEnumerable<ServerConnection>>(x => !x.Any()));
            jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>());
            credentialsLoader.DeleteCredentials(sonarCloud.CredentialsUri);
        });
    }

    [TestMethod]
    public void TryDelete_SonarCloudConnectionWasRemoved_RemovesCredentials()
    {
        var sonarCloud = MockFileWithOneSonarCloudConnection();
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(true);

        var succeeded = testSubject.TryDelete(sonarCloud.Id);

        succeeded.Should().BeTrue();
        credentialsLoader.Received(1).DeleteCredentials(sonarCloud.CredentialsUri);
    }

    [TestMethod]
    public void TryDelete_SonarQubeConnectionWasRemoved_RemovesCredentials()
    {
        var sonarQube = MockFileWithOneSonarQubeConnection();
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(true);

        var succeeded = testSubject.TryDelete(sonarQube.Id);

        succeeded.Should().BeTrue();
        credentialsLoader.Received(1).DeleteCredentials(sonarQube.CredentialsUri);
    }

    [TestMethod]
    public void TryDelete_ConnectionWasNotRemoved_DoesNotRemoveCredentials()
    {
        var sonarCloud = MockFileWithOneSonarCloudConnection();
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(false);

        var succeeded = testSubject.TryDelete(sonarCloud.Id);

        succeeded.Should().BeFalse();
        credentialsLoader.DidNotReceive().DeleteCredentials(Arg.Any<Uri>());
    }

    [TestMethod]
    public void TryDelete_WritingToFileFails_ReturnsFalse()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(false);

        var succeeded = testSubject.TryDelete("myOrg");

        succeeded.Should().BeFalse();
    }

    [TestMethod]
    public void TryDelete_WritingThrowsException_DoesNotUpdateConnectionAndWritesLog()
    {
        var exceptionMsg = "IO exception";
        var sonarCloud = MockFileWithOneSonarCloudConnection();
        jsonFileHandler.When(x => x.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>())).Do(x => throw new Exception(exceptionMsg));

        var succeeded = testSubject.TryDelete(sonarCloud.Id);

        succeeded.Should().BeFalse();
        logger.Received(1).WriteLine($"Failed updating the {ServerConnectionsRepository.ConnectionsFileName}: {exceptionMsg}");
    }

    [TestMethod]
    public void TryUpdateSettingsById_FileCouldNotBeRead_ReturnsFalse()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.When(x => x.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>())).Do(x => throw new Exception());

        var succeeded = testSubject.TryUpdateSettingsById("myOrg", new ServerConnectionSettings(true));

        succeeded.Should().BeFalse();
        jsonFileHandler.DidNotReceive().TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>());
    }

    [TestMethod]
    public void TryUpdateSettingsById_FileExistsAndHasNoConnection_ReturnsFalse()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());

        var succeeded = testSubject.TryUpdateSettingsById("myOrg", new ServerConnectionSettings(true));

        succeeded.Should().BeFalse();
        jsonFileHandler.DidNotReceive().TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>());
    }

    [TestMethod]
    [DataRow(false, true)]
    [DataRow(true, false)]
    public void TryUpdateSettingsById_FileExistsAndConnectionExists_UpdatesSettings(bool oldSmartNotifications, bool newSmartNotifications)
    {
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(true);
        MockFileWithOneSonarCloudConnection(oldSmartNotifications);

        var succeeded = testSubject.TryUpdateSettingsById("myOrg", new ServerConnectionSettings(newSmartNotifications));

        succeeded.Should().BeTrue();
        Received.InOrder(() =>
        {
            jsonFileHandler.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>());
            serverConnectionModelMapper.GetServerConnectionsListJsonModel(Arg.Is<IEnumerable<ServerConnection>>(x => x.Count() == 1 && x.Single().Settings.IsSmartNotificationsEnabled == newSmartNotifications));
            jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>());
        });
    }

    [TestMethod]
    public void TryUpdateSettingsById_WritingToFileFails_DoesNotUpdateConnection()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());
        jsonFileHandler.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>()).Returns(false);

        var succeeded = testSubject.TryUpdateSettingsById("myOrg", new ServerConnectionSettings(true));

        succeeded.Should().BeFalse();
    }

    [TestMethod]
    public void TryUpdateSettingsById_WritingThrowsException_DoesNotUpdateConnectionAndWritesLog()
    {
        var exceptionMsg = "IO exception";
        var sonarCloud = MockFileWithOneSonarCloudConnection();
        jsonFileHandler.When(x => x.TryWriteToFile(Arg.Any<string>(), Arg.Any<ServerConnectionsListJsonModel>())).Do(x => throw new Exception(exceptionMsg));

        var succeeded = testSubject.TryUpdateSettingsById(sonarCloud.Id, new ServerConnectionSettings(true));

        succeeded.Should().BeFalse();
        logger.Received(1).WriteLine($"Failed updating the {ServerConnectionsRepository.ConnectionsFileName}: {exceptionMsg}");
    }

    [TestMethod]
    public void TryUpdateCredentialsById_ConnectionDoesNotExist_DoesNotUpdateCredentials()
    {
        MockReadingFile(new ServerConnectionsListJsonModel());

        var succeeded = testSubject.TryUpdateCredentialsById("myConn", Substitute.For<ICredentials>());

        succeeded.Should().BeFalse();
        credentialsLoader.DidNotReceive().Save(Arg.Any<ICredentials>(), Arg.Any<Uri>());
    }

    [TestMethod]
    public void TryUpdateCredentialsById_SonarCloudConnectionExists_UpdatesCredentials()
    {
        var sonarCloud = MockFileWithOneSonarCloudConnection();
        var newCredentials = Substitute.For<ICredentials>();

        var succeeded = testSubject.TryUpdateCredentialsById(sonarCloud.Id, newCredentials);

        succeeded.Should().BeTrue();
        credentialsLoader.Received(1).Save(newCredentials, sonarCloud.CredentialsUri);
    }

    [TestMethod]
    public void TryUpdateCredentialsById_SonarQubeConnectionExists_UpdatesCredentials()
    {
        var sonarQube = MockFileWithOneSonarQubeConnection();
        var newCredentials = Substitute.For<ICredentials>();

        var succeeded = testSubject.TryUpdateCredentialsById(sonarQube.Id, newCredentials);

        succeeded.Should().BeTrue();
        credentialsLoader.Received(1).Save(newCredentials, sonarQube.ServerUri);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void IsConnectionsFileExisting_ReturnsTrueOnlyIfTheConnectionsFileExists(bool fileExists)
    {
        fileSystem.File.Exists(Arg.Any<string>()).Returns(fileExists);

        var result = testSubject.IsConnectionsFileExisting();

        result.Should().Be(fileExists);
    }

    [TestMethod]
    public void TryUpdateCredentialsById_SavingCredentialsThrows_ReturnsFalseAndLogs()
    {
        var exceptionMsg = "failed";
        var connection = MockFileWithOneSonarCloudConnection();
        credentialsLoader.When(x => x.Save(Arg.Any<ICredentials>(), Arg.Any<Uri>())).Do(x => throw new Exception(exceptionMsg));

        var succeeded = testSubject.TryUpdateCredentialsById(connection.Id, Substitute.For<ICredentials>());

        succeeded.Should().BeFalse();
        logger.Received(1).WriteLine($"Failed updating credentials: {exceptionMsg}");
    }

    private SonarCloud MockFileWithOneSonarCloudConnection(bool isSmartNotificationsEnabled = true)
    {
        var sonarCloudModel = GetSonarCloudJsonModel("myOrg", isSmartNotificationsEnabled);
        var sonarCloud = new SonarCloud(sonarCloudModel.Id, sonarCloudModel.Settings, Substitute.For<ICredentials>());
        MockReadingFile(new ServerConnectionsListJsonModel { ServerConnections = [sonarCloudModel] });
        serverConnectionModelMapper.GetServerConnection(sonarCloudModel).Returns(sonarCloud);
        
        return sonarCloud;
    }

    private ServerConnection.SonarQube MockFileWithOneSonarQubeConnection(bool isSmartNotificationsEnabled = true)
    {
        var sonarQubeModel = GetSonarQubeJsonModel(new Uri("http://localhost"), isSmartNotificationsEnabled);
        var sonarQube = new ServerConnection.SonarQube(new Uri(sonarQubeModel.ServerUri), sonarQubeModel.Settings, Substitute.For<ICredentials>());
        MockReadingFile(new ServerConnectionsListJsonModel { ServerConnections = [sonarQubeModel] });
        serverConnectionModelMapper.GetServerConnection(sonarQubeModel).Returns(sonarQube);

        return sonarQube;
    }

    private void MockReadingFile(ServerConnectionsListJsonModel modelToReturn)
    {
        jsonFileHandler.ReadFile<ServerConnectionsListJsonModel>(Arg.Any<string>()).Returns(modelToReturn);
    }

    private static ServerConnectionJsonModel GetSonarCloudJsonModel(string id, bool isSmartNotificationsEnabled = false)
    {
        return new ServerConnectionJsonModel
        {
            Id = id,
            OrganizationKey = id,
            Settings = new ServerConnectionSettings(isSmartNotificationsEnabled)
        };
    }

    private static ServerConnectionJsonModel GetSonarQubeJsonModel(Uri id, bool isSmartNotificationsEnabled = false)
    {
        return new ServerConnectionJsonModel
        {
            Id = id.ToString(),
            ServerUri = id.ToString(),
            Settings = new ServerConnectionSettings(isSmartNotificationsEnabled)
        };
    }
   
}

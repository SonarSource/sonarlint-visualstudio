/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class SolutionBindingRepositoryTests
{
    private const string MockFilePath = "test file path";
    private const string LocalBindingKey = "my solution";

    private BindingJsonModel bindingJsonModel;
    private IBindingJsonModelConverter bindingJsonModelConverter;
    private BoundServerProject boundServerProject;
    private ISolutionBindingCredentialsLoader credentialsLoader;
    private TestLogger logger;

    private UsernameAndPasswordCredentials mockCredentials;
    private ServerConnection serverConnection;
    private IServerConnectionsRepository serverConnectionsRepository;
    private ISolutionBindingFileLoader solutionBindingFileLoader;
    private ISolutionBindingRepository testSubject;
    private IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        unintrusiveBindingPathProvider = Substitute.For<IUnintrusiveBindingPathProvider>();
        bindingJsonModelConverter = Substitute.For<IBindingJsonModelConverter>();
        serverConnectionsRepository = Substitute.For<IServerConnectionsRepository>();
        credentialsLoader = Substitute.For<ISolutionBindingCredentialsLoader>();
        solutionBindingFileLoader = Substitute.For<ISolutionBindingFileLoader>();
        logger = new TestLogger();

        testSubject = new SolutionBindingRepository(unintrusiveBindingPathProvider, bindingJsonModelConverter, serverConnectionsRepository, credentialsLoader, solutionBindingFileLoader, logger);

        mockCredentials = new UsernameAndPasswordCredentials("user", "pwd".ToSecureString());

        serverConnection = new ServerConnection.SonarCloud("org");
        boundServerProject = new BoundServerProject("solution.123", "project_123", serverConnection);
        bindingJsonModel = new BindingJsonModel { ServerConnectionId = serverConnection.Id };
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SolutionBindingRepository, ISolutionBindingRepository>(
            MefTestHelpers.CreateExport<IUnintrusiveBindingPathProvider>(),
            MefTestHelpers.CreateExport<IBindingJsonModelConverter>(),
            MefTestHelpers.CreateExport<IServerConnectionsRepository>(),
            MefTestHelpers.CreateExport<ISolutionBindingCredentialsLoader>(),
            MefTestHelpers.CreateExport<ISolutionBindingFileLoader>(),
            MefTestHelpers.CreateExport<ILogger>());
        MefTestHelpers.CheckTypeCanBeImported<SolutionBindingRepository, ILegacySolutionBindingRepository>(
            MefTestHelpers.CreateExport<IUnintrusiveBindingPathProvider>(),
            MefTestHelpers.CreateExport<IBindingJsonModelConverter>(),
            MefTestHelpers.CreateExport<IServerConnectionsRepository>(),
            MefTestHelpers.CreateExport<ISolutionBindingCredentialsLoader>(),
            MefTestHelpers.CreateExport<ISolutionBindingFileLoader>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SolutionBindingRepository>();

    [TestMethod]
    public void Read_ProjectIsNull_Null()
    {
        solutionBindingFileLoader.Load(MockFilePath).Returns(null as BindingJsonModel);

        var actual = testSubject.Read(MockFilePath);
        actual.Should().Be(null);
    }

    [TestMethod]
    public void Read_ProjectIsNull_CredentialsNotRead()
    {
        solutionBindingFileLoader.Load(MockFilePath).Returns(null as BindingJsonModel);

        testSubject.Read(MockFilePath);

        credentialsLoader.DidNotReceiveWithAnyArgs().Load(default);
    }

    [TestMethod]
    public void Read_ProjectIsNotNull_ReadsConnectionRepositoryForConnection()
    {
        serverConnectionsRepository.TryGet(bindingJsonModel.ServerConnectionId, out Arg.Any<ServerConnection>()).Returns(call =>
        {
            call[1] = serverConnection;
            return true;
        });
        solutionBindingFileLoader.Load(MockFilePath).Returns(bindingJsonModel);
        unintrusiveBindingPathProvider.GetBindingKeyFromPath(MockFilePath).Returns(boundServerProject.LocalBindingKey);
        bindingJsonModelConverter.ConvertFromModel(bindingJsonModel, serverConnection, boundServerProject.LocalBindingKey).Returns(boundServerProject);

        var actual = testSubject.Read(MockFilePath);

        actual.Should().BeSameAs(boundServerProject);

        credentialsLoader.DidNotReceiveWithAnyArgs().Load(default);
    }

    [TestMethod]
    public void Read_ProjectIsNotNull_NoConnection_ReturnsNull()
    {
        serverConnectionsRepository.TryGet(bindingJsonModel.ServerConnectionId, out Arg.Any<ServerConnection>()).Returns(call =>
        {
            call[1] = null;
            return false;
        });
        solutionBindingFileLoader.Load(MockFilePath).Returns(bindingJsonModel);

        var actual = testSubject.Read(MockFilePath);

        credentialsLoader.DidNotReceiveWithAnyArgs().Load(default);
        actual.Should().BeNull();
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void Write_ConfigFilePathIsNull_ReturnsFalse(string filePath)
    {
        var actual = testSubject.Write(filePath, boundServerProject);
        actual.Should().Be(false);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void Write_ConfigFilePathIsNull_FileNotWritten(string filePath)
    {
        testSubject.Write(filePath, boundServerProject);

        solutionBindingFileLoader.DidNotReceiveWithAnyArgs().Save(default, default);
    }

    [TestMethod]
    public void Write_ProjectIsNull_Exception()
    {
        Action act = () => testSubject.Write(MockFilePath, null);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Write_ProjectIsNull_FileNotWritten()
    {
        Action act = () => testSubject.Write(MockFilePath, null);

        act.Should().Throw<ArgumentNullException>();

        solutionBindingFileLoader.DidNotReceiveWithAnyArgs().Save(default, default);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Write_EventTriggered_DependingOnFileWriteStatus(bool triggered)
    {
        var eventHandler = Substitute.For<EventHandler>();
        testSubject.BindingUpdated += eventHandler;
        bindingJsonModelConverter.ConvertToModel(boundServerProject).Returns(bindingJsonModel);
        solutionBindingFileLoader.Save(MockFilePath, bindingJsonModel).Returns(triggered);

        testSubject.Write(MockFilePath, boundServerProject);

        eventHandler.ReceivedWithAnyArgs(triggered ? 1 : 0).Invoke(default, default);
    }

    [TestMethod]
    public void Write_FileWritten_NoOnSaveCallback_NoException()
    {
        bindingJsonModelConverter.ConvertToModel(boundServerProject).Returns(bindingJsonModel);
        solutionBindingFileLoader.Save(MockFilePath, bindingJsonModel).Returns(true);

        Action act = () => testSubject.Write(MockFilePath, boundServerProject);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void List_FilesExist_Returns()
    {
        var connection1 = new ServerConnection.SonarCloud("org");
        var connection2 = new ServerConnection.SonarQube(new Uri("http://localhost/"));
        var bindingConfig1 = "C:\\Bindings\\solution1\\binding.config";
        var solution1 = "solution1";
        var bindingConfig2 = "C:\\Bindings\\solution2\\binding.config";
        var solution2 = "solution2";
        SetUpUnintrusiveBindingPathProvider(bindingConfig1, bindingConfig2);
        SetUpConnections(connection1, connection2);
        var boundServerProject1 = SetUpBinding(solution1, connection1, bindingConfig1);
        var boundServerProject2 = SetUpBinding(solution2, connection2, bindingConfig2);

        var result = testSubject.List();

        result.Should().BeEquivalentTo(boundServerProject1, boundServerProject2);
    }

    [TestMethod]
    public void List_SkipsBindingsWithoutConnections()
    {
        var connection1 = new ServerConnection.SonarCloud("org");
        var connection2 = new ServerConnection.SonarQube(new Uri("http://localhost/"));
        var bindingConfig1 = "C:\\Bindings\\solution1\\binding.config";
        var solution1 = "solution1";
        var bindingConfig2 = "C:\\Bindings\\solution2\\binding.config";
        var solution2 = "solution2";
        SetUpUnintrusiveBindingPathProvider(bindingConfig1, bindingConfig2);
        SetUpConnections(connection2); // only one connection
        _ = SetUpBinding(solution1, null, bindingConfig1);
        var boundServerProject2 = SetUpBinding(solution2, connection2, bindingConfig2);

        var result = testSubject.List();

        result.Should().BeEquivalentTo(boundServerProject2);
    }

    [TestMethod]
    public void List_SkipsBindingsThatCannotBeRead()
    {
        var connection1 = new ServerConnection.SonarCloud("org");
        var connection2 = new ServerConnection.SonarQube(new Uri("http://localhost/"));
        var bindingConfig1 = "C:\\Bindings\\solution1\\binding.config";
        var solution1 = "solution1";
        var bindingConfig2 = "C:\\Bindings\\solution2\\binding.config";
        SetUpUnintrusiveBindingPathProvider(bindingConfig1, bindingConfig2);
        SetUpConnections(connection1, connection2);
        var boundServerProject1 = SetUpBinding(solution1, connection1, bindingConfig1);
        solutionBindingFileLoader.Load(bindingConfig2).Returns((BindingJsonModel)null);

        var result = testSubject.List();

        result.Should().BeEquivalentTo(boundServerProject1);
    }

    [TestMethod]
    public void List_CannotGetConnections_EmptyList()
    {
        serverConnectionsRepository.TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>()).Returns(false);

        var act = () => testSubject.List().ToList();

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void LegacyRead_NoFile_ReturnsNull()
    {
        solutionBindingFileLoader.Load(MockFilePath).Returns((BindingJsonModel)null);

        ((ILegacySolutionBindingRepository)testSubject).Read(MockFilePath).Should().BeNull();
        credentialsLoader.DidNotReceiveWithAnyArgs().Load(default);
    }

    [TestMethod]
    public void LegacyRead_ValidBinding_LoadsCredentials()
    {
        var boundSonarQubeProject = new BoundSonarQubeProject();
        bindingJsonModel.ServerUri = new Uri("http://localhost/");
        credentialsLoader.Load(bindingJsonModel.ServerUri).Returns(mockCredentials);
        solutionBindingFileLoader.Load(MockFilePath).Returns(bindingJsonModel);
        bindingJsonModelConverter.ConvertFromModelToLegacy(bindingJsonModel, mockCredentials).Returns(boundSonarQubeProject);

        ((ILegacySolutionBindingRepository)testSubject).Read(MockFilePath).Should().BeSameAs(boundSonarQubeProject);
        credentialsLoader.Received().Load(bindingJsonModel.ServerUri);
    }

    [TestMethod]
    public void DeleteBinding_DeletesBindingDirectoryOfBindingFile()
    {
        unintrusiveBindingPathProvider.GetBindingPath(LocalBindingKey).Returns(MockFilePath);

        testSubject.DeleteBinding(LocalBindingKey);

        solutionBindingFileLoader.Received(1).DeleteBindingDirectory(MockFilePath);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void DeleteBinding_ReturnsResultOfDeleteBindingDirectory(bool expectedResult)
    {
        MockDeletingBindingDirectory(LocalBindingKey, expectedResult);

        var result = testSubject.DeleteBinding(LocalBindingKey);

        result.Should().Be(expectedResult);
    }

    [TestMethod]
    public void DeleteBinding_DirectoryNotDeleted_EventNotTriggered()
    {
        var eventHandler = Substitute.For<EventHandler<LocalBindingKeyEventArgs>>();
        testSubject.BindingDeleted += eventHandler;
        MockDeletingBindingDirectory(LocalBindingKey, deleted: false);

        testSubject.DeleteBinding(LocalBindingKey);

        eventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void DeleteBinding_DirectoryDeleted_EventTriggered()
    {
        var eventHandler = Substitute.For<EventHandler<LocalBindingKeyEventArgs>>();
        testSubject.BindingDeleted += eventHandler;
        MockDeletingBindingDirectory(LocalBindingKey, deleted: true);

        testSubject.DeleteBinding(LocalBindingKey);

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<LocalBindingKeyEventArgs>(x => x.LocalBindingKey == LocalBindingKey));
    }

    private BoundServerProject SetUpBinding(string solution, ServerConnection connection, string bindingConfig)
    {
        var dto = new BindingJsonModel { ServerConnectionId = connection?.Id };
        solutionBindingFileLoader.Load(bindingConfig).Returns(dto);
        if (connection == null)
        {
            return null;
        }
        var bound = new BoundServerProject(solution, "any", connection);
        unintrusiveBindingPathProvider.GetBindingKeyFromPath(bindingConfig).Returns(solution);
        bindingJsonModelConverter.ConvertFromModel(dto, connection, solution).Returns(bound);
        return bound;
    }

    private void SetUpConnections(params ServerConnection[] connections) =>
        serverConnectionsRepository
            .TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>())
            .Returns(call =>
            {
                call[0] = connections;
                return true;
            });

    private void SetUpUnintrusiveBindingPathProvider(params string[] bindigFolders) => unintrusiveBindingPathProvider.GetBindingPaths().Returns(bindigFolders);

    private void MockDeletingBindingDirectory(string localBindingKey, bool deleted)
    {
        unintrusiveBindingPathProvider.GetBindingPath(localBindingKey).Returns(MockFilePath);
        solutionBindingFileLoader.DeleteBindingDirectory(MockFilePath).Returns(deleted);
    }
}

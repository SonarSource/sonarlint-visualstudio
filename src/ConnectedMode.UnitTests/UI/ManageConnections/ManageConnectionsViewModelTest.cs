/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using static SonarLint.VisualStudio.Core.Binding.ServerConnection;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ManageConnections;

[TestClass]
public class ManageConnectionsViewModelTest
{
    private const string LocalBindingKey1 = "solution name 1";
    private const string LocalBindingKey2 = "solution name 2";
    private IConnectedModeBindingServices connectedModeBindingServices;
    private IConnectedModeServices connectedModeServices;
    private ILogger logger;
    private IProgressReporterViewModel progressReporterViewModel;
    private IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter;
    private ISolutionBindingRepository solutionBindingRepository;
    private ISolutionInfoProvider solutionInfoProvider;
    private ManageConnectionsViewModel testSubject;
    private IThreadHandling threadHandling;
    private List<Connection> twoConnections;

    [TestInitialize]
    public void TestInitialize()
    {
        twoConnections =
        [
            new Connection(new ConnectionInfo(new Uri("http://localhost:9000").ToString(), ConnectionServerType.SonarQube)),
            new Connection(new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud), false)
        ];
        progressReporterViewModel = Substitute.For<IProgressReporterViewModel>();
        connectedModeServices = Substitute.For<IConnectedModeServices>();
        connectedModeBindingServices = Substitute.For<IConnectedModeBindingServices>();

        testSubject = new ManageConnectionsViewModel(connectedModeServices, connectedModeBindingServices, progressReporterViewModel);

        MockServices();
    }

    [TestMethod]
    public void ConnectionViewModels_NoInitialization_HasEmptyList()
    {
        testSubject.ConnectionViewModels.Should().NotBeNull();
        testSubject.ConnectionViewModels.Count.Should().Be(0);
    }

    [TestMethod]
    public void InitializeConnectionViewModels_InitializesConnectionsCorrectly()
    {
        MockTryGetConnections(twoConnections);

        testSubject.InitializeConnectionViewModels();

        HasExpectedConnections(twoConnections);
    }

    [TestMethod]
    public async Task RemoveConnectionWithProgressAsync_InitializesDataAndReportsProgress()
    {
        await testSubject.RemoveConnectionWithProgressAsync([], CreateConnectionViewModel(twoConnections[0]));

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.ProgressStatus == UiResources.RemovingConnectionText &&
                    x.WarningText == UiResources.RemovingConnectionFailedText &&
                    x.SuccessText == string.Format(UiResources.RemovingConnectionSucceededText, twoConnections[0].Info.Id)));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void RemoveConnectionViewModel_ReturnsStatusFromSlCore(bool expectedStatus)
    {
        InitializeTwoConnections();
        var connectionToRemove = testSubject.ConnectionViewModels[0];
        serverConnectionsRepositoryAdapter.TryRemoveConnection(connectionToRemove.Connection.Info).Returns(expectedStatus);

        var succeeded = testSubject.RemoveConnectionViewModel([], connectionToRemove);

        succeeded.Should().Be(expectedStatus);
        serverConnectionsRepositoryAdapter.Received(1).TryRemoveConnection(connectionToRemove.Connection.Info);
    }

    [TestMethod]
    public void RemoveConnection_ConnectionWasRemoved_RemovesProvidedConnectionViewModel()
    {
        InitializeTwoConnections();
        var connectionToRemove = testSubject.ConnectionViewModels[0];
        serverConnectionsRepositoryAdapter.TryRemoveConnection(connectionToRemove.Connection.Info).Returns(true);

        testSubject.RemoveConnectionViewModel([], connectionToRemove);

        testSubject.ConnectionViewModels.Count.Should().Be(twoConnections.Count - 1);
        testSubject.ConnectionViewModels.Should().NotContain(connectionToRemove);
    }

    [TestMethod]
    public void RemoveConnectionViewModel_ConnectionWasNotRemoved_DoesNotRemoveProvidedConnectionViewModel()
    {
        InitializeTwoConnections();
        var connectionToRemove = testSubject.ConnectionViewModels[0];
        serverConnectionsRepositoryAdapter.TryRemoveConnection(connectionToRemove.Connection.Info).Returns(false);

        testSubject.RemoveConnectionViewModel([], connectionToRemove);

        testSubject.ConnectionViewModels.Count.Should().Be(twoConnections.Count);
        testSubject.ConnectionViewModels.Should().Contain(connectionToRemove);
    }

    [TestMethod]
    public void RemoveConnection_ConnectionWasRemoved_RaisesEvents()
    {
        InitializeTwoConnections();
        serverConnectionsRepositoryAdapter.TryRemoveConnection(Arg.Any<ConnectionInfo>()).Returns(true);
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.RemoveConnectionViewModel([], testSubject.ConnectionViewModels[0]);

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.NoConnectionExists)));
    }

    [TestMethod]
    public void RemoveConnectionViewModel_ConnectionWasNotRemoved_DoesNotRaiseEvents()
    {
        InitializeTwoConnections();
        serverConnectionsRepositoryAdapter.TryRemoveConnection(Arg.Any<ConnectionInfo>()).Returns(false);
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.RemoveConnectionViewModel([], testSubject.ConnectionViewModels[0]);

        eventHandler.DidNotReceive().Invoke(testSubject, Arg.Any<PropertyChangedEventArgs>());
    }

    [TestMethod]
    public void RemoveConnectionViewModel_TwoBindingsExistForConnection_RemovesBindingsAndThenConnection()
    {
        InitializeTwoConnections();
        MockDeleteBinding(LocalBindingKey1, true);
        MockDeleteBinding(LocalBindingKey2, true);

        testSubject.RemoveConnectionViewModel([LocalBindingKey1, LocalBindingKey2], testSubject.ConnectionViewModels[0]);

        Received.InOrder(() =>
        {
            solutionBindingRepository.DeleteBinding(LocalBindingKey1);
            solutionBindingRepository.DeleteBinding(LocalBindingKey2);
            serverConnectionsRepositoryAdapter.TryRemoveConnection(testSubject.ConnectionViewModels[0].Connection.Info);
        });
    }

    [TestMethod]
    public void RemoveConnectionViewModel_TwoBindingsExistForConnection_DeletingOneBindingFails_DoesNotRemoveConnection()
    {
        InitializeTwoConnections();
        MockDeleteBinding(LocalBindingKey1, true);
        MockDeleteBinding(LocalBindingKey2, false);

        testSubject.RemoveConnectionViewModel([LocalBindingKey1, LocalBindingKey2], testSubject.ConnectionViewModels[0]);

        Received.InOrder(() =>
        {
            solutionBindingRepository.DeleteBinding(LocalBindingKey1);
            solutionBindingRepository.DeleteBinding(LocalBindingKey2);
            logger.WriteLine(UiResources.DeleteConnection_DeleteBindingFails, LocalBindingKey2);
        });
        serverConnectionsRepositoryAdapter.DidNotReceive().TryRemoveConnection(testSubject.ConnectionViewModels[0].Connection.Info);
    }

    [TestMethod]
    public void RemoveConnectionViewModel_TwoBindingsExistForConnection_OneBindingIsForCurrentSolution_CallsUnbind()
    {
        InitializeTwoConnections();
        InitializeCurrentSolution(LocalBindingKey2);
        MockDeleteBinding(LocalBindingKey1, true);
        MockUnbind(LocalBindingKey2, true);

        testSubject.RemoveConnectionViewModel([LocalBindingKey1, LocalBindingKey2], testSubject.ConnectionViewModels[0]);

        Received.InOrder(() =>
        {
            solutionBindingRepository.DeleteBinding(LocalBindingKey1);
            connectedModeBindingServices.BindingControllerAdapter.Unbind(LocalBindingKey2);
            serverConnectionsRepositoryAdapter.TryRemoveConnection(testSubject.ConnectionViewModels[0].Connection.Info);
        });
        solutionBindingRepository.DidNotReceive().DeleteBinding(LocalBindingKey2);
    }

    [TestMethod]
    public void RemoveConnectionViewModel_TwoBindingsExistForConnection_OneBindingIsForCurrentSolution_UnbindFails_DoesNotRemoveConnection()
    {
        InitializeTwoConnections();
        InitializeCurrentSolution(LocalBindingKey2);
        MockDeleteBinding(LocalBindingKey1, true);
        MockUnbind(LocalBindingKey2, false);

        testSubject.RemoveConnectionViewModel([LocalBindingKey1, LocalBindingKey2], testSubject.ConnectionViewModels[0]);

        solutionBindingRepository.Received(1).DeleteBinding(LocalBindingKey1);
        connectedModeBindingServices.BindingControllerAdapter.Received(1).Unbind(LocalBindingKey2);
        serverConnectionsRepositoryAdapter.DidNotReceive().TryRemoveConnection(testSubject.ConnectionViewModels[0].Connection.Info);
    }

    [TestMethod]
    public async Task SafeExecuteActionAsync_LoadsConnectionsOnUIThread()
    {
        await testSubject.SafeExecuteActionAsync(() => true);

        await threadHandling.Received(1).RunOnUIThreadAsync(Arg.Any<Action>());
    }

    [TestMethod]
    public async Task SafeExecuteActionAsync_LoadingConnectionsThrows_ReturnsFalse()
    {
        var exceptionMsg = "Failed to load connections";
        var mockedThreadHandling = Substitute.For<IThreadHandling>();
        connectedModeServices.ThreadHandling.Returns(mockedThreadHandling);
        mockedThreadHandling.When(x => x.RunOnUIThreadAsync(Arg.Any<Action>())).Do(callInfo => throw new Exception(exceptionMsg));

        var adapterResponse = await testSubject.SafeExecuteActionAsync(() => true);

        adapterResponse.Success.Should().BeFalse();
        logger.Received(1).WriteLine(exceptionMsg);
    }

    [TestMethod]
    public void AddConnectionViewModel_AddsProvidedConnection()
    {
        var connectionToAdd = new Connection(new ConnectionInfo("mySecondOrg", ConnectionServerType.SonarCloud), false);

        testSubject.AddConnectionViewModel(connectionToAdd);

        testSubject.ConnectionViewModels.Count.Should().Be(1);
        testSubject.ConnectionViewModels[0].Connection.Should().Be(connectionToAdd);
    }

    [TestMethod]
    public void AddConnectionViewModel_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.AddConnectionViewModel(new Connection(new ConnectionInfo("mySecondOrg", ConnectionServerType.SonarCloud), false));

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.NoConnectionExists)));
    }

    [TestMethod]
    public void NoConnectionExists_NoConnections_ReturnsTrue()
    {
        MockTryGetConnections([]);

        testSubject.InitializeConnectionViewModels();

        testSubject.NoConnectionExists.Should().BeTrue();
    }

    [TestMethod]
    public void NoConnectionExists_HasConnections_ReturnsFalse()
    {
        InitializeTwoConnections();

        testSubject.NoConnectionExists.Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadConnectionsWithProgressAsync_InitializesDataAndReportsProgress()
    {
        await testSubject.LoadConnectionsWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.ProgressStatus == UiResources.LoadingConnectionsText &&
                    x.WarningText == UiResources.LoadingConnectionsFailedText));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void InitializeConnectionViewModels_ReturnsResponseFromAdapter(bool expectedStatus)
    {
        serverConnectionsRepositoryAdapter.TryGetAllConnections(out _).Returns(expectedStatus);

        var adapterResponse = testSubject.InitializeConnectionViewModels();

        adapterResponse.Should().Be(expectedStatus);
    }

    [TestMethod]
    public async Task CreateConnectionsWithProgressAsync_InitializesDataAndReportsProgress()
    {
        var connectionToAdd = CreateSonarCloudConnection();

        await testSubject.CreateConnectionsWithProgressAsync(connectionToAdd, Substitute.For<ICredentialsModel>());

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.ProgressStatus == UiResources.CreatingConnectionProgressText &&
                    x.WarningText == UiResources.CreatingConnectionFailedText &&
                    x.SuccessText == string.Format(UiResources.CreatingConnectionSucceededText, connectionToAdd.Info.Id)));
    }

    [TestMethod]
    public void CreateNewConnection_ConnectionWasAddedToRepository_AddsProvidedConnection()
    {
        var connectionToAdd = new Connection(new ConnectionInfo("mySecondOrg", ConnectionServerType.SonarCloud), false);
        serverConnectionsRepositoryAdapter.TryAddConnection(connectionToAdd, Arg.Any<ICredentialsModel>()).Returns(true);

        var succeeded = testSubject.CreateNewConnection(connectionToAdd, Substitute.For<ICredentialsModel>());

        succeeded.Should().BeTrue();
        testSubject.ConnectionViewModels.Count.Should().Be(1);
        testSubject.ConnectionViewModels[0].Connection.Should().Be(connectionToAdd);
        serverConnectionsRepositoryAdapter.Received(1).TryAddConnection(connectionToAdd, Arg.Any<ICredentialsModel>());
    }

    [TestMethod]
    public void CreateNewConnection_ConnectionWasNotAddedToRepository_DoesNotAddConnection()
    {
        var connectionToAdd = new Connection(new ConnectionInfo("mySecondOrg", ConnectionServerType.SonarCloud), false);
        serverConnectionsRepositoryAdapter.TryAddConnection(connectionToAdd, Arg.Any<ICredentialsModel>()).Returns(false);

        var succeeded = testSubject.CreateNewConnection(connectionToAdd, Substitute.For<ICredentialsModel>());

        succeeded.Should().BeFalse();
        testSubject.ConnectionViewModels.Should().BeEmpty();
        serverConnectionsRepositoryAdapter.Received(1).TryAddConnection(connectionToAdd, Arg.Any<ICredentialsModel>());
    }

    [TestMethod]
    public async Task GetConnectionReferencesWithProgressAsync_CalculatesReferencesAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatusWithData<List<string>>>>()).Returns(new ResponseStatusWithData<List<string>>(true, []));

        await testSubject.GetConnectionReferencesWithProgressAsync(CreateConnectionViewModel(twoConnections[0]));

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatusWithData<List<string>>>>(x =>
                    x.ProgressStatus == UiResources.CalculatingConnectionReferencesText &&
                    x.WarningText == UiResources.CalculatingConnectionReferencesFailedText));
    }

    [TestMethod]
    public async Task GetConnectionReferencesOnBackgroundThreadAsync_RunsOnBackgroundThread()
    {
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<ResponseStatusWithData<List<string>>>>>()).Returns(new ResponseStatusWithData<List<string>>(true, []));

        await testSubject.GetConnectionReferencesOnBackgroundThreadAsync(CreateConnectionViewModel(twoConnections[0]));

        await threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<ResponseStatusWithData<List<string>>>>>());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task GetConnectionReferencesOnBackgroundThreadAsync_ReturnsCalculatedReferences(bool expectedResponse)
    {
        var bindingKey = "localBindingKey";
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<ResponseStatusWithData<List<string>>>>>()).Returns(new ResponseStatusWithData<List<string>>(expectedResponse, [bindingKey]));

        var responses = await testSubject.GetConnectionReferencesOnBackgroundThreadAsync(CreateConnectionViewModel(twoConnections[0]));

        responses.Success.Should().Be(expectedResponse);
        responses.ResponseData.Should().Contain(bindingKey);
    }

    [TestMethod]
    public void GetConnectionReferences_NoBindingReferencesConnection_ReturnsEmptyList()
    {
        var response = testSubject.GetConnectionReferences(CreateConnectionViewModel(twoConnections[0]));

        response.Success.Should().BeTrue();
        response.ResponseData.Should().BeEmpty();
    }

    [TestMethod]
    public void GetConnectionReferences_OneBindingReferencesSonarCloudConnection_ReturnsOneBinding()
    {
        var bindingKey = "localBindingKey";
        var sonarCloud = twoConnections.First(conn => conn.Info.ServerType == ConnectionServerType.SonarCloud);
        solutionBindingRepository.List().Returns([new BoundServerProject(bindingKey, "myProject", CreateSonarCloudServerConnection(sonarCloud))]);

        var response = testSubject.GetConnectionReferences(CreateConnectionViewModel(sonarCloud));

        response.Success.Should().BeTrue();
        response.ResponseData.Should().Contain(bindingKey);
    }

    [TestMethod]
    public void GetConnectionReferences_OneBindingReferencesSonarQubeConnection_ReturnsOneBinding()
    {
        var bindingKey = "localBindingKey";
        var sonarQube = twoConnections.First(conn => conn.Info.ServerType == ConnectionServerType.SonarQube);
        solutionBindingRepository.List().Returns([new BoundServerProject(bindingKey, "myProject", CreateSonarQubeServerConnection(sonarQube))]);

        var response = testSubject.GetConnectionReferences(CreateConnectionViewModel(sonarQube));

        response.Success.Should().BeTrue();
        response.ResponseData.Should().Contain(bindingKey);
    }

    [TestMethod]
    public void GetConnectionReferences_TwoBindingsReferencesSonarQubeConnection_ReturnsTwoBindings()
    {
        var sonarQube = twoConnections.First(conn => conn.Info.ServerType == ConnectionServerType.SonarQube);
        var serverConnectionToBeRemoved = CreateSonarQubeServerConnection(sonarQube);
        solutionBindingRepository.List().Returns([
            new BoundServerProject("binding1", "myProject", serverConnectionToBeRemoved),
            new BoundServerProject("binding2", "myProject2", serverConnectionToBeRemoved)
        ]);

        var response = testSubject.GetConnectionReferences(CreateConnectionViewModel(sonarQube));

        response.Success.Should().BeTrue();
        response.ResponseData.Should().BeEquivalentTo("binding1", "binding2");
    }

    [TestMethod]
    public void GetConnectionReferences_TwoBindingsReferencesSonarCloudConnection_ReturnsTwoBindings()
    {
        var sonarCloud = twoConnections.First(conn => conn.Info.ServerType == ConnectionServerType.SonarQube);
        var serverConnectionToBeRemoved = CreateSonarCloudServerConnection(sonarCloud);
        solutionBindingRepository.List().Returns([
            new BoundServerProject("binding1", "myProject", serverConnectionToBeRemoved),
            new BoundServerProject("binding2", "myProject2", serverConnectionToBeRemoved)
        ]);

        var response = testSubject.GetConnectionReferences(CreateConnectionViewModel(sonarCloud));

        response.Success.Should().BeTrue();
        response.ResponseData.Should().BeEquivalentTo("binding1", "binding2");
    }

    [TestMethod]
    public void GetConnectionReferences_BindingRepositoryThrowsException_ReturnsEmptyList()
    {
        var exceptionMsg = "Failed to retrieve bindings";
        var exception = new Exception(exceptionMsg);
        solutionBindingRepository.When(repo => repo.List()).Do(_ => throw exception);

        var response = testSubject.GetConnectionReferences(CreateConnectionViewModel(twoConnections[0]));

        response.Success.Should().BeFalse();
        response.ResponseData.Should().BeEmpty();
        logger.Received(1).WriteLine(exception.ToString());
    }

    private void HasExpectedConnections(IEnumerable<Connection> expectedConnections)
    {
        testSubject.ConnectionViewModels.Should().NotBeNull();
        testSubject.ConnectionViewModels.Count.Should().Be(twoConnections.Count);
        foreach (var connection in expectedConnections)
        {
            var connectionViewModel = testSubject.ConnectionViewModels.SingleOrDefault(c => c.Name == connection.Info.Id);
            connectionViewModel.Should().NotBeNull();
            connectionViewModel.ServerType.Should().Be(connection.Info.ServerType.ToString());
            connectionViewModel.EnableSmartNotifications.Should().Be(connection.EnableSmartNotifications);
        }
    }

    private void InitializeTwoConnections()
    {
        serverConnectionsRepositoryAdapter.TryGetAllConnections(out _).Returns(callInfo =>
        {
            callInfo[0] = twoConnections;
            return true;
        });
        testSubject.InitializeConnectionViewModels();
    }

    private void MockServices()
    {
        serverConnectionsRepositoryAdapter = Substitute.For<IServerConnectionsRepositoryAdapter>();
        threadHandling = Substitute.For<IThreadHandling>();
        logger = Substitute.For<ILogger>();

        connectedModeServices.ServerConnectionsRepositoryAdapter.Returns(serverConnectionsRepositoryAdapter);
        connectedModeServices.ThreadHandling.Returns(threadHandling);
        connectedModeServices.Logger.Returns(logger);
        MockTryGetConnections(twoConnections);

        solutionBindingRepository = Substitute.For<ISolutionBindingRepository>();
        connectedModeBindingServices.SolutionBindingRepository.Returns(solutionBindingRepository);

        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        connectedModeBindingServices.SolutionInfoProvider.Returns(solutionInfoProvider);
    }

    private void MockTryGetConnections(List<Connection> connections) =>
        serverConnectionsRepositoryAdapter.TryGetAllConnections(out _).Returns(callInfo =>
        {
            callInfo[0] = connections;
            return true;
        });

    private static Connection CreateSonarCloudConnection() => new(new ConnectionInfo("mySecondOrg", ConnectionServerType.SonarCloud), false);

    private static SonarCloud CreateSonarCloudServerConnection(Connection sonarCloud) => new(sonarCloud.Info.Id);

    private static ServerConnection.SonarQube CreateSonarQubeServerConnection(Connection sonarQube) => new(new Uri(sonarQube.Info.Id));

    private void MockDeleteBinding(string localBindingKey, bool success) => connectedModeBindingServices.SolutionBindingRepository.DeleteBinding(localBindingKey).Returns(success);

    private void MockUnbind(string localBindingKey, bool success) => connectedModeBindingServices.BindingControllerAdapter.Unbind(localBindingKey).Returns(success);

    private void InitializeCurrentSolution(string solutionName) => connectedModeBindingServices.SolutionInfoProvider.GetSolutionName().Returns(solutionName);

    private ConnectionViewModel CreateConnectionViewModel(Connection connection) => new(connection, connectedModeServices.ServerConnectionsRepositoryAdapter);
}

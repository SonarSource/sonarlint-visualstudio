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

using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.Credentials;

[TestClass]
public class EditCredentialsViewModelTests
{
    private static readonly ConnectionInfo SonarQubeConnectionInfo = new("http://localhost:9000/", ConnectionServerType.SonarQube);
    private static readonly ConnectionInfo SonarCloudEuConnectionInfo = new("myOrg", ConnectionServerType.SonarCloud, CloudServerRegion.Eu);
    private readonly Connection sonarQubeConnection = new(SonarQubeConnectionInfo, false);
    private readonly Connection sonarCloudConnection = new(SonarCloudEuConnectionInfo, false);
    private IConnectedModeUIManager uiManager;
    private IConnectedModeBindingServices connectedModeBindingServices;
    private IConnectedModeServices connectedModeServices;
    private IProgressReporterViewModel progressReporterViewModel;
    private IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter;
    private ISolutionBindingRepository solutionBindingRepository;
    private ISolutionInfoProvider solutionInfoProvider;
    private EditCredentialsViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        MockServices();
        progressReporterViewModel = Substitute.For<IProgressReporterViewModel>();

        testSubject = CreateTestSubject(sonarQubeConnection);
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_UpdatesConnectionAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatus>>()).Returns(Task.FromResult(new ResponseStatus(true)));

        await testSubject.UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.TaskToPerform == testSubject.UpdateConnectionCredentialsAsync &&
                    x.ProgressStatus == UiResources.UpdatingConnectionCredentialsProgressText &&
                    x.WarningText == UiResources.UpdatingConnectionCredentialsFailedText));
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_WhenCurrentSolutionIsBoundToUpdatedSonarQubeConnection_RebindsAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatus>>()).Returns(Task.FromResult(new ResponseStatus(true)));
        MockConfigurationProvider(CreateSonarQubeServerConnection());

        await CreateTestSubject(sonarQubeConnection).UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.ProgressStatus == UiResources.RebindingProgressText &&
                    x.WarningText == UiResources.RebindingFailedText));
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_WhenCurrentSolutionIsBoundToUpdatedSonarCloudConnection_RebindsAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatus>>()).Returns(Task.FromResult(new ResponseStatus(true)));
        MockConfigurationProvider(CreateSonarCloudServerConnection(sonarCloudConnection.Info.Id, sonarCloudConnection.Info.CloudServerRegion));

        await CreateTestSubject(sonarCloudConnection).UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.ProgressStatus == UiResources.RebindingProgressText &&
                    x.WarningText == UiResources.RebindingFailedText));
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_WhenCurrentSolutionIsBoundToDifferentSonarCloudConnection_RebindsAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatus>>()).Returns(Task.FromResult(new ResponseStatus(true)));
        MockConfigurationProvider(CreateSonarCloudServerConnection(sonarCloudConnection.Info.Id, CloudServerRegion.Us));

        await CreateTestSubject(sonarCloudConnection).UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.DidNotReceive()
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.ProgressStatus == UiResources.RebindingProgressText &&
                    x.WarningText == UiResources.RebindingFailedText));
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_WhenCurrentSolutionIsStandalone_DoesNotRebindAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatus>>()).Returns(Task.FromResult(new ResponseStatus(true)));
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(BindingConfiguration.Standalone);
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);

        await testSubject.UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.DidNotReceive()
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.ProgressStatus == UiResources.RebindingProgressText &&
                    x.WarningText == UiResources.RebindingFailedText));
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_WhenConnectionFailedToUpdate_DoesNotRebindAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatus>>()).Returns(Task.FromResult(new ResponseStatus(false)));

        await testSubject.UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.DidNotReceive()
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.ProgressStatus == UiResources.RebindingProgressText &&
                    x.WarningText == UiResources.RebindingFailedText));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task UpdateConnectionCredentials_TryUpdatesProvidedConnection(bool success)
    {
        serverConnectionsRepositoryAdapter.TryUpdateCredentials(sonarQubeConnection, Arg.Any<ICredentialsModel>()).Returns(success);

        var succeeded = await testSubject.UpdateConnectionCredentialsAsync();

        succeeded.Success.Should().Be(success);
        serverConnectionsRepositoryAdapter.Received(1).TryUpdateCredentials(sonarQubeConnection, Arg.Is<TokenCredentialsModel>(x => x.Token == testSubject.Token));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task RebindAsync_CallsBindingControllerAdapter(bool success)
    {
        const string serverProjectKey = "serverProjectKey";
        const string serverConnectionId = "serverConnectionId";
        connectedModeBindingServices.BindingControllerAdapter.ValidateAndBindAsync(
            Arg.Is<BindingRequest.Manual>(x => x.ConnectionId == serverConnectionId && x.ProjectKey == serverProjectKey),
            uiManager,
            CancellationToken.None).Returns(success ? BindingResult.Success : BindingResult.Failed);

        var response = await testSubject.RebindAsync(serverProjectKey, serverConnectionId);

        response.Success.Should().Be(success);
    }

    private void MockServices()
    {
        connectedModeServices = Substitute.For<IConnectedModeServices>();
        connectedModeBindingServices = Substitute.For<IConnectedModeBindingServices>();
        serverConnectionsRepositoryAdapter = Substitute.For<IServerConnectionsRepositoryAdapter>();
        solutionBindingRepository = Substitute.For<ISolutionBindingRepository>();
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        uiManager = Substitute.For<IConnectedModeUIManager>();

        connectedModeServices.ServerConnectionsRepositoryAdapter.Returns(serverConnectionsRepositoryAdapter);
        connectedModeBindingServices.SolutionBindingRepository.Returns(solutionBindingRepository);
        connectedModeBindingServices.SolutionInfoProvider.Returns(solutionInfoProvider);
    }

    private void MockConfigurationProvider(ServerConnection serverConnectionToUpdate)
    {
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(new BindingConfiguration(new BoundServerProject("local", "server", serverConnectionToUpdate), SonarLintMode.Connected, "binding-dir"));
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);
    }

    private ServerConnection.SonarQube CreateSonarQubeServerConnection() => new(new Uri(sonarQubeConnection.Info.Id));

    private ServerConnection.SonarCloud CreateSonarCloudServerConnection(string organization, CloudServerRegion region) => new(organization, region);

    private EditCredentialsViewModel CreateTestSubject(Connection connection) => new(connection, uiManager, connectedModeServices, connectedModeBindingServices, progressReporterViewModel);
}

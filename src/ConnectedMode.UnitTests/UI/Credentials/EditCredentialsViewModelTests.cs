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

using SonarLint.VisualStudio.ConnectedMode.Binding;
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
    private static readonly ConnectionInfo SonarQubeConnectionInfo = new("http://localhost:9000/", ConnectionServerType.SonarCloud, CloudServerRegion.Us);
    private readonly Connection sonarQubeConnection = new(SonarQubeConnectionInfo, false);
    private IBindingController bindingController;
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

        testSubject = new EditCredentialsViewModel(sonarQubeConnection, connectedModeServices, connectedModeBindingServices, progressReporterViewModel);
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_UpdatesConnectionAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<AdapterResponse>>()).Returns(Task.FromResult(new AdapterResponse(true)));

        await testSubject.UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
                    x.TaskToPerform == testSubject.UpdateConnectionCredentialsAsync &&
                    x.ProgressStatus == UiResources.UpdatingConnectionCredentialsProgressText &&
                    x.WarningText == UiResources.UpdatingConnectionCredentialsFailedText));
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_WhenCurrentSolutionIsBoundToUpdatedConnection_RebindsAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<AdapterResponse>>()).Returns(Task.FromResult(new AdapterResponse(true)));
        MockConfigurationProvider();

        await testSubject.UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
                    x.ProgressStatus == UiResources.RebindingProgressText &&
                    x.WarningText == UiResources.RebindingFailedText));
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_WhenCurrentSolutionIsStandalone_DoesNotRebindAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<AdapterResponse>>()).Returns(Task.FromResult(new AdapterResponse(true)));
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(BindingConfiguration.Standalone);
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);

        await testSubject.UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.DidNotReceive()
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
                    x.ProgressStatus == UiResources.RebindingProgressText &&
                    x.WarningText == UiResources.RebindingFailedText));
    }

    [TestMethod]
    public async Task UpdateConnectionCredentialsWithProgressAsync_WhenConnectionFailedToUpdate_DoesNotRebindAndReportsProgress()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<AdapterResponse>>()).Returns(Task.FromResult(new AdapterResponse(false)));

        await testSubject.UpdateConnectionCredentialsWithProgressAsync();

        await progressReporterViewModel.DidNotReceive()
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
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

    [TestMethod]
    public async Task RebindAsync_WhenServerConnectionCannotBeFound_Fails()
    {
        connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(Arg.Any<ConnectionInfo>(), out Arg.Any<ServerConnection>()).Returns(false);

        var response = await testSubject.RebindAsync("serverProjectKey");

        await bindingController.DidNotReceiveWithAnyArgs().BindAsync(Arg.Any<BoundServerProject>(), Arg.Any<CancellationToken>());
        response.Success.Should().BeFalse();
    }

    [TestMethod]
    public async Task RebindAsync_WhenExceptionThrownDuringBinding_Fails()
    {
        MockTryGetServerConnection(CreateSonarCloudServerConnection());
        MockSolutionInfoProvider("mySolution");
        bindingController.BindAsync(Arg.Any<BoundServerProject>(), Arg.Any<CancellationToken>()).Returns(_ => throw new Exception("Failed to bind"));

        var response = await testSubject.RebindAsync("serverProjectKey");

        await bindingController.ReceivedWithAnyArgs().BindAsync(Arg.Any<BoundServerProject>(), Arg.Any<CancellationToken>());
        response.Success.Should().BeFalse();
    }

    [TestMethod]
    public async Task RebindAsync_WhenBindingSucceeds_Succeed()
    {
        var serverConnectionToUpdate = CreateSonarCloudServerConnection();
        MockTryGetServerConnection(serverConnectionToUpdate);
        MockSolutionInfoProvider("mySolution");

        var response = await testSubject.RebindAsync("serverProjectKey");

        await bindingController.Received().BindAsync(Arg.Is<BoundServerProject>(
                x => x.LocalBindingKey == "mySolution" && x.ServerConnection.Id == serverConnectionToUpdate.Id),
            CancellationToken.None);
        response.Success.Should().BeTrue();
    }

    private void MockSolutionInfoProvider(string solutionName) => solutionInfoProvider.GetSolutionNameAsync().Returns(Task.FromResult(solutionName));

    private void MockTryGetServerConnection(ServerConnection expectedServerConnection = null) =>
        connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(Arg.Any<ConnectionInfo>(), out _).Returns(callInfo =>
        {
            callInfo[1] = expectedServerConnection;
            return true;
        });

    private void MockServices()
    {
        connectedModeServices = Substitute.For<IConnectedModeServices>();
        connectedModeBindingServices = Substitute.For<IConnectedModeBindingServices>();
        serverConnectionsRepositoryAdapter = Substitute.For<IServerConnectionsRepositoryAdapter>();
        solutionBindingRepository = Substitute.For<ISolutionBindingRepository>();
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        bindingController = Substitute.For<IBindingController>();

        connectedModeServices.ServerConnectionsRepositoryAdapter.Returns(serverConnectionsRepositoryAdapter);
        connectedModeBindingServices.SolutionBindingRepository.Returns(solutionBindingRepository);
        connectedModeBindingServices.SolutionInfoProvider.Returns(solutionInfoProvider);
        connectedModeBindingServices.BindingController.Returns(bindingController);
    }

    private void MockConfigurationProvider()
    {
        var serverConnectionToUpdate = CreateSonarCloudServerConnection();
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(new BindingConfiguration(new BoundServerProject("local", "server", serverConnectionToUpdate), SonarLintMode.Connected, "binding-dir"));
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);
    }

    private ServerConnection.SonarQube CreateSonarCloudServerConnection() => new(new Uri(sonarQubeConnection.Info.Id));
}

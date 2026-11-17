/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using SonarLint.VisualStudio.ConnectedMode.Automation;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Automation;

/// <summary>Uses the real dialog view models over substituted services.</summary>
[TestClass]
public class ConnectedModeAutomationTests
{
    private const string ServerUrl = "http://localhost:9000";
    private const string Token = "a-token";
    private const string ProjectKey = "a-project";
    private static readonly ConnectionInfo ExpectedConnectionInfo = new(ServerUrl, ConnectionServerType.SonarQube);
    private static readonly ServerConnection.SonarQube SonarQubeConnection = new(new Uri(ServerUrl));
    private static readonly ServerProject Project = new(ProjectKey, "A project");

    private ISlCoreConnectionAdapter slCoreConnectionAdapter;
    private IServerConnectionsRepositoryAdapter repository;
    private IBindingControllerAdapter bindingControllerAdapter;
    private ISolutionInfoProvider solutionInfoProvider;
    private ITelemetryManager telemetryManager;
    private IConnectedModeUIManager uiManager;
    private ConnectedModeAutomation testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
        repository = Substitute.For<IServerConnectionsRepositoryAdapter>();
        bindingControllerAdapter = Substitute.For<IBindingControllerAdapter>();
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        uiManager = Substitute.For<IConnectedModeUIManager>();
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(BindingConfiguration.Standalone);
        var connectedModeServices = Substitute.For<IConnectedModeServices>();
        connectedModeServices.SlCoreConnectionAdapter.Returns(slCoreConnectionAdapter);
        connectedModeServices.ServerConnectionsRepositoryAdapter.Returns(repository);
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);
        connectedModeServices.TelemetryManager.Returns(telemetryManager);
        connectedModeServices.ThreadHandling.Returns(new NoOpThreadHandler());
        connectedModeServices.Logger.Returns(new TestLogger());
        var bindingServices = Substitute.For<IConnectedModeBindingServices>();
        bindingServices.BindingControllerAdapter.Returns(bindingControllerAdapter);
        bindingServices.SolutionInfoProvider.Returns(solutionInfoProvider);
        solutionInfoProvider.GetSolutionNameAsync().Returns("Solution");
        testSubject = new ConnectedModeAutomation(connectedModeServices, bindingServices, uiManager);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ConnectedModeAutomation, IConnectedModeAutomation>(
            MefTestHelpers.CreateExport<IConnectedModeServices>(),
            MefTestHelpers.CreateExport<IConnectedModeBindingServices>(),
            MefTestHelpers.CreateExport<IConnectedModeUIManager>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ConnectedModeAutomation>();

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_ValidationFails_ReturnsItsReasonWithoutSaving()
    {
        MockValidation(new ResponseStatus(false, "invalid token"));

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be("invalid token");
        repository.DidNotReceiveWithAnyArgs().TryAddConnection(default, default);
    }

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_Valid_ValidatesThenSavesWithTheSameToken()
    {
        MockValidation(new ResponseStatus(true));
        repository.TryAddConnection(Arg.Any<Connection>(), Arg.Any<ICredentialsModel>()).Returns(true);

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Success.Should().BeTrue();
        Received.InOrder(() =>
        {
            slCoreConnectionAdapter.ValidateConnectionAsync(ExpectedConnectionInfo, Arg.Is<ICredentialsModel>(x => HasToken(x)));
            repository.TryAddConnection(Arg.Is<Connection>(x => x.Info == ExpectedConnectionInfo), Arg.Is<ICredentialsModel>(x => HasToken(x)));
        });
    }

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_UsesTheServerSelectionDialogsUrlHandling()
    {
        MockValidation(new ResponseStatus(false, "stop here"));

        await testSubject.AddSonarQubeConnectionAsync("http://local host:9000", Token);

        await slCoreConnectionAdapter.Received(1).ValidateConnectionAsync(ExpectedConnectionInfo, Arg.Any<ICredentialsModel>());
    }

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_ConnectionExists_ReturnsTheDialogsWarning()
    {
        MockValidation(new ResponseStatus(true));
        MockExistingConnection();

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(UiResources.ConnectionAlreadyExistsText);
        repository.DidNotReceiveWithAnyArgs().TryAddConnection(default, default);
    }

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_SaveFails_ReturnsTheDialogsWarning()
    {
        MockValidation(new ResponseStatus(true));
        repository.TryAddConnection(Arg.Any<Connection>(), Arg.Any<ICredentialsModel>()).Returns(false);

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(UiResources.CreatingConnectionFailedText);
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_SelectsTheListedConnectionAndProject_ThenBinds()
    {
        MockBindableSetup(listedProjects: [new ServerProject("other", "Other"), Project]);
        MockBinding(BindingResult.Success);

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeTrue();
        await bindingControllerAdapter.Received(1).ValidateAndBindAsync(
            new BindingRequest.Manual(ProjectKey, ExpectedConnectionInfo.GetServerIdFromConnectionInfo()),
            uiManager,
            Arg.Any<CancellationToken>());
        await slCoreConnectionAdapter.DidNotReceiveWithAnyArgs().GetServerProjectByKeyAsync(default, default);
        telemetryManager.Received(1).AddedManualBindings();
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_ProjectNotListed_ChoosesItByKey()
    {
        MockBindableSetup(listedProjects: []);
        slCoreConnectionAdapter.GetServerProjectByKeyAsync(SonarQubeConnection, ProjectKey).Returns(new ResponseStatusWithData<ServerProject>(true, Project));
        MockBinding(BindingResult.Success);

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeTrue();
        await bindingControllerAdapter.Received(1).ValidateAndBindAsync(
            new BindingRequest.Manual(ProjectKey, ExpectedConnectionInfo.GetServerIdFromConnectionInfo()),
            uiManager,
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_ProjectNotFound_FailsWithoutBinding()
    {
        MockBindableSetup(listedProjects: []);
        slCoreConnectionAdapter.GetServerProjectByKeyAsync(SonarQubeConnection, ProjectKey).Returns(new ResponseStatusWithData<ServerProject>(false, null));

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(UiResources.FetchingProjectInfoFailedText);
        await bindingControllerAdapter.DidNotReceiveWithAnyArgs().ValidateAndBindAsync(default, default, default);
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_NoConnectionToTheServer_FailsWithoutBinding()
    {
        MockConnections();

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(ConnectedModeAutomation.ConnectionNotListedText);
        await bindingControllerAdapter.DidNotReceiveWithAnyArgs().ValidateAndBindAsync(default, default, default);
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_NoSolutionOpen_FailsLikeTheDisabledButton()
    {
        MockBindableSetup(listedProjects: [Project]);
        solutionInfoProvider.GetSolutionNameAsync().Returns((string)null);

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(ConnectedModeAutomation.SelectProjectButtonDisabledText);
        await bindingControllerAdapter.DidNotReceiveWithAnyArgs().ValidateAndBindAsync(default, default, default);
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_BindingFails_ReturnsItsProblem()
    {
        MockBindableSetup(listedProjects: [Project]);
        MockBinding(BindingResult.ConnectionNotFound);

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(BindingResult.ConnectionNotFound.ProblemDescription);
    }

    private void MockValidation(ResponseStatus response) =>
        slCoreConnectionAdapter.ValidateConnectionAsync(Arg.Any<ConnectionInfo>(), Arg.Any<ICredentialsModel>()).Returns(response);

    private void MockExistingConnection() =>
        repository.TryGet(Arg.Is<ConnectionInfo>(x => x == ExpectedConnectionInfo), out _).Returns(call =>
        {
            call[1] = SonarQubeConnection;
            return true;
        });

    private void MockConnections(params ConnectionInfo[] connections) =>
        repository.TryGetAllConnectionsInfo(out _).Returns(call =>
        {
            call[0] = connections.ToList();
            return true;
        });

    private void MockBindableSetup(List<ServerProject> listedProjects)
    {
        MockConnections(ExpectedConnectionInfo);
        MockExistingConnection();
        slCoreConnectionAdapter.GetAllProjectsAsync(SonarQubeConnection).Returns(new ResponseStatusWithData<List<ServerProject>>(true, listedProjects));
    }

    private void MockBinding(BindingResult result) =>
        bindingControllerAdapter.ValidateAndBindAsync(Arg.Any<BindingRequest>(), Arg.Any<IConnectedModeUIManager>(), Arg.Any<CancellationToken>())
            .Returns(result);

    private static bool HasToken(ICredentialsModel credentials) =>
        credentials is TokenCredentialsModel tokenCredentials && tokenCredentials.Token.ToUnsecureString() == Token;
}

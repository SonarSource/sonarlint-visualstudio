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
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Automation;

[TestClass]
public class ConnectedModeAutomationTests
{
    private const string ServerUrl = "http://localhost:9000";
    private const string Token = "a-token";
    private const string ProjectKey = "a-project";

    private IServerConnectionsRepositoryAdapter repository;
    private ISlCoreConnectionAdapter slCoreConnectionAdapter;
    private IBindingControllerAdapter bindingControllerAdapter;
    private IConnectedModeUIManager uiManager;
    private ConnectedModeAutomation testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        repository = Substitute.For<IServerConnectionsRepositoryAdapter>();
        slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
        bindingControllerAdapter = Substitute.For<IBindingControllerAdapter>();
        uiManager = Substitute.For<IConnectedModeUIManager>();
        var connectedModeServices = Substitute.For<IConnectedModeServices>();
        connectedModeServices.ServerConnectionsRepositoryAdapter.Returns(repository);
        connectedModeServices.SlCoreConnectionAdapter.Returns(slCoreConnectionAdapter);
        var bindingServices = Substitute.For<IConnectedModeBindingServices>();
        bindingServices.BindingControllerAdapter.Returns(bindingControllerAdapter);
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
    [DataRow(null)]
    [DataRow("")]
    [DataRow("not a url")]
    [DataRow("localhost:9000/relative")]
    public async Task AddSonarQubeConnectionAsync_InvalidUrl_FailsWithoutCallingServices(string serverUrl)
    {
        var result = await testSubject.AddSonarQubeConnectionAsync(serverUrl, Token);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(ConnectedModeAutomation.InvalidServerUrlText);
        await slCoreConnectionAdapter.DidNotReceiveWithAnyArgs().ValidateConnectionAsync(default, default);
        repository.DidNotReceiveWithAnyArgs().TryAddConnection(default, default);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task AddSonarQubeConnectionAsync_NoToken_FailsWithoutCallingServices(string token)
    {
        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, token);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(ConnectedModeAutomation.MissingArgumentText);
        await slCoreConnectionAdapter.DidNotReceiveWithAnyArgs().ValidateConnectionAsync(default, default);
        repository.DidNotReceiveWithAnyArgs().TryAddConnection(default, default);
    }

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_ConnectionExists_FailsWithoutValidating()
    {
        repository.TryGet(Arg.Any<ConnectionInfo>(), out Arg.Any<ServerConnection>())
            .Returns(call =>
            {
                call[1] = new ServerConnection.SonarQube(new Uri(ServerUrl));
                return true;
            });

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(UiResources.ConnectionAlreadyExistsText);
        await slCoreConnectionAdapter.DidNotReceiveWithAnyArgs().ValidateConnectionAsync(default, default);
        repository.DidNotReceiveWithAnyArgs().TryAddConnection(default, default);
    }

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_ValidationFails_ReturnsValidationResultWithoutSaving()
    {
        var validation = new ResponseStatus(false, "invalid token");
        slCoreConnectionAdapter.ValidateConnectionAsync(Arg.Any<ConnectionInfo>(), Arg.Any<ICredentialsModel>()).Returns(validation);

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Should().BeSameAs(validation);
        repository.DidNotReceiveWithAnyArgs().TryAddConnection(default, default);
    }

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_Valid_ValidatesThenSavesTheConnectionAndToken()
    {
        slCoreConnectionAdapter.ValidateConnectionAsync(Arg.Any<ConnectionInfo>(), Arg.Any<ICredentialsModel>()).Returns(new ResponseStatus(true));
        repository.TryAddConnection(Arg.Any<Connection>(), Arg.Any<ICredentialsModel>()).Returns(true);

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Success.Should().BeTrue();
        var expectedInfo = new ConnectionInfo(ServerUrl, ConnectionServerType.SonarQube);
        Received.InOrder(() =>
        {
            slCoreConnectionAdapter.ValidateConnectionAsync(expectedInfo, Arg.Is<ICredentialsModel>(x => HasToken(x, Token)));
            repository.TryAddConnection(Arg.Is<Connection>(x => x.Info == expectedInfo), Arg.Is<ICredentialsModel>(x => HasToken(x, Token)));
        });
    }

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_SaveFails_Fails()
    {
        slCoreConnectionAdapter.ValidateConnectionAsync(Arg.Any<ConnectionInfo>(), Arg.Any<ICredentialsModel>()).Returns(new ResponseStatus(true));
        repository.TryAddConnection(Arg.Any<Connection>(), Arg.Any<ICredentialsModel>()).Returns(false);

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(ConnectedModeAutomation.SaveConnectionFailedText);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("not a url")]
    public async Task BindOpenSolutionAsync_InvalidUrl_FailsWithoutBinding(string serverUrl)
    {
        var result = await testSubject.BindOpenSolutionAsync(serverUrl, ProjectKey);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(ConnectedModeAutomation.InvalidServerUrlText);
        await bindingControllerAdapter.DidNotReceiveWithAnyArgs().ValidateAndBindAsync(default, default, default);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task BindOpenSolutionAsync_NoProjectKey_FailsWithoutBinding(string projectKey)
    {
        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, projectKey);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(ConnectedModeAutomation.MissingArgumentText);
        await bindingControllerAdapter.DidNotReceiveWithAnyArgs().ValidateAndBindAsync(default, default, default);
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_SendsTheManualRequestOfTheBindButton()
    {
        bindingControllerAdapter.ValidateAndBindAsync(Arg.Any<BindingRequest>(), Arg.Any<IConnectedModeUIManager>(), Arg.Any<CancellationToken>())
            .Returns(BindingResult.Success);

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeTrue();
        var expectedConnectionId = new ConnectionInfo(ServerUrl, ConnectionServerType.SonarQube).GetServerIdFromConnectionInfo();
        await bindingControllerAdapter.Received(1).ValidateAndBindAsync(
            new BindingRequest.Manual(ProjectKey, expectedConnectionId),
            uiManager,
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_BindingFails_ReturnsItsProblem()
    {
        bindingControllerAdapter.ValidateAndBindAsync(Arg.Any<BindingRequest>(), Arg.Any<IConnectedModeUIManager>(), Arg.Any<CancellationToken>())
            .Returns(BindingResult.ConnectionNotFound);

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(BindingResult.ConnectionNotFound.ProblemDescription);
    }

    private static bool HasToken(ICredentialsModel credentials, string token) =>
        credentials is TokenCredentialsModel tokenCredentials && tokenCredentials.Token.ToUnsecureString() == token;
}

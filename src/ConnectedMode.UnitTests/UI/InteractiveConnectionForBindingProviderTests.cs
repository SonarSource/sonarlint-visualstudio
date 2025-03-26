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

using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI;

[TestClass]
public class InteractiveConnectionForBindingProviderTests
{
    private IConnectedModeUIManager connectedModeUiManager;
    private IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter;
    private InteractiveConnectionForBindingProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        connectedModeUiManager = Substitute.For<IConnectedModeUIManager>();
        serverConnectionsRepositoryAdapter = Substitute.For<IServerConnectionsRepositoryAdapter>();
        testSubject = new InteractiveConnectionForBindingProvider(connectedModeUiManager, serverConnectionsRepositoryAdapter);
    }

    [DynamicData(nameof(RequestsAndConnections))]
    [DataTestMethod]
    public async Task GetServerConnectionAsync_ExistingConnectionWithCredentials_ReturnsWithoutInteraction(BindingRequest request, ServerConnection connection)
    {
        SetUpConnectionsAdapter(request, (true, connection));

        var result = await testSubject.GetServerConnectionAsync(request);

        result.Should().BeSameAs(connection);
        connectedModeUiManager.DidNotReceiveWithAnyArgs().ShowEditCredentialsDialogAsync(default);
    }

    [DynamicData(nameof(RequestsAndConnections))]
    [DataTestMethod]
    public async Task GetServerConnectionAsync_ExistingConnectionWithoutCredentials_SuccessfulCredentialsUpdate_ReturnsUpdatedConnection(BindingRequest request, ServerConnection connection)
    {
        var copyOfConnectionWithoutCredentials = connection.ToConnection().ToServerConnection();
        SetUpConnectionsAdapter(request, (true, copyOfConnectionWithoutCredentials), (true, connection));
        connectedModeUiManager.ShowEditCredentialsDialogAsync(Arg.Any<Connection>()).Returns(true);

        var result = await testSubject.GetServerConnectionAsync(request);

        result.Should().BeSameAs(connection);
    }

    [DynamicData(nameof(RequestsAndConnections))]
    [DataTestMethod]
    public async Task GetServerConnectionAsync_ExistingConnectionWithoutCredentials_FailedCredentialsUpdate_ReturnsConnectionWithoutCredentials(BindingRequest request, ServerConnection connection)
    {
        var copyOfConnectionWithoutCredentials = connection.ToConnection().ToServerConnection();
        SetUpConnectionsAdapter(request, (true, copyOfConnectionWithoutCredentials));
        connectedModeUiManager.ShowEditCredentialsDialogAsync(Arg.Any<Connection>()).Returns(false);

        var result = await testSubject.GetServerConnectionAsync(request);

        result.Should().BeSameAs(copyOfConnectionWithoutCredentials);
    }


    [DynamicData(nameof(RequestsAndConnections))]
    [DataTestMethod]
    public async Task GetServerConnectionAsync_ExistingConnectionWithoutCredentials_SuccessfulCredentialsUpdate_ConnectionNotStored_ReturnsNull(BindingRequest request, ServerConnection connection)
    {
        var copyOfConnectionWithoutCredentials = connection.ToConnection().ToServerConnection();
        SetUpConnectionsAdapter(request, (true, copyOfConnectionWithoutCredentials), (false, null));
        connectedModeUiManager.ShowEditCredentialsDialogAsync(Arg.Is<Connection>(x => x.ToServerConnection().Id == connection.Id)).Returns(true);

        var result = await testSubject.GetServerConnectionAsync(request);

        result.Should().BeNull();
    }

    [DynamicData(nameof(OtherThanSharedRequestsAndConnections))]
    [DataTestMethod]
    public async Task GetServerConnectionAsync_NonSharedRequest_MissingConnection_ReturnsNull(BindingRequest request, ServerConnection connection)
    {
        connectedModeUiManager.ShowTrustConnectionDialogAsync(connection, null).Returns(true);
        SetUpConnectionsAdapter(request, (false, null), (true, connection));

        var result = await testSubject.GetServerConnectionAsync(request);

        result.Should().BeNull();
        connectedModeUiManager.DidNotReceiveWithAnyArgs().ShowEditCredentialsDialogAsync(default);
    }


    [DynamicData(nameof(SharedRequestsAndConnections))]
    [DataTestMethod]
    public async Task GetServerConnectionAsync_SharedRequest_MissingConnection_NewConnectionTrusted_ExistingStoredCredentials_ReturnsNewConnectionWithCredentials(BindingRequest request, ServerConnection connection)
    {
        connectedModeUiManager.ShowTrustConnectionDialogAsync(Arg.Is<ServerConnection>(x => x.Id == connection.Id), null).Returns(true);
        SetUpConnectionsAdapter(request, (false, null), (true, connection));

        var result = await testSubject.GetServerConnectionAsync(request);

        result.Should().BeSameAs(connection);
        connectedModeUiManager.DidNotReceiveWithAnyArgs().ShowEditCredentialsDialogAsync(default);
    }

    [DynamicData(nameof(SharedRequestsAndConnections))]
    [DataTestMethod]
    public async Task GetServerConnectionAsync_SharedRequest_MissingConnection_NewConnectionTrusted_NoStoredCredentials_SuccessfulCredentialsUpdate_ReturnsNewConnectionWithAddedCredentials(BindingRequest request, ServerConnection connection)
    {

        var copyOfConnectionWithoutCredentials = connection.ToConnection().ToServerConnection();
        connectedModeUiManager.ShowTrustConnectionDialogAsync(Arg.Is<ServerConnection>(x => x.Id == connection.Id), null).Returns(true);
        SetUpConnectionsAdapter(request, (false, null), (true, copyOfConnectionWithoutCredentials), (true, connection));
        connectedModeUiManager.ShowEditCredentialsDialogAsync(Arg.Is<Connection>(x => x.ToServerConnection().Id == connection.Id)).Returns(true);

        var result = await testSubject.GetServerConnectionAsync(request);

        result.Should().BeSameAs(connection);
    }

    [DynamicData(nameof(SharedRequestsAndConnections))]
    [DataTestMethod]
    public async Task GetServerConnectionAsync_SharedRequest_MissingConnection_NewConnectionTrusted_NoStoredCredentials_FailedCredentialsUpdate_ReturnsConnectionWithoutCredentials(BindingRequest request, ServerConnection connection)
    {

        var copyOfConnectionWithoutCredentials = connection.ToConnection().ToServerConnection();
        connectedModeUiManager.ShowTrustConnectionDialogAsync(Arg.Is<ServerConnection>(x => x.Id == connection.Id), null).Returns(true);
        SetUpConnectionsAdapter(request, (false, null), (true, copyOfConnectionWithoutCredentials));
        connectedModeUiManager.ShowEditCredentialsDialogAsync(Arg.Is<Connection>(x => x.ToServerConnection().Id == connection.Id)).Returns(false);

        var result = await testSubject.GetServerConnectionAsync(request);

        result.Should().Be(copyOfConnectionWithoutCredentials);
    }

    [DynamicData(nameof(SharedRequestsAndConnections))]
    [DataTestMethod]
    public async Task GetServerConnectionAsync_SharedRequest_MissingConnection_NewConnectionTrusted_NoStoredCredentials_SuccessfulCredentialsUpdate_ConnectionNotStored_Connection(BindingRequest request, ServerConnection connection)
    {

        var copyOfConnectionWithoutCredentials = connection.ToConnection().ToServerConnection();
        connectedModeUiManager.ShowTrustConnectionDialogAsync(Arg.Is<ServerConnection>(x => x.Id == connection.Id), null).Returns(true);
        SetUpConnectionsAdapter(request, (false, null), (true, copyOfConnectionWithoutCredentials), (false, null));
        connectedModeUiManager.ShowEditCredentialsDialogAsync(Arg.Is<Connection>(x => x.ToServerConnection().Id == connection.Id)).Returns(true);

        var result = await testSubject.GetServerConnectionAsync(request);

        result.Should().BeNull();
    }

    private void SetUpConnectionsAdapter(BindingRequest request, params (bool result, ServerConnection connection)[] connections)
    {
        int index = 0;
        serverConnectionsRepositoryAdapter.TryGet(request.ConnectionId, out Arg.Any<ServerConnection>()).Returns(info =>
        {
            var connectionAndResult = connections[index];
            info[1] = connectionAndResult.connection;
            index = Math.Min(index + 1, connections.Length - 1);
            return connectionAndResult.result;
        });
    }

    public static object[][] SharedRequestsAndConnections => RequestsAndConnections.Where(x => x.First() is BindingRequest.Shared).ToArray();
    public static object[][] OtherThanSharedRequestsAndConnections => RequestsAndConnections.Where(x => x.First() is not BindingRequest.Shared).ToArray();
    public static object[][] RequestsAndConnections =>
    [
        [new BindingRequest.Manual("any", "http://anyhost/"), new ServerConnection.SonarQube(new Uri("http://anyhost/"), credentials: Substitute.For<IConnectionCredentials>())],
        [new BindingRequest.Manual("any", "https://sonarcloud.io/organizations/orgkey"), new ServerConnection.SonarCloud("orgkey", credentials: Substitute.For<IConnectionCredentials>())],
        [new BindingRequest.Assisted("http://anyhost/", "any", false), new ServerConnection.SonarQube(new Uri("http://anyhost/"), credentials: Substitute.For<IConnectionCredentials>())],
        [new BindingRequest.Assisted("https://sonarcloud.io/organizations/orgkey", "any", false), new ServerConnection.SonarCloud("orgkey", credentials: Substitute.For<IConnectionCredentials>())],
        [new BindingRequest.Shared(new SharedBindingConfigModel{Uri = new("http://anyhost/")}), new ServerConnection.SonarQube(new Uri("http://anyhost/"), credentials: Substitute.For<IConnectionCredentials>())],
        [new BindingRequest.Shared(new SharedBindingConfigModel{Organization = "orgkey"}), new ServerConnection.SonarCloud("orgkey", credentials: Substitute.For<IConnectionCredentials>())],
    ];
}

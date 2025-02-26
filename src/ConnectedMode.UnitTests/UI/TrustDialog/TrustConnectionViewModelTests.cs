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

using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.ConnectedMode.UI.TrustConnection;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.TrustDialog;

[TestClass]
public class TrustConnectionViewModelTests
{
    private TrustConnectionViewModel trustSonarCloudConnectionViewModel;
    private IConnectedModeServices connectedModeServices;
    private readonly string token = Guid.NewGuid().ToString();
    private TrustConnectionViewModel trustSonarQubeConnectionViewModel;
    private IProgressReporterViewModel progressReporterViewModel;
    private IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter;
    private ILogger logger;
    private readonly ServerConnection.SonarCloud sonarCloudServerConnection = new("myOrg");
    private readonly ServerConnection.SonarQube sonarQubeServerConnection = new(new Uri("http://localhost:9000"));

    [TestInitialize]
    public void TestInitialize()
    {
        connectedModeServices = Substitute.For<IConnectedModeServices>();
        progressReporterViewModel = Substitute.For<IProgressReporterViewModel>();
        trustSonarCloudConnectionViewModel = CreateTestSubject(sonarCloudServerConnection, token);
        trustSonarQubeConnectionViewModel = CreateTestSubject(sonarQubeServerConnection, token);

        MockServices();
    }

    [TestMethod]
    public void Ctor_SonarCloud_InitializesProperties()
    {
        trustSonarCloudConnectionViewModel.Connection.Info.Id.Should().Be("myOrg");
        trustSonarCloudConnectionViewModel.Connection.Info.ServerType.Should().Be(ConnectionServerType.SonarCloud);

        trustSonarQubeConnectionViewModel.Connection.Info.Id.Should().Be("http://localhost:9000/");
        trustSonarQubeConnectionViewModel.Connection.Info.ServerType.Should().Be(ConnectionServerType.SonarQube);

        trustSonarCloudConnectionViewModel.ProgressReporterViewModel.Should().NotBeNull();
    }

    [TestMethod]
    public async Task CreateConnectionsWithProgressAsync_InitializesDataAndReportsProgress()
    {
        await trustSonarCloudConnectionViewModel.CreateConnectionsWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
                    x.TaskToPerform == trustSonarCloudConnectionViewModel.CreateNewConnectionAsync &&
                    x.ProgressStatus == UiResources.CreatingConnectionProgressText &&
                    x.WarningText == UiResources.CreatingConnectionFailedText));
    }

    [TestMethod]
    public async Task CreateNewConnectionAsync_SonarCloud_AddsProvidedConnectionToRepository()
    {
        serverConnectionsRepositoryAdapter.TryAddConnection(Arg.Any<Connection>(), Arg.Any<ICredentialsModel>()).Returns(true);

        var succeeded = await trustSonarCloudConnectionViewModel.CreateNewConnectionAsync();

        succeeded.Success.Should().BeTrue();
        serverConnectionsRepositoryAdapter.Received(1).TryAddConnection(Arg.Is<Connection>(x => x.Info.Id == sonarCloudServerConnection.OrganizationKey),
            Arg.Is<ICredentialsModel>(x => VerifyExpectedToken(x)));
    }

    [TestMethod]
    public async Task CreateNewConnectionAsync_SonarQube_AddsProvidedConnectionToRepository()
    {
        serverConnectionsRepositoryAdapter.TryAddConnection(Arg.Any<Connection>(), Arg.Any<ICredentialsModel>()).Returns(true);

        var succeeded = await trustSonarQubeConnectionViewModel.CreateNewConnectionAsync();

        succeeded.Success.Should().BeTrue();
        serverConnectionsRepositoryAdapter.Received(1).TryAddConnection(Arg.Is<Connection>(x => x.Info.Id == sonarQubeServerConnection.ServerUri.ToString()),
            Arg.Is<ICredentialsModel>(x => VerifyExpectedToken(x)));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CreateNewConnectionAsync_ReturnsResultFromServerConnectionsRepositoryAdapter(bool expectedResult)
    {
        serverConnectionsRepositoryAdapter.TryAddConnection(Arg.Any<Connection>(), Arg.Any<ICredentialsModel>()).Returns(expectedResult);

        var succeeded = await trustSonarQubeConnectionViewModel.CreateNewConnectionAsync();

        succeeded.Success.Should().Be(expectedResult);
    }

    private TrustConnectionViewModel CreateTestSubject(ServerConnection serverConnection, string unsecureToken) =>
        new(connectedModeServices, progressReporterViewModel, serverConnection, unsecureToken.ToSecureString());

    private void MockServices()
    {
        serverConnectionsRepositoryAdapter = Substitute.For<IServerConnectionsRepositoryAdapter>();
        logger = Substitute.For<ILogger>();

        connectedModeServices.ServerConnectionsRepositoryAdapter.Returns(serverConnectionsRepositoryAdapter);
        connectedModeServices.Logger.Returns(logger);
    }

    private bool VerifyExpectedToken(ICredentialsModel credentialsModel)
    {
        var tokenCredentials = credentialsModel as TokenCredentialsModel;
        tokenCredentials.Should().NotBeNull();
        tokenCredentials.Token.ToUnsecureString().Should().Be(token);
        return true;
    }
}

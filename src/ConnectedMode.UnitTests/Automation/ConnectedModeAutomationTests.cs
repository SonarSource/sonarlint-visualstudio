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
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Automation;

[TestClass]
public class ConnectedModeAutomationTests
{
    private const string ServerUrl = "http://localhost:9000";
    private const string Token = "a-token";
    private const string ProjectKey = "a-project";
    private static readonly ConnectionInfo ExpectedConnectionInfo = new(ServerUrl, ConnectionServerType.SonarQube);

    private ISlCoreConnectionAdapter slCoreConnectionAdapter;
    private IServerConnectionsRepositoryAdapter repository;
    private IBindingControllerAdapter bindingControllerAdapter;
    private IConnectedModeUIManager uiManager;
    private ConnectedModeAutomation testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
        repository = Substitute.For<IServerConnectionsRepositoryAdapter>();
        bindingControllerAdapter = Substitute.For<IBindingControllerAdapter>();
        uiManager = Substitute.For<IConnectedModeUIManager>();
        testSubject = new ConnectedModeAutomation(slCoreConnectionAdapter, repository, bindingControllerAdapter, uiManager);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ConnectedModeAutomation, IConnectedModeAutomation>(
            MefTestHelpers.CreateExport<ISlCoreConnectionAdapter>(),
            MefTestHelpers.CreateExport<IServerConnectionsRepositoryAdapter>(),
            MefTestHelpers.CreateExport<IBindingControllerAdapter>(),
            MefTestHelpers.CreateExport<IConnectedModeUIManager>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ConnectedModeAutomation>();

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_ValidationFails_ReturnsItWithoutSaving()
    {
        var validation = new ResponseStatus(false, "invalid token");
        slCoreConnectionAdapter.ValidateConnectionAsync(Arg.Any<ConnectionInfo>(), Arg.Any<ICredentialsModel>()).Returns(validation);

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Should().BeSameAs(validation);
        repository.DidNotReceiveWithAnyArgs().TryAddConnection(default, default);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task AddSonarQubeConnectionAsync_Valid_ValidatesThenSavesWithTheSameToken(bool saved)
    {
        slCoreConnectionAdapter.ValidateConnectionAsync(Arg.Any<ConnectionInfo>(), Arg.Any<ICredentialsModel>()).Returns(new ResponseStatus(true));
        repository.TryAddConnection(Arg.Any<Connection>(), Arg.Any<ICredentialsModel>()).Returns(saved);

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Success.Should().Be(saved);
        result.WarningText.Should().Be(saved ? null : ConnectedModeAutomation.SaveConnectionFailedText);
        Received.InOrder(() =>
        {
            slCoreConnectionAdapter.ValidateConnectionAsync(ExpectedConnectionInfo, Arg.Is<ICredentialsModel>(x => HasToken(x)));
            repository.TryAddConnection(Arg.Is<Connection>(x => x.Info == ExpectedConnectionInfo), Arg.Is<ICredentialsModel>(x => HasToken(x)));
        });
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_SendsTheManualRequestOfTheBindButton()
    {
        MockBinding(BindingResult.Success);

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeTrue();
        await bindingControllerAdapter.Received(1).ValidateAndBindAsync(
            new BindingRequest.Manual(ProjectKey, ExpectedConnectionInfo.GetServerIdFromConnectionInfo()),
            uiManager,
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_BindingFails_ReturnsItsProblem()
    {
        MockBinding(BindingResult.ConnectionNotFound);

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(BindingResult.ConnectionNotFound.ProblemDescription);
    }

    private void MockBinding(BindingResult result) =>
        bindingControllerAdapter.ValidateAndBindAsync(Arg.Any<BindingRequest>(), Arg.Any<IConnectedModeUIManager>(), Arg.Any<CancellationToken>())
            .Returns(result);

    private static bool HasToken(ICredentialsModel credentials) =>
        credentials is TokenCredentialsModel tokenCredentials && tokenCredentials.Token.ToUnsecureString() == Token;
}

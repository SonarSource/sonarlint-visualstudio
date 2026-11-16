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

    private IServerConnectionCreator serverConnectionCreator;
    private IBindingControllerAdapter bindingControllerAdapter;
    private IConnectedModeUIManager uiManager;
    private ConnectedModeAutomation testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        serverConnectionCreator = Substitute.For<IServerConnectionCreator>();
        bindingControllerAdapter = Substitute.For<IBindingControllerAdapter>();
        uiManager = Substitute.For<IConnectedModeUIManager>();
        var connectedModeServices = Substitute.For<IConnectedModeServices>();
        connectedModeServices.ServerConnectionCreator.Returns(serverConnectionCreator);
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
    public async Task AddSonarQubeConnectionAsync_ValidationFails_ReturnsItWithoutAdding()
    {
        var validation = new ResponseStatus(false, "invalid token");
        serverConnectionCreator.ValidateAsync(Arg.Any<ConnectionInfo>(), Arg.Any<ICredentialsModel>()).Returns(validation);

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Should().BeSameAs(validation);
        serverConnectionCreator.DidNotReceiveWithAnyArgs().Add(default, default);
    }

    [TestMethod]
    public async Task AddSonarQubeConnectionAsync_Valid_ValidatesThenAddsWithTheSameToken()
    {
        var added = new ResponseStatus(true);
        serverConnectionCreator.ValidateAsync(Arg.Any<ConnectionInfo>(), Arg.Any<ICredentialsModel>()).Returns(new ResponseStatus(true));
        serverConnectionCreator.Add(Arg.Any<Connection>(), Arg.Any<ICredentialsModel>()).Returns(added);

        var result = await testSubject.AddSonarQubeConnectionAsync(ServerUrl, Token);

        result.Should().BeSameAs(added);
        Received.InOrder(() =>
        {
            serverConnectionCreator.ValidateAsync(ExpectedConnectionInfo, Arg.Is<ICredentialsModel>(x => HasToken(x, Token)));
            serverConnectionCreator.Add(Arg.Is<Connection>(x => x.Info == ExpectedConnectionInfo), Arg.Is<ICredentialsModel>(x => HasToken(x, Token)));
        });
    }

    [TestMethod]
    public async Task BindOpenSolutionAsync_SendsTheManualRequestOfTheBindButton()
    {
        bindingControllerAdapter.ValidateAndBindAsync(Arg.Any<BindingRequest>(), Arg.Any<IConnectedModeUIManager>(), Arg.Any<CancellationToken>())
            .Returns(BindingResult.Success);

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
        bindingControllerAdapter.ValidateAndBindAsync(Arg.Any<BindingRequest>(), Arg.Any<IConnectedModeUIManager>(), Arg.Any<CancellationToken>())
            .Returns(BindingResult.ConnectionNotFound);

        var result = await testSubject.BindOpenSolutionAsync(ServerUrl, ProjectKey);

        result.Success.Should().BeFalse();
        result.WarningText.Should().Be(BindingResult.ConnectionNotFound.ProblemDescription);
    }

    private static bool HasToken(ICredentialsModel credentials, string token) =>
        credentials is TokenCredentialsModel tokenCredentials && tokenCredentials.Token.ToUnsecureString() == token;
}

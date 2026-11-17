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

using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.ConnectedMode.Automation;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands.ConnectedModeMenu;

[TestClass]
public class ConnectedModeAutomationCommandTests
{
    private const string ServerUrl = "http://localhost:9000";

    private IConnectedModeAutomation automation;
    private IEnvironmentSettings environmentSettings;
    private TestLogger logger;
    private AddConnectionAutomationCommand addConnection;
    private BindAutomationCommand bind;

    [TestInitialize]
    public void TestInitialize()
    {
        automation = Substitute.For<IConnectedModeAutomation>();
        environmentSettings = Substitute.For<IEnvironmentSettings>();
        environmentSettings.AreConnectedModeAutomationCommandsEnabled().Returns(true);
        logger = new TestLogger();
        var threadHandling = new NoOpThreadHandler();
        addConnection = new AddConnectionAutomationCommand(automation, environmentSettings, threadHandling, logger);
        bind = new BindAutomationCommand(automation, environmentSettings, threadHandling, logger);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void QueryStatus_FollowsTheEnvironmentGate(bool enabled)
    {
        environmentSettings.AreConnectedModeAutomationCommandsEnabled().Returns(enabled);
        var command = CommandHelper.CreateRandomOleMenuCommand();

        addConnection.QueryStatus(command, EventArgs.Empty);

        command.Supported.Should().Be(enabled);
        command.Visible.Should().Be(enabled);
        command.Enabled.Should().Be(enabled);
    }

    [TestMethod]
    public void Invoke_Disabled_ThrowsWithoutCallingAutomation()
    {
        environmentSettings.AreConnectedModeAutomationCommandsEnabled().Returns(false);

        // The command object still reports Enabled: a caller can raise a command without querying its status.
        var act = () => Invoke(addConnection, $"{ServerUrl} token");

        act.Should().Throw<InvalidOperationException>().WithMessage(ConnectedModeAutomationCommandBase.DisabledText);
        automation.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(ServerUrl)]
    [DataRow(ServerUrl + " token extra")]
    public void Invoke_WrongNumberOfArguments_ThrowsWithoutCallingAutomation(string argument)
    {
        var act = () => Invoke(addConnection, argument);

        act.Should().Throw<ArgumentException>().WithMessage("AddConnection expects the arguments: <serverUrl> <token>");
        automation.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void AddConnection_Succeeds_CallsAutomationAndLogs()
    {
        automation.AddSonarQubeConnectionAsync(ServerUrl, "token").Returns(new ResponseStatus(true));

        Invoke(addConnection, $"  {ServerUrl}   token ");

        automation.Received(1).AddSonarQubeConnectionAsync(ServerUrl, "token");
        logger.AssertPartialOutputStringExists("[E2E] AddConnection succeeded");
    }

    [TestMethod]
    public void AddConnection_Fails_ThrowsWithTheReasonButNotTheToken()
    {
        automation.AddSonarQubeConnectionAsync(ServerUrl, "secret-token").Returns(new ResponseStatus(false, "invalid token"));

        var act = () => Invoke(addConnection, $"{ServerUrl} secret-token");

        act.Should().Throw<InvalidOperationException>().WithMessage("AddConnection failed: invalid token");
        logger.AssertPartialOutputStringExists("[E2E] AddConnection failed: invalid token");
        logger.AssertPartialOutputStringDoesNotExist("secret-token");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void AddConnection_FailsWithoutReason_ThrowsWithAFallbackReason(string warningText)
    {
        automation.AddSonarQubeConnectionAsync(ServerUrl, "token").Returns(new ResponseStatus(false, warningText));

        var act = () => Invoke(addConnection, $"{ServerUrl} token");

        act.Should().Throw<InvalidOperationException>().WithMessage($"AddConnection failed: {ConnectedModeAutomationCommandBase.NoReasonText}");
        logger.AssertPartialOutputStringExists($"[E2E] AddConnection failed: {ConnectedModeAutomationCommandBase.NoReasonText}");
    }

    [TestMethod]
    public void Bind_AutomationThrows_ReportsItAsAFailure()
    {
        var error = new UriFormatException("Invalid URI: The format of the URI could not be determined.");
        automation.BindOpenSolutionAsync("not-a-url", "project").Returns<Task<ResponseStatus>>(_ => throw error);

        var act = () => Invoke(bind, "not-a-url project");

        act.Should().Throw<InvalidOperationException>().WithMessage($"Bind failed: {error.Message}").WithInnerException<UriFormatException>();
        logger.AssertPartialOutputStringExists($"[E2E] Bind failed: {error.Message}");
    }

    [TestMethod]
    public void Bind_Succeeds_CallsAutomationWithServerAndProjectKey()
    {
        automation.BindOpenSolutionAsync(ServerUrl, "project").Returns(new ResponseStatus(true));

        Invoke(bind, $"{ServerUrl} project");

        automation.Received(1).BindOpenSolutionAsync(ServerUrl, "project");
        logger.AssertPartialOutputStringExists("[E2E] Bind succeeded");
    }

    [TestMethod]
    public void Bind_Fails_Throws()
    {
        automation.BindOpenSolutionAsync(ServerUrl, "project").Returns(new ResponseStatus(false, "no connection"));

        var act = () => Invoke(bind, $"{ServerUrl} project");

        act.Should().Throw<InvalidOperationException>().WithMessage("Bind failed: no connection");
    }

    private static void Invoke(ConnectedModeAutomationCommandBase command, string argument) =>
        command.Invoke(CommandHelper.CreateRandomOleMenuCommand(), new OleMenuCmdEventArgs(argument, IntPtr.Zero));
}

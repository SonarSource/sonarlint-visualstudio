/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding.Suggestion;

[TestClass]
public class UpdateTokenNotificationTests
{
    private INotificationService notificationService;
    private IUpdateTokenNotification testSubject;
    private IConnectedModeUIManager connectedModeUiManager;
    private readonly ServerConnection.SonarCloud serverConnection = new("myOrg");
    private IServerConnectionsRepository serverConnectionsRepository;
    private IMessageBox messageBox;

    [TestInitialize]
    public void TestInitialize()
    {
        notificationService = Substitute.For<INotificationService>();
        connectedModeUiManager = Substitute.For<IConnectedModeUIManager>();
        serverConnectionsRepository = Substitute.For<IServerConnectionsRepository>();
        messageBox = Substitute.For<IMessageBox>();
        testSubject = new UpdateTokenNotification(notificationService, connectedModeUiManager, serverConnectionsRepository, messageBox);
    }

    [TestMethod]
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<UpdateTokenNotification, IUpdateTokenNotification>(
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IConnectedModeUIManager>(),
            MefTestHelpers.CreateExport<IServerConnectionsRepository>(),
            MefTestHelpers.CreateExport<IMessageBox>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<UpdateTokenNotification>();

    [TestMethod]
    public void Show_ServerConnectionDoesNotExist_DoesNotShowUpdateTokenNotification()
    {
        MockServerConnection(serverConnectionToReturn: null);

        testSubject.Show(serverConnection.Id);

        notificationService.DidNotReceive().ShowNotification(Arg.Any<Notification>());
    }

    [TestMethod]
    public void Show_GeneratesCorrectNotificationStructure()
    {
        MockServerConnection(serverConnection);

        testSubject.Show(serverConnection.Id);

        notificationService.Received(1).ShowNotification(Arg.Is<Notification>(x => IsExpectedNotification(x, serverConnection.ToConnection().Info.Id)));
    }

    [TestMethod]
    public void Show_EditCredentialsCommand_ShowsEditCredentialsDialog()
    {
        MockServerConnection(serverConnection);

        testSubject.Show(serverConnection.Id);

        InvokeNotificationCommand(BindingStrings.UpdateTokenNotificationEditCredentialsOptionText);
        connectedModeUiManager.Received(1).ShowEditCredentialsDialogAsync(Arg.Is<Connection>(connection => connection.Info.Id == serverConnection.ToConnection().Info.Id));
    }

    [TestMethod]
    public void Show_EditCredentialsCommand_EditingCredentialsSucceeds_ShowsMessage()
    {
        MockServerConnection(serverConnection);
        MockShowEditCredentialsDialog(dialogResult: true, new ResponseStatus(true));

        testSubject.Show(serverConnection.Id);
        InvokeNotificationCommand(BindingStrings.UpdateTokenNotificationEditCredentialsOptionText);

        messageBox.Received(1).Show(string.Format(BindingStrings.UpdateTokenSuccessfullyMessageBoxText, serverConnection.ToConnection().Info.Id),
            BindingStrings.UpdateTokenSuccessfullyMessageBoxCaption,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("warning test")]
    public void Show_EditCredentialsCommand_EditingCredentialsFails_ShowsMessage(string warningText)
    {
        MockServerConnection(serverConnection);
        MockShowEditCredentialsDialog(dialogResult: true, new ResponseStatus(false, warningText));

        testSubject.Show(serverConnection.Id);
        InvokeNotificationCommand(BindingStrings.UpdateTokenNotificationEditCredentialsOptionText);

        messageBox.Received(1).Show(
            string.Format(BindingStrings.UpdateTokenUnsuccessfullyMessageBoxText, serverConnection.ToConnection().Info.Id, warningText ?? BindingStrings.UpdateTokenUnsuccessfullyCheckLogsText),
            BindingStrings.UpdateTokenUnsuccessfullyMessageBoxCaption,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    [TestMethod]
    public void Show_EditCredentialsCommand_EditingCredentialsFails_DoesNotShowMessage()
    {
        MockServerConnection(serverConnection);
        MockShowEditCredentialsDialog(dialogResult: false, default);

        testSubject.Show(serverConnection.Id);
        InvokeNotificationCommand(BindingStrings.UpdateTokenNotificationEditCredentialsOptionText);

        messageBox.DidNotReceiveWithAnyArgs().Show(default, default, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [TestMethod]
    public void Show_DismissCommand_DoesNotShowCredentialsDialog()
    {
        MockServerConnection(serverConnection);

        testSubject.Show(serverConnection.Id);

        InvokeNotificationCommand(BindingStrings.UpdateTokenDismissOptionText);
        notificationService.Received(1).CloseNotification();
    }

    private void InvokeNotificationCommand(string commandText)
    {
        var notification = (Notification)notificationService.ReceivedCalls().Single().GetArguments()[0];
        var editCredentialsAction = notification.Actions.First(a => a.CommandText == commandText);
        editCredentialsAction.Action(null);
    }

    private bool IsExpectedNotification(Notification notification, string connectionId)
    {
        notification.Id.Should().Be(string.Format(UpdateTokenNotification.IdTemplate, connectionId));
        notification.Message.Should().Be(string.Format(BindingStrings.UpdateTokenNotificationText, connectionId));
        var notificationActions = notification.Actions.ToArray();
        notificationActions.Should().HaveCount(2);
        notificationActions[0].CommandText.Should().Be(BindingStrings.UpdateTokenNotificationEditCredentialsOptionText);
        notificationActions[0].ShouldDismissAfterAction.Should().BeTrue();
        notificationActions[1].CommandText.Should().Be(BindingStrings.UpdateTokenDismissOptionText);
        notificationActions[1].ShouldDismissAfterAction.Should().BeTrue();
        notification.CloseOnSolutionClose.Should().Be(true);
        notification.ShowOncePerSession.Should().Be(true);
        return true;
    }

    private void MockServerConnection(ServerConnection.SonarCloud serverConnectionToReturn) =>
        serverConnectionsRepository.TryGet(serverConnectionToReturn?.Id ?? Arg.Any<string>(), out _).Returns(x =>
        {
            x[1] = serverConnectionToReturn;
            return serverConnectionToReturn != null;
        });

    private void MockShowEditCredentialsDialog(bool dialogResult, ResponseStatus responseStatus) =>
        connectedModeUiManager.ShowEditCredentialsDialogAsync(Arg.Is<Connection>(connection => connection.Info.Id == serverConnection.ToConnection().Info.Id)).Returns((dialogResult, responseStatus));
}

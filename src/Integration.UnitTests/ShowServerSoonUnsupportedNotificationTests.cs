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

using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests;

[TestClass]
public class ShowServerSoonUnsupportedNotificationTests
{
    private INotificationService notificationService;
    private IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction;
    private ShowServerSoonUnsupportedNotification testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        notificationService = Substitute.For<INotificationService>();
        doNotShowAgainNotificationAction = Substitute.For<IDoNotShowAgainNotificationAction>();
        testSubject = new ShowServerSoonUnsupportedNotification(notificationService, doNotShowAgainNotificationAction);
    }

    [TestMethod]
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<ShowServerSoonUnsupportedNotification, IShowServerSoonUnsupportedNotification>(
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IDoNotShowAgainNotificationAction>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<ShowServerSoonUnsupportedNotification>();

    [TestMethod]
    public void ShowSoonUnsupportedMessage_ShowsNotificationWithCorrectMessageAndId()
    {
        const string text = "Server will soon be unsupported";
        const string notificationId = "server.soon.unsupported.9.9";

        testSubject.ShowSoonUnsupportedMessage(text, notificationId);

        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(n =>
            n.Id == notificationId &&
            n.Message == text));
    }

    [TestMethod]
    public void ShowSoonUnsupportedMessage_ConfiguresNotificationCorrectly()
    {
        testSubject.ShowSoonUnsupportedMessage("text", "id");

        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(n =>
            !n.CloseOnSolutionClose));
    }

    [TestMethod]
    public void ShowSoonUnsupportedMessage_HasDoNotShowAgainAction()
    {
        testSubject.ShowSoonUnsupportedMessage("text", "id");

        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(n =>
            n.Actions.Count() == 1 &&
            n.Actions.Single() == doNotShowAgainNotificationAction));
    }

    [TestMethod]
    public void ShowSoonUnsupportedMessage_UsesNotificationIdForDoNotShowAgainStorage()
    {
        const string notificationId = "server.soon.unsupported.9.9";

        testSubject.ShowSoonUnsupportedMessage("text", notificationId);

        var notification = GetNotification();
        notification.Id.Should().Be(notificationId);
    }

    private INotification GetNotification() =>
        notificationService.ReceivedCalls()
            .Select(call => call.GetArguments()[0] as INotification)
            .First();
}

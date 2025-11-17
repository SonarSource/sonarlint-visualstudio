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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.OpenInIDE;

[TestClass]
public class OpenInIdeNotificationTests
{
    private const string AFilePath = @"file\path\123.cs";

    private static readonly Guid AToolWindowId = Guid.NewGuid();

    private OpenInIdeNotification testSubject;
    private IToolWindowService toolWindowService;
    private INotificationService notificationService;
    private IOutputWindowService outputWindowService;
    private IBrowserService browserService;

    [TestInitialize]
    public void TestInitialize()
    {
        toolWindowService = Substitute.For<IToolWindowService>();
        notificationService = Substitute.For<INotificationService>();
        outputWindowService = Substitute.For<IOutputWindowService>();
        browserService = Substitute.For<IBrowserService>();

        testSubject = new OpenInIdeNotification(toolWindowService, notificationService, outputWindowService, browserService);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<OpenInIdeNotification, IOpenInIdeNotification>(
            MefTestHelpers.CreateExport<IToolWindowService>(),
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IOutputWindowService>(),
            MefTestHelpers.CreateExport<IBrowserService>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OpenInIdeNotification>();
    }

    [TestMethod]
    public void UnableToLocateIssue_DisplaysNotification_WithMessage()
    {
        testSubject.UnableToLocateIssue(AFilePath, AToolWindowId);

        AssertReceivedNotificationWithMessage(string.Format(OpenInIdeResources.Notification_UnableToLocateIssue, AFilePath), AToolWindowId);
    }

    [TestMethod]
    public void UnableToLocateIssue_ShouldNotShowOnlyOncePerSession()
    {
        testSubject.UnableToLocateIssue(AFilePath, AToolWindowId);

        AssertNotificationNotShownOnlyOncePerSession(AToolWindowId);
    }

    [TestMethod]
    public void UnableToLocateIssue_DisplaysNotification_WithActionToOpenDocsInBrowser()
    {
        testSubject.UnableToLocateIssue(AFilePath, AToolWindowId);

        SimulateNotificationActionInvoked(0);

        AssertOpenedDocsPageInBrowser();
    }

    [TestMethod]
    public void UnableToLocateIssue_DisplaysNotification_WithActionToShowLogs()
    {
        testSubject.UnableToLocateIssue(AFilePath, AToolWindowId);

        SimulateNotificationActionInvoked(1);

        AssertShownLogsWindow();
    }

    [TestMethod]
    public void UnableToLocateIssue_NotificationActions_ShouldNotDismissNotification()
    {
        testSubject.UnableToLocateIssue(AFilePath, AToolWindowId);

        AssertActionsAreNotDismissingNotification(AToolWindowId);
    }

    [TestMethod]
    public void UnableToLocateIssue_NotificationActions_ShouldBeExactlyTwo()
    {
        testSubject.UnableToLocateIssue(AFilePath, AToolWindowId);

        AssertActionButtonsAreExactly(2);
    }

    [TestMethod]
    public void UnableToLocateIssue_BringsToolWindowToFocus()
    {
        testSubject.UnableToLocateIssue(AFilePath, AToolWindowId);

        AssertBroughtToolWindowToFocus(AToolWindowId);
    }

    [TestMethod]
    public void UnableToOpenFile_DisplaysNotification_WithMessage()
    {
        testSubject.UnableToOpenFile(AFilePath, AToolWindowId);

        AssertReceivedNotificationWithMessage(string.Format(OpenInIdeResources.Notification_UnableToOpenFile, AFilePath), AToolWindowId);
    }

    [TestMethod]
    public void UnableToOpenFile_ShouldNotShowOnlyOncePerSession()
    {
        testSubject.UnableToOpenFile(AFilePath, AToolWindowId);

        AssertNotificationNotShownOnlyOncePerSession(AToolWindowId);
    }

    [TestMethod]
    public void UnableToOpenFile_DisplaysNotification_WithActionToOpenDocsInBrowser()
    {
        testSubject.UnableToOpenFile(AFilePath, AToolWindowId);

        SimulateNotificationActionInvoked(0);

        AssertOpenedDocsPageInBrowser();
    }

    [TestMethod]
    public void UnableToOpenFile_DisplaysNotification_WithActionToShowLogs()
    {
        testSubject.UnableToOpenFile(AFilePath, AToolWindowId);

        SimulateNotificationActionInvoked(1);

        AssertShownLogsWindow();
    }

    [TestMethod]
    public void UnableToOpenFile_NotificationActions_ShouldNotDismissNotification()
    {
        testSubject.UnableToOpenFile(AFilePath, AToolWindowId);

        AssertActionsAreNotDismissingNotification(AToolWindowId);
    }

    [TestMethod]
    public void UnableToOpenFile_NotificationActions_ShouldBeExactlyTwo()
    {
        testSubject.UnableToOpenFile(AFilePath, AToolWindowId);

        AssertActionButtonsAreExactly(2);
    }

    [TestMethod]
    public void UnableToOpenFile_BringsToolWindowToFocus()
    {
        testSubject.UnableToOpenFile(AFilePath, AToolWindowId);

        AssertBroughtToolWindowToFocus(AToolWindowId);
    }

    [TestMethod]
    public void InvalidRequest_DisplaysNotification_WithMessage()
    {
        testSubject.InvalidRequest(AFilePath, AToolWindowId);

        AssertReceivedNotificationWithMessage(string.Format(OpenInIdeResources.Notification_InvalidConfiguration, AFilePath), AToolWindowId);
    }

    [TestMethod]
    public void InvalidRequest_ShouldNotShowOnlyOncePerSession()
    {
        testSubject.InvalidRequest(AFilePath, AToolWindowId);

        AssertNotificationNotShownOnlyOncePerSession(AToolWindowId);
    }

    [TestMethod]
    public void InvalidRequest_DisplaysNotification_WithActionToShowLogs()
    {
        testSubject.InvalidRequest(AFilePath, AToolWindowId);

        SimulateNotificationActionInvoked(0);

        AssertShownLogsWindow();
    }

    [TestMethod]
    public void InvalidRequest_NotificationActions_ShouldNotDismissNotification()
    {
        testSubject.InvalidRequest(AFilePath, AToolWindowId);

        AssertActionsAreNotDismissingNotification(AToolWindowId);
    }

    [TestMethod]
    public void InvalidRequest_NotificationActions_ShouldBeExactlyOne()
    {
        testSubject.InvalidRequest(AFilePath, AToolWindowId);

        AssertActionButtonsAreExactly(1);
    }

    [TestMethod]
    public void InvalidRequest_BringsToolWindowToFocus()
    {
        testSubject.InvalidRequest(AFilePath, AToolWindowId);

        AssertBroughtToolWindowToFocus(AToolWindowId);
    }

    [TestMethod]
    public void Clear_ClosesAnyOpenNotification()
    {
        testSubject.Clear();

        notificationService.Received(1).CloseNotification();
    }

    [TestMethod]
    public void Dispose_ClosesAnyOpenNotification()
    {
        testSubject.Dispose();

        notificationService.Received(1).CloseNotification();
    }

    private void AssertReceivedNotificationWithMessage(string message, Guid toolWindowId)
    {
        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(x => x.Message == message), toolWindowId);
    }

    private void AssertNotificationNotShownOnlyOncePerSession(Guid toolWindowId)
    {
        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(x => !x.ShowOncePerSession), toolWindowId);
    }

    private void AssertOpenedDocsPageInBrowser()
    {
        browserService.Received(1).Navigate(DocumentationLinks.OpenInIdeIssueLocation);
    }

    private void AssertShownLogsWindow()
    {
        outputWindowService.Received(1).Show();
    }

    private void AssertActionsAreNotDismissingNotification(Guid toolWindowId)
    {
        notificationService.Received(1).ShowNotification(Arg.Is<Notification>(n =>
            n.Actions.All(a => !a.ShouldDismissAfterAction)
        ), toolWindowId);
    }

    private void AssertActionButtonsAreExactly(int count)
    {
        var notification = notificationService.ReceivedCalls().Single().GetArguments()[0] as Notification;
        notification.Should().NotBeNull();
        notification!.Actions.Count().Should().Be(count);
    }

    private void AssertBroughtToolWindowToFocus(Guid toolWindowId)
    {
        toolWindowService.Show(toolWindowId);
    }

    private void SimulateNotificationActionInvoked(int actionIndex)
    {
        var notification = notificationService.ReceivedCalls().Single().GetArguments()[0] as Notification;
        notification.Should().NotBeNull();
        notification!.Actions.ToList()[actionIndex].Action.Invoke(notification);
    }
}

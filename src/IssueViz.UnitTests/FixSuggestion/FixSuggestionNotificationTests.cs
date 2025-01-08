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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.FixSuggestion;

[TestClass]
public class FixSuggestionNotificationTests
{
    private const string AFilePath = @"C:\Data\repo\file.cs";

    private FixSuggestionNotification testSubject;
    private INotificationService notificationService;
    private IOutputWindowService outputWindowService;
    private IBrowserService browserService;

    [TestInitialize]
    public void TestInitialize()
    {
        notificationService = Substitute.For<INotificationService>();
        outputWindowService = Substitute.For<IOutputWindowService>();
        browserService = Substitute.For<IBrowserService>();

        testSubject = new FixSuggestionNotification(notificationService, outputWindowService, browserService);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<FixSuggestionNotification, IFixSuggestionNotification>(
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IOutputWindowService>(),
            MefTestHelpers.CreateExport<IBrowserService>());
    }

    [TestMethod]
    public void UnableToOpenFile_DisplaysNotification_WithMessage()
    {
        testSubject.UnableToOpenFile(AFilePath);

        AssertReceivedNotificationWithMessage(string.Format(FixSuggestionResources.InfoBarUnableToOpenFile, AFilePath));
    }

    [TestMethod]
    public void UnableToOpenFile_ShouldNotShowOnlyOncePerSession()
    {
        testSubject.UnableToOpenFile(AFilePath);

        AssertNotificationNotShownOnlyOncePerSession();
    }

    [TestMethod]
    public void UnableToOpenFile_DisplaysNotification_WithActionToOpenDocsInBrowser()
    {
        testSubject.UnableToOpenFile(AFilePath);

        SimulateNotificationActionInvoked(0);

        AssertOpenedDocsPageInBrowser();
    }

    [TestMethod]
    public void UnableToOpenFile_DisplaysNotification_WithActionToShowLogs()
    {
        testSubject.UnableToOpenFile(AFilePath);

        SimulateNotificationActionInvoked(1);

        AssertShownLogsWindow();
    }

    [TestMethod]
    public void UnableToOpenFile_NotificationActions_ShouldNotDismissNotification()
    {
        testSubject.UnableToOpenFile(AFilePath);

        AssertActionsAreNotDismissingNotification();
    }

    [TestMethod]
    public void InvalidRequest_DisplaysNotification_WithMessage()
    {
        testSubject.InvalidRequest(AFilePath);

        AssertReceivedNotificationWithMessage(string.Format(FixSuggestionResources.InfoBarInvalidRequest, AFilePath));
    }

    [TestMethod]
    public void InvalidRequest_ShouldNotShowOnlyOncePerSession()
    {
        testSubject.InvalidRequest(AFilePath);

        AssertNotificationNotShownOnlyOncePerSession();
    }

    [TestMethod]
    public void InvalidRequest_DisplaysNotification_WithActionToOpenDocsInBrowser()
    {
        testSubject.InvalidRequest(AFilePath);

        SimulateNotificationActionInvoked(0);

        AssertOpenedDocsPageInBrowser();
    }

    [TestMethod]
    public void InvalidRequest_DisplaysNotification_WithActionToShowLogs()
    {
        testSubject.InvalidRequest(AFilePath);

        SimulateNotificationActionInvoked(1);

        AssertShownLogsWindow();
    }

    [TestMethod]
    public void InvalidRequest_NotificationActions_ShouldNotDismissNotification()
    {
        testSubject.InvalidRequest(AFilePath);

        AssertActionsAreNotDismissingNotification();
    }

    [TestMethod]
    public void UnableToLocateIssue_DisplaysNotification_WithMessage()
    {
        testSubject.UnableToLocateIssue(AFilePath);

        AssertReceivedNotificationWithMessage(string.Format(FixSuggestionResources.InfoBarUnableToLocateFixSuggestion, AFilePath));
    }

    [TestMethod]
    public void UnableToLocateIssue_ShouldNotShowOnlyOncePerSession()
    {
        testSubject.UnableToLocateIssue(AFilePath);

        AssertNotificationNotShownOnlyOncePerSession();
    }

    [TestMethod]
    public void UnableToLocateIssue_DisplaysNotification_WithActionToOpenDocsInBrowser()
    {
        testSubject.UnableToLocateIssue(AFilePath);

        SimulateNotificationActionInvoked(0);

        AssertOpenedDocsPageInBrowser();
    }

    [TestMethod]
    public void UnableToLocateIssue_DisplaysNotification_WithActionToShowLogs()
    {
        testSubject.UnableToLocateIssue(AFilePath);

        SimulateNotificationActionInvoked(1);

        AssertShownLogsWindow();
    }

    [TestMethod]
    public void UnableToLocateIssue_NotificationActions_ShouldNotDismissNotification()
    {
        testSubject.UnableToLocateIssue(AFilePath);

        AssertActionsAreNotDismissingNotification();
    }

    [TestMethod]
    public void Clear_TriggersNotificationServiceClear()
    {
        testSubject.Clear();

        notificationService.Received(1).CloseNotification();
    }

    private void AssertReceivedNotificationWithMessage(string message)
    {
        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(x => x.Message == message));
    }

    private void AssertNotificationNotShownOnlyOncePerSession()
    {
        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(x => !x.ShowOncePerSession));
    }

    private void AssertOpenedDocsPageInBrowser()
    {
        browserService.Received(1).Navigate(DocumentationLinks.OpenInIdeIssueLocation);
    }

    private void AssertShownLogsWindow()
    {
        outputWindowService.Received(1).Show();
    }

    private void AssertActionsAreNotDismissingNotification()
    {
        notificationService.Received(1).ShowNotification(Arg.Is<Notification>(n =>
            n.Actions.All(a => !a.ShouldDismissAfterAction)
        ));
    }

    private void SimulateNotificationActionInvoked(int actionIndex)
    {
        var notification = notificationService.ReceivedCalls().Single().GetArguments()[0] as Notification;
        notification.Should().NotBeNull();
        notification!.Actions.ToList()[actionIndex].Action.Invoke(notification);
    }
}

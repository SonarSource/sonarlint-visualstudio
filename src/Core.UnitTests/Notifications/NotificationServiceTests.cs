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

using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Notifications;

[TestClass]
public class NotificationServiceTests
{
    private IInfoBarManager infoBarManager;
    private IDisabledNotificationsStorage disabledNotificationsStorage;
    private IActiveSolutionTracker activeSolutionTracker;
    private IThreadHandling threadHandling;
    private TestLogger logger;
    private NotificationService testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        infoBarManager = Substitute.For<IInfoBarManager>();
        disabledNotificationsStorage = Substitute.For<IDisabledNotificationsStorage>();
        activeSolutionTracker = Substitute.For<IActiveSolutionTracker>();
        threadHandling = Substitute.For<IThreadHandling>();
        logger = new TestLogger();
        testSubject = new NotificationService(infoBarManager, disabledNotificationsStorage, activeSolutionTracker, threadHandling, logger);

        AllowRunningUiThreadActions();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<NotificationService, INotificationService>(
            MefTestHelpers.CreateExport<IInfoBarManager>(),
            MefTestHelpers.CreateExport<IDisabledNotificationsStorage>(),
            MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void CheckIsNonSharedMefComponent()
    {
        MefTestHelpers.CheckIsNonSharedMefComponent<NotificationService>();
    }

    [TestMethod]
    public void ShowNotification_NullNotification_ArgumentNullException()
    {
        var act = () => testSubject.ShowNotification(null);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("notification");
    }

    [TestMethod]
    public void ShowNotification_WhenNotificationIsDisabled_NotificationNotShown()
    {
        var attachedNotification = ShowNotification(id: "some id", disabled: true);

        AssertNotificationNotShown(attachedNotification);
    }

    [TestMethod]
    public void ShowNotification_WhenNewNotificationIsDisabled_DoesNotClosePrevious()
    {
        var attachedNotification1 = ShowNotification(id: "some id1", disabled: false);
        var attachedNotification2 = ShowNotification(id: "some id2", disabled: true);

        AssertNotificationNotClosed(attachedNotification1);
        AssertNotificationNotShown(attachedNotification2);
    }

    [TestMethod]
    public void ShowNotification_SubscribesToInfoBarEvents()
    {
        var attachedNotification = ShowNotification();

        AssertSubscribedToInfoBarEvents(attachedNotification);
    }

    [TestMethod]
    public void ShowNotification_AttachesInfoBarToWindow_RunsOnUIThread()
    {
        var notification = CreateNotification();
        AttachNotification(notification);

        // Prevent the ThreadHandling action to run immediately and capture it for later execution
        Action showInfoBar = null;
        var captureParamThreadHandling = Substitute.For<IThreadHandling>();
        captureParamThreadHandling.RunOnUIThreadAsync(Arg.Do<Action>(arg => showInfoBar = arg));
        var threadHandledTestSubject = new NotificationService(infoBarManager, disabledNotificationsStorage, activeSolutionTracker, captureParamThreadHandling, logger);

        // Trigger the ShowNotification without running the action to validate that the UI action is the one responsible for showing the notification
        threadHandledTestSubject.ShowNotification(notification);
        infoBarManager.DidNotReceiveWithAnyArgs().AttachInfoBarToMainWindow(Arg.Any<string>(), Arg.Any<SonarLintImageMoniker>(), Arg.Any<string[]>());

        // Run the action captured by the ThreadHandling to validate that the InfoBar is shown
        showInfoBar();
        infoBarManager.ReceivedWithAnyArgs(1).AttachInfoBarToMainWindow(Arg.Any<string>(), Arg.Any<SonarLintImageMoniker>(), Arg.Any<string[]>());
        captureParamThreadHandling.ReceivedWithAnyArgs(1).RunOnUIThreadAsync(Arg.Any<Action>());
    }

    [TestMethod]
    public void ShowNotification_InfoBarCreatedCorrectly()
    {
        var attachedNotification = ShowNotification(actions:
        [
            new NotificationAction("button1", _ => { }, false),
            new NotificationAction("button2", _ => { }, false),
            new NotificationAction("button3", _ => { }, false)
        ]);

        AssertNotificationWasShown(attachedNotification);
    }

    [TestMethod]
    public void ShowNotification_WithSameId_OnceWhenPerSessionEnabled_IsShownOnlyOnce()
    {
        const string notificationId = "some id";

        var attachedNotification1 = ShowNotification(id: notificationId, oncePerSession: true);
        var attachedNotification2 = ShowNotification(id: notificationId, oncePerSession: true);

        AssertNotificationNotClosed(attachedNotification1);
        AssertNotificationNotShown(attachedNotification2);
    }

    [TestMethod]
    public void ShowNotification_WithSameId_WhenOncePerSessionDisabled_IsShownEverytime()
    {
        const string notificationId = "some id";
        const string anotherNotificationId = "some other id";

        var attachedNotification1 = ShowNotification(id: notificationId, oncePerSession: false);
        var attachedNotification2 = ShowNotification(id: anotherNotificationId, oncePerSession: false);
        var attachedNotification3 = ShowNotification(id: notificationId, oncePerSession: false);

        AssertNotificationWasShown(attachedNotification1);
        AssertNotificationWasShown(attachedNotification2);
        AssertNotificationWasShown(attachedNotification3);
    }

    [TestMethod]
    public void ShowNotification_ButtonClicked_WhenUnknownButton_NoException()
    {
        var callback = Substitute.For<Action>();
        var attachedNotification = ShowNotification(actions: new NotificationAction("notification1", _ => callback(), false));

        attachedNotification.InfoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("unknown notification"));

        callback.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void ShowNotification_ButtonClicked_WhenCriticalException_ExceptionNotCaught()
    {
        var callback = Substitute.For<Action>();
        callback.When(x => x.Invoke()).Throw(new StackOverflowException("critical exception"));
        var attachedNotification = ShowNotification(actions: new NotificationAction("notification1", _ => callback(), false));

        var act = () => attachedNotification.InfoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("notification1"));

        act.Should().ThrowExactly<StackOverflowException>().WithMessage("critical exception");
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void ShowNotification_ButtonClicked_WhenNonCriticalException_ExceptionCaught()
    {
        var callback = Substitute.For<Action<string>>();
        callback.WhenForAnyArgs(x => x.Invoke(Arg.Any<string>())).Throw(new NotImplementedException("non critical exception"));
        var attachedNotification = ShowNotification(actions: new NotificationAction("notification1", _ => callback("action1"), false));

        attachedNotification.InfoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("notification1"));

        logger.AssertPartialOutputStringExists(string.Format(CoreStrings.Notifications_FailedToExecuteAction, "System.NotImplementedException: non critical exception"));
    }

    [TestMethod]
    public void ShowNotification_InfoBarButtonClicked_WhenShouldDismissAfterAction_ClosesNotification()
    {
        var attachedNotification = ShowNotification(actions: new NotificationAction("notification1", _ => { }, true));

        attachedNotification.InfoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("notification1"));

        AssertNotificationClosed(attachedNotification);
    }

    [TestMethod]
    public void ShowNotification_InfoBarButtonClicked_WhenDontDismissAfterAction_NotificationRemains()
    {
        var attachedNotification = ShowNotification(actions: new NotificationAction("notification1", _ => { }, false));

        attachedNotification.InfoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("notification1"));

        AssertNotificationNotClosed(attachedNotification);
    }

    [TestMethod]
    public void ShowNotification_InfoBarButtonClicked_CorrectActionInvoked()
    {
        var callback = Substitute.For<Action<string>>();
        var infoBar = ShowNotification(actions:
            [
                new NotificationAction("notification1", _ => callback("action1"), false),
                new NotificationAction("notification2", _ => callback("action2"), false),
                new NotificationAction("notification3", _ => callback("action3"), false)
            ])
            .InfoBar;

        infoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("notification2"));
        callback.Received(1).Invoke("action2");

        infoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("notification3"));
        callback.Received(1).Invoke("action3");

        callback.ReceivedCalls().Count().Should().Be(2);
    }

    [TestMethod]
    public void ShowNotification_ClosesPreviousNotification()
    {
        var attachedNotification1 = ShowNotification();
        var attachedNotification2 = ShowNotification();

        AssertNotificationClosed(attachedNotification1);
        AssertNotificationWasShown(attachedNotification2);
    }

    [TestMethod]
    public void ShowNotification_WithToolWindowGuid_ShowOnToolWindow()
    {
        var aToolWindowId = Guid.NewGuid();
        var notification = CreateNotification();
        var attachedNotification = AttachNotification(notification, aToolWindowId);

        testSubject.ShowNotification(notification, aToolWindowId);

        AssertNotificationWasShown(attachedNotification, aToolWindowId);
    }

    [TestMethod]
    public void ActiveSolutionChangedEvent_WhenNoActiveNotification_NotificationNotClosed()
    {
        activeSolutionTracker.ActiveSolutionChanged += Raise.Event<EventHandler<ActiveSolutionChangedEventArgs>>(this, new ActiveSolutionChangedEventArgs(false, default));

        infoBarManager.DidNotReceive().CloseInfoBar(Arg.Any<IInfoBar>());
    }

    [TestMethod]
    public void ActiveSolutionChangedEvent_WhenSolutionClosedAndCloseOnSolutionClose_NotificationCloses()
    {
        var attachedNotification = ShowNotification(closeOnSolutionClose: true);

        activeSolutionTracker.ActiveSolutionChanged += Raise.Event<EventHandler<ActiveSolutionChangedEventArgs>>(this, new ActiveSolutionChangedEventArgs(false, default));

        AssertNotificationClosed(attachedNotification);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void ActiveSolutionChangedEvent_WhenSolutionOpenedOrNotCloseOnSolutionClose_NotificationDoesNotClose(bool isSolutionOpen, bool closeOnSolutionClose)
    {
        var attachedNotification = ShowNotification(closeOnSolutionClose: closeOnSolutionClose);

        activeSolutionTracker.ActiveSolutionChanged += Raise.Event<EventHandler<ActiveSolutionChangedEventArgs>>(this, new ActiveSolutionChangedEventArgs(isSolutionOpen, default));

        AssertNotificationNotClosed(attachedNotification);
    }

    [TestMethod]
    public void Dispose_ClosesNotification()
    {
        var attachedNotification = ShowNotification();

        testSubject.Dispose();

        AssertNotificationClosed(attachedNotification);
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromActiveSolutionChangeEvents()
    {
        testSubject.Dispose();

        activeSolutionTracker.Received(1).ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
    }

    [TestMethod]
    public void ShowNotification_WhenNonCriticalException_ExceptionCaught()
    {
        infoBarManager.When(x => x.AttachInfoBarToMainWindow(Arg.Any<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, Arg.Any<string[]>()))
            .Do(_ => throw new NotImplementedException("this is a test"));

        Action act = () => ShowNotification();

        act.Should().NotThrow();

        logger.AssertPartialOutputStringExists("this is a test");
    }

    [TestMethod]
    public void ShowNotification_WhenCriticalException_ExceptionNotCaught()
    {
        infoBarManager.When(x => x.AttachInfoBarToMainWindow(Arg.Any<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, Arg.Any<string[]>()))
            .Do(_ => throw new StackOverflowException("this is a test"));

        var act = () => ShowNotification();

        act.Should().ThrowExactly<StackOverflowException>().WithMessage("this is a test");

        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void CloseNotification_ClosesCurrentNotification_AndUnsubscribesFromInfoBarEvents()
    {
        var attachedNotification = ShowNotification();

        testSubject.CloseNotification();

        AssertNotificationClosed(attachedNotification);
    }

    [TestMethod]
    public void CloseNotification_CurrentNotificationIsNull_DoesNotThrow()
    {
        var act = () => testSubject.CloseNotification();

        act.Should().NotThrow();
        infoBarManager.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void CloseNotification_ClosesInfoBar_RunsOnUIThread()
    {
        var notification = CreateNotification();
        var infoBar = AttachNotification(notification).InfoBar;

        // Prevent the ThreadHandling action to run immediately and capture it for later execution
        Action runThreadAction = null;
        var captureParamThreadHandling = Substitute.For<IThreadHandling>();
        captureParamThreadHandling.RunOnUIThreadAsync(Arg.Do<Action>(arg => runThreadAction = arg));
        var threadHandledTestSubject = new NotificationService(infoBarManager, disabledNotificationsStorage, activeSolutionTracker, captureParamThreadHandling, logger);

        // Show a notification to be able to close it
        threadHandledTestSubject.ShowNotification(notification);
        runThreadAction();

        // Trigger the CloseNotification without running the action to validate that the UI action is the one responsible for closing the notification
        threadHandledTestSubject.CloseNotification();
        infoBarManager.DidNotReceiveWithAnyArgs().CloseInfoBar(infoBar);

        // Run the action captured by the ThreadHandling to validate that the InfoBar is shown
        runThreadAction();
        infoBarManager.ReceivedWithAnyArgs(1).CloseInfoBar(infoBar);
        captureParamThreadHandling.ReceivedWithAnyArgs(2).RunOnUIThreadAsync(Arg.Any<Action>());
    }

    [TestMethod]
    public void CloseNotification_WhenNonCriticalException_ExceptionCaught()
    {
        var infoBar = ShowNotification().InfoBar;
        infoBarManager.WhenForAnyArgs(x => x.CloseInfoBar(infoBar))
            .Do(_ => throw new NotImplementedException("this is a test"));

        var act = () => testSubject.CloseNotification();

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("this is a test");
    }

    [TestMethod]
    public void CloseNotification_WhenCriticalException_ExceptionNotCaught()
    {
        var infoBar = ShowNotification().InfoBar;
        infoBarManager.When(x => x.CloseInfoBar(infoBar))
            .Do(_ => throw new StackOverflowException("this is a test"));

        var act = () => testSubject.CloseNotification();

        act.Should().ThrowExactly<StackOverflowException>().WithMessage("this is a test");
        logger.AssertNoOutputMessages();
    }

    private void AllowRunningUiThreadActions()
    {
        threadHandling.RunOnUIThreadAsync(Arg.Do<Action>(arg => arg()));
    }

    private AttachedNotification ShowNotification(string id = null, bool oncePerSession = true, bool disabled = false, bool closeOnSolutionClose = true, params INotificationAction[] actions)
    {
        var notification = CreateNotification(id, oncePerSession, disabled, closeOnSolutionClose, actions);
        var attachedNotification = AttachNotification(notification);
        testSubject.ShowNotification(notification);
        return attachedNotification;
    }

    private INotification CreateNotification(string id = null, bool oncePerSession = true, bool disabled = false, bool closeOnSolutionClose = true, params INotificationAction[] actions)
    {
        var notificationId = id ?? Guid.NewGuid().ToString();
        var notification = new Notification(notificationId, notificationId, actions, oncePerSession, closeOnSolutionClose);

        disabledNotificationsStorage.IsNotificationDisabled(notificationId).Returns(disabled);

        return notification;
    }

    private AttachedNotification AttachNotification(INotification notification)
    {
        var infoBar = Substitute.For<IInfoBar>();

        infoBarManager.AttachInfoBarToMainWindow(
                notification.Message,
                Arg.Any<SonarLintImageMoniker>(),
                Arg.Any<string[]>())
            .Returns(infoBar);

        infoBarManager.When(x => x.CloseInfoBar(infoBar))
            .Do(_ => infoBar.Closed += Raise.Event());

        return new AttachedNotification(notification, infoBar);
    }

    private AttachedNotification AttachNotification(INotification notification, Guid toolWindowId)
    {
        var infoBar = Substitute.For<IInfoBar>();

        infoBarManager.AttachInfoBarWithButtons(
                toolWindowId,
                notification.Message,
                Arg.Any<string[]>(),
                SonarLintImageMoniker.OfficialSonarLintMoniker)
            .Returns(infoBar);

        infoBarManager.When(x => x.CloseInfoBar(infoBar))
            .Do(_ => infoBar.Closed += Raise.Event());

        return new AttachedNotification(notification, infoBar);
    }

    private void AssertNotificationWasShown(AttachedNotification attachedNotification)
    {
        infoBarManager.ReceivedWithAnyArgs().AttachInfoBarToMainWindow(
            attachedNotification.Notification.Message,
            SonarLintImageMoniker.OfficialSonarLintMoniker,
            Arg.Any<string[]>());

        AssertNotificationContainsActionButtons(attachedNotification.Notification);
    }

    private void AssertNotificationWasShown(AttachedNotification attachedNotification, Guid toolWindowId)
    {
        infoBarManager.ReceivedWithAnyArgs().AttachInfoBarWithButtons(
            toolWindowId,
            attachedNotification.Notification.Message,
            Arg.Any<string[]>(),
            SonarLintImageMoniker.OfficialSonarLintMoniker);

        AssertNotificationContainsActionButtons(attachedNotification.Notification);
    }

    private void AssertNotificationContainsActionButtons(INotification notification)
    {
        var expectedButtonTexts = notification.Actions.Select(x => x.CommandText).ToArray();
        var actualButtonTexts = infoBarManager.ReceivedCalls().Last().GetArguments()[2] as IEnumerable<string>;
        actualButtonTexts.Should().BeEquivalentTo(expectedButtonTexts);
    }

    private void AssertNotificationClosed(AttachedNotification attachedNotification)
    {
        AssertUnsubscribedFromInfoBarEvents(attachedNotification);
        infoBarManager.Received(1).CloseInfoBar(attachedNotification.InfoBar);
    }

    private void AssertNotificationNotClosed(AttachedNotification attachedNotification)
    {
        infoBarManager.DidNotReceive().CloseInfoBar(attachedNotification.InfoBar);
    }

    private static void AssertNotificationNotShown(AttachedNotification attachedNotification)
    {
        attachedNotification.InfoBar.ReceivedCalls().Should().BeEmpty();
    }

    private static void AssertSubscribedToInfoBarEvents(AttachedNotification attachedNotification)
    {
        attachedNotification.InfoBar.ReceivedWithAnyArgs(1).Closed += Arg.Any<EventHandler>();
        attachedNotification.InfoBar.ReceivedWithAnyArgs(1).ButtonClick += Arg.Any<EventHandler<InfoBarButtonClickedEventArgs>>();
    }

    private static void AssertUnsubscribedFromInfoBarEvents(AttachedNotification attachedNotification)
    {
        attachedNotification.InfoBar.ReceivedWithAnyArgs(1).Closed -= Arg.Any<EventHandler>();
        attachedNotification.InfoBar.ReceivedWithAnyArgs(1).ButtonClick -= Arg.Any<EventHandler<InfoBarButtonClickedEventArgs>>();
    }

    private record AttachedNotification(INotification Notification, IInfoBar InfoBar);
}

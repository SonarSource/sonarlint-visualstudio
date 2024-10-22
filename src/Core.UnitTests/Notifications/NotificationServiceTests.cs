/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
    private IThreadHandling threadHandling;
    private TestLogger logger;
    private NotificationService testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        infoBarManager = Substitute.For<IInfoBarManager>();
        disabledNotificationsStorage = Substitute.For<IDisabledNotificationsStorage>();
        threadHandling = Substitute.For<IThreadHandling>();
        logger = new TestLogger();
        testSubject = new NotificationService(infoBarManager, disabledNotificationsStorage, threadHandling, logger);
        
        AllowRunningUiThreadActions();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<NotificationService, INotificationService>(
            MefTestHelpers.CreateExport<IInfoBarManager>(),
            MefTestHelpers.CreateExport<IDisabledNotificationsStorage>(),
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
        ShowNotification(id: "some id", disabled: true);

        AssertNoNotificationWasShown();
    }

    [TestMethod]
    public void ShowNotification_WhenNewNotificationIsDisabled_DoesNotClosePrevious()
    {
        var infoBar1 = ShowNotification(id: "some id1");
        var infoBar2 = ShowNotification(id: "some id2", disabled: true);
        
        AssertNotificationNotClosed(infoBar1);
        infoBar2.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void ShowNotification_SubscribesToInfoBarEvents()
    {
        var infoBar = ShowNotification();
        
        AssertSubscribedToInfoBarEvents(infoBar);
    }
    
    [TestMethod]
    public void ShowNotification_AttachesInfoBarToWindow_RunsOnUIThread()
    {
        var notification = CreateNotification();
        AttachNotification(notification, out _);
        
        // Prevent the ThreadHandling action to run immediately and capture it for later execution
        Action showInfoBar = null;
        var captureParamThreadHandling = Substitute.For<IThreadHandling>();
        captureParamThreadHandling.RunOnUIThreadAsync(Arg.Do<Action>(arg => showInfoBar = arg));
        var threadHandledTestSubject = new NotificationService(infoBarManager, disabledNotificationsStorage, captureParamThreadHandling, logger);
        
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
        ShowNotification(out var notification, actions:
        [
            new NotificationAction("button1", _ => { }, false),
            new NotificationAction("button2", _ => { }, false),
            new NotificationAction("button3", _ => { }, false)
        ]);

        AssertNotificationWasShown(notification);
    }

    [TestMethod]
    public void ShowNotification_WithSameId_OnceWhenPerSessionEnabled_IsShownOnlyOnce()
    {
        const string notificationId = "some id";
        const string anotherNotificationId = "some other id";

        ShowNotification(id: notificationId, oncePerSession: true);
        var infoBar2 = ShowNotification(id: anotherNotificationId, oncePerSession: true);
        var infoBar3 = ShowNotification(id: notificationId, oncePerSession: true);
        
        AssertNotificationNotClosed(infoBar2);
        infoBar3.ReceivedCalls().Count().Should().Be(0);
    }

    [TestMethod]
    public void ShowNotification_WithSameId_WhenOncePerSessionDisabled_IsShownEverytime()
    {
        const string notificationId = "some id";
        const string anotherNotificationId = "some other id";
        
        ShowNotification(id: notificationId, oncePerSession: false);
        ShowNotification(id: anotherNotificationId, oncePerSession: false);
        ShowNotification(out var notification3, id: notificationId, oncePerSession: false);
        
        AssertNotificationWasShown(notification3);
    }
    
    [TestMethod]
    public void ShowNotification_WhenUnknownInfoBarButtonClicked_NoException()
    {
        var callback = Substitute.For<Action<string>>();
        var infoBar = ShowNotification(actions: new NotificationAction("notification1", _ => callback("action1"), false));
        
        infoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("unknown notification"));
        
        callback.ReceivedCalls().Should().BeEmpty();
    }
    
    [DataRow(false, 0)]
    [DataRow(true, 1)]
    [TestMethod]
    public void ShowNotification_InfoBarButtonClicked_ActionInvokedWithTheNotification(bool shouldDismissNotificationAfterAction, int dismissInvocationCount)
    {
        var callback = Substitute.For<Action<INotification>>();
        var infoBar = ShowNotification(out var notification, actions: new NotificationAction("notification1", x => callback(x), shouldDismissNotificationAfterAction));
    
        infoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("notification1"));

        callback.Received(1).Invoke(notification);
        infoBarManager.Received(dismissInvocationCount).CloseInfoBar(infoBar);
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
        ]);
    
        infoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("notification2"));
        callback.Received(1).Invoke("action2");
    
        infoBar.ButtonClick += Raise.Event<EventHandler<InfoBarButtonClickedEventArgs>>(this, new InfoBarButtonClickedEventArgs("notification3"));
        callback.Received(1).Invoke("action3");
        
        callback.ReceivedCalls().Count().Should().Be(2);
    }
    
    [TestMethod]
    public void ShowNotification_ClosesPreviousNotification()
    {
        var infoBar1 = ShowNotification();
    
        ShowNotification(out var notification2);
        
        AssertNotificationClosed(infoBar1);
        AssertNotificationWasShown(notification2);
    }
        
    [TestMethod]
    public void ShowNotification_WithToolWindowGuid_ShowOnToolWindow()
    {
        var aToolWindowId = Guid.NewGuid();
        var notification = CreateNotification();
        AttachNotification(notification, aToolWindowId);
    
        testSubject.ShowNotification(notification, aToolWindowId);
    
        AssertNotificationWasShown(notification, aToolWindowId);
    }
    
    [TestMethod]
    public void Dispose_ClosesNotification()
    {
        var infoBar = ShowNotification();

        testSubject.Dispose();
    
        AssertNotificationClosed(infoBar);
    }

    [TestMethod]
    public void ShowNotification_WhenNonCriticalException_ExceptionCaught()
    {
        var notification = CreateNotification();
        AttachNotification(notification, out _);
        infoBarManager.When(x => x.AttachInfoBarToMainWindow(Arg.Any<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, Arg.Any<string[]>()))
            .Do(_ => throw new NotImplementedException("this is a test"));
        
        Action act = () => testSubject.ShowNotification(notification);
    
        act.Should().NotThrow();
    
        logger.AssertPartialOutputStringExists("this is a test");
    }
    
    [TestMethod]
    public void ShowNotification_WhenCriticalException_ExceptionNotCaught()
    {
        var notification = CreateNotification();
        AttachNotification(notification, out _);
        infoBarManager.When(x => x.AttachInfoBarToMainWindow(Arg.Any<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, Arg.Any<string[]>()))
            .Do(_ => throw new StackOverflowException("this is a test"));
        
        Action act = () => testSubject.ShowNotification(notification);
    
        act.Should().ThrowExactly<StackOverflowException>().WithMessage("this is a test");
    
        logger.AssertNoOutputMessages();
    }
    
    [TestMethod]
    public void CloseNotification_ClosesCurrentNotification_AndUnsubscribesFromInfoBarEvents()
    {
        var infoBar = ShowNotification();
    
        testSubject.CloseNotification();
    
        AssertNotificationClosed(infoBar);
    }
    
    [TestMethod]
    public void CloseNotification_CurrentNotificationIsNull_DoesNotThrow()
    {
        Action act = () => testSubject.CloseNotification();
    
        act.Should().NotThrow();
        infoBarManager.ReceivedCalls().Should().BeEmpty();
    }
    
    [TestMethod]
    public void CloseNotification_ClosesInfoBar_RunsOnUIThread()
    {
        var notification = CreateNotification();
        AttachNotification(notification, out var infoBar);
        
        // Prevent the ThreadHandling action to run immediately and capture it for later execution
        Action runThreadAction = null;
        var captureParamThreadHandling = Substitute.For<IThreadHandling>();
        captureParamThreadHandling.RunOnUIThreadAsync(Arg.Do<Action>(arg => runThreadAction = arg));
        var threadHandledTestSubject = new NotificationService(infoBarManager, disabledNotificationsStorage, captureParamThreadHandling, logger);
        
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
        var notification = CreateNotification();
        AttachNotification(notification, out var infoBar);
        infoBarManager.When(x => x.CloseInfoBar(infoBar))
            .Do(_ => throw new NotImplementedException("this is a test"));
    
        testSubject.ShowNotification(notification);
        Action act = () => testSubject.CloseNotification();
        
        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("this is a test");
    }
    
    [TestMethod]
    public void CloseNotification_WhenCriticalException_ExceptionNotCaught()
    {
        var notification = CreateNotification();
        AttachNotification(notification, out var infoBar);
        infoBarManager.When(x => x.CloseInfoBar(infoBar))
            .Do(_ => throw new StackOverflowException("this is a test"));
    
        testSubject.ShowNotification(notification);
    
        Action act = () => testSubject.CloseNotification();
        act.Should().ThrowExactly<StackOverflowException>().WithMessage("this is a test");
    
        logger.AssertNoOutputMessages();
    }
    
    private void AllowRunningUiThreadActions()
    {
        threadHandling.RunOnUIThreadAsync(Arg.Do<Action>(arg => arg()));
    }
    
    private IInfoBar ShowNotification(string id = null, bool oncePerSession = true, bool disabled = false, params INotificationAction[] actions)
    {
        return ShowNotification(out _, id, oncePerSession, disabled, actions);
    }
    
    private IInfoBar ShowNotification(out INotification notification, string id = null, bool oncePerSession = true, bool disabled = false, params INotificationAction[] actions)
    {
        notification = CreateNotification(id, oncePerSession, disabled, actions);
        AttachNotification(notification, out var infoBar);
        testSubject.ShowNotification(notification);
        return infoBar;
    }

    private INotification CreateNotification(string id = null, bool oncePerSession = true, bool disabled = false, params INotificationAction[] actions)
    {
        var notificationId = id ?? Guid.NewGuid().ToString();
        var notification = new Notification(notificationId, notificationId, actions, oncePerSession);
        
        disabledNotificationsStorage.IsNotificationDisabled(notificationId).Returns(disabled);
        
        return notification;
    }

    private void AttachNotification(INotification notification, out IInfoBar infoBar)
    {
        infoBar = Substitute.For<IInfoBar>();
        
        infoBarManager.AttachInfoBarToMainWindow(
                notification.Message,
                Arg.Any<SonarLintImageMoniker>(),
                Arg.Any<string[]>())
            .Returns(infoBar);

        infoBarManager.When(x => x.CloseInfoBar(Arg.Any<IInfoBar>()))
            .Do(x => ((IInfoBar)x.Args()[0]).Closed += Raise.Event());
    }

    private void AttachNotification(INotification notification, Guid toolWindowId)
    {
        var infoBar = Substitute.For<IInfoBar>();
        
        infoBarManager.AttachInfoBarWithButtons(
                toolWindowId,
                notification.Message,
                Arg.Any<string[]>(),
                SonarLintImageMoniker.OfficialSonarLintMoniker)
            .Returns(infoBar);

        infoBarManager.When(x => x.CloseInfoBar(Arg.Any<IInfoBar>()))
            .Do(x => ((IInfoBar)x.Args()[0]).Closed += Raise.Event());
    }

    private void AssertNotificationWasShown(INotification notification)
    {
        infoBarManager.ReceivedWithAnyArgs().AttachInfoBarToMainWindow(
            notification.Message,
            SonarLintImageMoniker.OfficialSonarLintMoniker,
            Arg.Any<string[]>());

        AssertNotificationContainsActionButtons(notification);
    }

    private void AssertNotificationWasShown(INotification notification, Guid toolWindowId)
    {
        infoBarManager.ReceivedWithAnyArgs().AttachInfoBarWithButtons(
            toolWindowId,
            notification.Message,
            Arg.Any<string[]>(),
            SonarLintImageMoniker.OfficialSonarLintMoniker);

        AssertNotificationContainsActionButtons(notification);
    }

    private void AssertNotificationContainsActionButtons(INotification notification)
    {
        var expectedButtonTexts = notification.Actions.Select(x => x.CommandText).ToArray();
        var actualButtonTexts = infoBarManager.ReceivedCalls().Last().GetArguments()[2] as IEnumerable<string>;
        actualButtonTexts.Should().BeEquivalentTo(expectedButtonTexts);
    }
    
    private void AssertNoNotificationWasShown()
    {
        infoBarManager.ReceivedCalls().Should().BeEmpty();
        threadHandling.DidNotReceive().RunOnUIThreadAsync(Arg.Any<Action>());
    }

    private void AssertNotificationClosed(IInfoBar infoBar)
    {
        AssertUnsubscribedFromInfoBarEvents(infoBar);
        infoBarManager.Received(1).CloseInfoBar(infoBar);
    }
    
    private void AssertNotificationNotClosed(IInfoBar infoBar)
    {
        infoBarManager.DidNotReceive().CloseInfoBar(infoBar);
    }
    
    private static void AssertSubscribedToInfoBarEvents(IInfoBar infoBar)
    {
        infoBar.ReceivedWithAnyArgs(1).Closed += Arg.Any<EventHandler>();
        infoBar.ReceivedWithAnyArgs(1).ButtonClick += Arg.Any<EventHandler<InfoBarButtonClickedEventArgs>>();
    }

    private static void AssertUnsubscribedFromInfoBarEvents(IInfoBar infoBar)
    {
        infoBar.ReceivedWithAnyArgs(1).Closed -= Arg.Any<EventHandler>();
        infoBar.ReceivedWithAnyArgs(1).ButtonClick -= Arg.Any<EventHandler<InfoBarButtonClickedEventArgs>>();
    }
}

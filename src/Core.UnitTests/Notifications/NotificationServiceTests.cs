/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Notifications
{
    [TestClass]
    public class NotificationServiceTests
    {
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
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.ShowNotification(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("notification");
        }

        [TestMethod]
        public void ShowNotification_ShouldAddInfoBarOnUiThread()
        {
            var notification = CreateNotification();
            var infoBar = CreateInfoBar();
            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);

            var threadHandling = new Mock<IThreadHandling>();
            Action runOnUiAction = null;
            threadHandling
                .Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback((Action callbackAction) => runOnUiAction = callbackAction);

            var testSubject = CreateTestSubject(infoBarManager.Object, threadHandling: threadHandling.Object);
            testSubject.ShowNotification(notification);

            infoBarManager.Invocations.Count.Should().Be(0);
            threadHandling.Verify(x => x.RunOnUIThread(It.IsAny<Action>()), Times.Once);

            runOnUiAction.Should().NotBeNull();
            runOnUiAction();

            VerifySubscribedToInfoBarEvents(infoBar);
            VerifyInfoBarCreatedCorrectly(infoBarManager, notification);

            infoBarManager.VerifyNoOtherCalls();
            threadHandling.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_InfoBarCreatedCorrectly()
        {
            var notification = CreateNotification(actions: new INotificationAction[]
            {
                new NotificationAction("notification1", _ => { }, false),
                new NotificationAction("notification2", _ => { }, false),
                new NotificationAction("notification2", _ => { }, false)
            });

            var infoBar = CreateInfoBar();
            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);

            var testSubject = CreateTestSubject(infoBarManager.Object);
            testSubject.ShowNotification(notification);

            VerifyInfoBarCreatedCorrectly(infoBarManager, notification);
            VerifySubscribedToInfoBarEvents(infoBar);

            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_NotificationIsDisabled_NotificationNotShown()
        {
            var notification = CreateNotification(id: "some id");
            var infoBar = CreateInfoBar();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);

            var disabledNotificationsStorage = new Mock<IDisabledNotificationsStorage>();
            disabledNotificationsStorage.Setup(x => x.IsNotificationDisabled("some id")).Returns(true);

            var testSubject = CreateTestSubject(infoBarManager.Object, disabledNotificationsStorage.Object);

            testSubject.ShowNotification(notification);

            disabledNotificationsStorage.Verify(x=> x.IsNotificationDisabled("some id"), Times.Once);
            disabledNotificationsStorage.VerifyNoOtherCalls();

            infoBarManager.Invocations.Count.Should().Be(0);
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_NotificationIsDisabled_PreviousNotificationNotRemoved()
        {
            var notification1 = CreateNotification(id: "some id1");
            var notification2 = CreateNotification(id: "some id2");

            var infoBar1 = CreateInfoBar();
            var infoBar2 = CreateInfoBar();

            var infoBarManager = new Mock<IInfoBarManager>();
            SetupInfoBarManager(infoBarManager, notification1, infoBar1.Object);
            SetupInfoBarManager(infoBarManager, notification2, infoBar2.Object);

            var disabledNotificationsStorage = new Mock<IDisabledNotificationsStorage>();
            disabledNotificationsStorage.Setup(x => x.IsNotificationDisabled("some id1")).Returns(false);
            disabledNotificationsStorage.Setup(x => x.IsNotificationDisabled("some id2")).Returns(true);

            var testSubject = CreateTestSubject(infoBarManager.Object, disabledNotificationsStorage.Object);

            testSubject.ShowNotification(notification1);

            disabledNotificationsStorage.Verify(x => x.IsNotificationDisabled("some id1"), Times.Once);
            disabledNotificationsStorage.VerifyNoOtherCalls();

            VerifyInfoBarCreatedCorrectly(infoBarManager, notification1);
            VerifySubscribedToInfoBarEvents(infoBar1);
            infoBarManager.Invocations.Count.Should().Be(1);

            testSubject.ShowNotification(notification2);

            disabledNotificationsStorage.Verify(x => x.IsNotificationDisabled("some id2"), Times.Once);
            disabledNotificationsStorage.VerifyNoOtherCalls();

            infoBarManager.Invocations.Count.Should().Be(1);
            infoBar2.Invocations.Count.Should().Be(0);
            infoBar1.VerifyNoOtherCalls();
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_NotificationWithSameIdAlreadyShown_InfoBarIsUnchanged()
        {
            var notification1 = CreateNotification(id: "some id");
            var notification2 = CreateNotification(id: "some other id");
            var notification3 = CreateNotification(id: "some id");

            var infoBar1 = CreateInfoBar();
            var infoBar2 = CreateInfoBar();
            var infoBar3 = CreateInfoBar();

            var infoBarManager = new Mock<IInfoBarManager>();
            SetupInfoBarManager(infoBarManager, notification1, infoBar1.Object);
            SetupInfoBarManager(infoBarManager, notification2, infoBar2.Object);
            SetupInfoBarManager(infoBarManager, notification3, infoBar3.Object);

            var testSubject = CreateTestSubject(infoBarManager.Object);

            testSubject.ShowNotification(notification1);

            VerifyInfoBarCreatedCorrectly(infoBarManager, notification1);
            VerifySubscribedToInfoBarEvents(infoBar1);

            testSubject.ShowNotification(notification2);

            VerifyInfoBarRemoved(infoBarManager, infoBar1);
            VerifyUnsubscribedFromInfoBarEvents(infoBar1);
            VerifyInfoBarCreatedCorrectly(infoBarManager, notification2);
            VerifySubscribedToInfoBarEvents(infoBar2);

            testSubject.ShowNotification(notification3);

            infoBar3.Invocations.Count.Should().Be(0);
            infoBar2.VerifyNoOtherCalls();
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_UnknownInfoBarButtonClicked_NoException()
        {
            var callback = new Mock<Action<string>>();

            var notification = CreateNotification(actions: 
                new NotificationAction("notification1", _ => callback.Object("action1"), false));

            var infoBar = new Mock<IInfoBar>();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);

            var testSubject = CreateTestSubject(infoBarManager.Object);
            testSubject.ShowNotification(notification);

            callback.Invocations.Count.Should().Be(0);

            infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs("unknown notification"));

            callback.Invocations.Count.Should().Be(0);            
        }

        [DataRow(false, 0)]
        [DataRow(true, 1)]
        [TestMethod]
        public void ShowNotification_InfoBarButtonClicked_ActionInvokedWithTheNotification(bool shouldDismissNotificationAfterAction, int dismissInvocationCount)
        {
            var callback = new Mock<Action<INotification>>();

            var notification = CreateNotification(actions: new INotificationAction[]
            {
                new NotificationAction("notification1", notification => callback.Object(notification), shouldDismissNotificationAfterAction)
            });

            var infoBar = new Mock<IInfoBar>();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);

            var testSubject = CreateTestSubject(infoBarManager.Object);
            testSubject.ShowNotification(notification);

            callback.Invocations.Count.Should().Be(0);

            infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs("notification1"));

            callback.Verify(x => x(notification), Times.Once);
            callback.VerifyNoOtherCalls();
            infoBarManager.Verify(ib => ib.DetachInfoBar(infoBar.Object), Times.Exactly(dismissInvocationCount));
        }

        [TestMethod]
        public void ShowNotification_InfoBarButtonClicked_CorrectActionInvoked()
        {
            var callback = new Mock<Action<string>>();

            var notification = CreateNotification(actions: new INotificationAction[]
            {
                new NotificationAction("notification1", _ => callback.Object("action1"), false),
                new NotificationAction("notification2", _ => callback.Object("action2"), false),
                new NotificationAction("notification3", _ => callback.Object("action3"), false)
            });

            var infoBar = new Mock<IInfoBar>();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);

            var testSubject = CreateTestSubject(infoBarManager.Object);
            testSubject.ShowNotification(notification);

            callback.Invocations.Count.Should().Be(0);

            infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs("notification2"));

            callback.Verify(x=> x("action2"), Times.Once);
            callback.VerifyNoOtherCalls();

            infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs("notification3"));

            callback.Verify(x => x("action3"), Times.Once);
            callback.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_HasPreviousNotification_PreviousNotificationRemoved()
        {
            var notification1 = CreateNotification();
            var infoBar1 = CreateInfoBar();

            var notification2 = CreateNotification();
            var infoBar2 = CreateInfoBar();

            var infoBarManager = new Mock<IInfoBarManager>();
            SetupInfoBarManager(infoBarManager, notification1, infoBar1.Object);
            SetupInfoBarManager(infoBarManager, notification2, infoBar2.Object);

            var testSubject = CreateTestSubject(infoBarManager.Object);

            testSubject.ShowNotification(notification1);

            VerifyInfoBarCreatedCorrectly(infoBarManager, notification1);
            VerifySubscribedToInfoBarEvents(infoBar1);

            testSubject.ShowNotification(notification2);

            VerifyInfoBarRemoved(infoBarManager, infoBar1);
            VerifyInfoBarCreatedCorrectly(infoBarManager, notification2);
            VerifySubscribedToInfoBarEvents(infoBar2);
        }

        [TestMethod]
        public void ShowNotification_UserClosedTheNotification_NotificationRemoved()
        {
            var notification = CreateNotification();
            var infoBar = CreateInfoBar();
            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);
            var testSubject = CreateTestSubject(infoBarManager.Object);

            testSubject.ShowNotification(notification);

            VerifyInfoBarCreatedCorrectly(infoBarManager, notification);
            VerifySubscribedToInfoBarEvents(infoBar);

            infoBar.Raise(x => x.Closed += null, EventArgs.Empty);

            VerifyInfoBarRemoved(infoBarManager, infoBar);

            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_NoExistingNotification_NoException()
        {
            var infoBarManager = new Mock<IInfoBarManager>();
            var testSubject = CreateTestSubject(infoBarManager.Object);

            Action act = () => testSubject.Dispose();

            act.Should().NotThrow();

            infoBarManager.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Dispose_HasExistingNotification_ExistingNotificationRemoved()
        {
            var notification = CreateNotification();
            var infoBar = CreateInfoBar();
            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);
            var testSubject = CreateTestSubject(infoBarManager.Object);

            testSubject.ShowNotification(notification);

            VerifyInfoBarCreatedCorrectly(infoBarManager, notification);
            VerifySubscribedToInfoBarEvents(infoBar);

            testSubject.Dispose();

            VerifyInfoBarRemoved(infoBarManager, infoBar);

            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_NonCriticalException_ExceptionCaught()
        {
            var notification = CreateNotification();

            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarToMainWindow(It.IsAny<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, It.IsAny<string[]>()))
                .Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(infoBarManager.Object, logger: logger);
            Action act = () => testSubject.ShowNotification(notification);

            act.Should().NotThrow();
            
            infoBarManager.VerifyAll();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void ShowNotification_CriticalException_ExceptionNotCaught()
        {
            var notification = CreateNotification();

            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarToMainWindow(It.IsAny<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, It.IsAny<string[]>()))
                .Throws(new StackOverflowException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(infoBarManager.Object, logger: logger);
            Action act = () => testSubject.ShowNotification(notification);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("this is a test");

            infoBarManager.VerifyAll();

            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void ShowNotification_InfoBarButtonClicked_NonCriticalException_ExceptionCaught()
        {
            var notification = CreateNotification(actions:
                new NotificationAction("action", _ => throw new NotImplementedException("this is a test"), false)
            );

            var infoBar = new Mock<IInfoBar>();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(infoBarManager.Object, logger: logger);
            testSubject.ShowNotification(notification);

            Action act = () => infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs("action"));

            act.Should().NotThrow();
            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void ShowNotification_InfoBarButtonClicked_CriticalException_ExceptionNotCaught()
        {
            var notification = CreateNotification(actions:
                new NotificationAction("action", _ => throw new StackOverflowException("this is a test"), false)
            );

            var infoBar = new Mock<IInfoBar>();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(infoBarManager.Object, logger: logger);
            testSubject.ShowNotification(notification);

            Action act = () => infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs("action"));

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("this is a test");
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void ShowNotification_UserClosedTheNotification_NonCriticalException_ExceptionCaught()
        {
            var notification = CreateNotification();
            var infoBar = CreateInfoBar();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);
            infoBarManager
                .Setup(x => x.DetachInfoBar(infoBar.Object))
                .Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(infoBarManager.Object, logger: logger);
            testSubject.ShowNotification(notification);

            Action act = () => infoBar.Raise(x => x.Closed += null, EventArgs.Empty);
            act.Should().NotThrow();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void ShowNotification_UserClosedTheNotification_CriticalException_ExceptionNotCaught()
        {
            var notification = CreateNotification();
            var infoBar = CreateInfoBar();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);
            infoBarManager
                .Setup(x => x.DetachInfoBar(infoBar.Object))
                .Throws(new StackOverflowException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(infoBarManager.Object, logger: logger);
            testSubject.ShowNotification(notification);

            Action act = () => infoBar.Raise(x => x.Closed += null, EventArgs.Empty);
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("this is a test");

            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Dispose_NonCriticalException_ExceptionCaught()
        {
            var notification = CreateNotification();
            var infoBar = CreateInfoBar();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);
            infoBarManager
                .Setup(x => x.DetachInfoBar(infoBar.Object))
                .Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(infoBarManager.Object, logger: logger);
            testSubject.ShowNotification(notification);

            Action act = () => testSubject.Dispose();
            act.Should().NotThrow();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void Dispose_CriticalException_ExceptionNotCaught()
        {
            var notification = CreateNotification();
            var infoBar = CreateInfoBar();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);
            infoBarManager
                .Setup(x => x.DetachInfoBar(infoBar.Object))
                .Throws(new StackOverflowException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(infoBarManager.Object, logger: logger);
            testSubject.ShowNotification(notification);

            Action act = () => testSubject.Dispose();
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("this is a test");

            logger.AssertNoOutputMessages();
        }

        private static NotificationService CreateTestSubject(IInfoBarManager infoBarManager = null,
            IDisabledNotificationsStorage disabledNotificationsStorage = null,
            IThreadHandling threadHandling = null,
            ILogger logger = null)
        {
            infoBarManager ??= Mock.Of<IInfoBarManager>();
            disabledNotificationsStorage ??= Mock.Of<IDisabledNotificationsStorage>();
            threadHandling ??= new NoOpThreadHandler();
            logger ??= Mock.Of<ILogger>();

            return new NotificationService(infoBarManager, disabledNotificationsStorage, threadHandling, logger);
        }

        private static INotification CreateNotification(string id = null, params INotificationAction[] actions)
        {
            var notification = new Mock<INotification>();

            notification.SetupGet(x => x.Id).Returns(id ?? Guid.NewGuid().ToString());
            notification.SetupGet(x => x.Message).Returns(Guid.NewGuid().ToString);
            notification.SetupGet(x => x.Actions).Returns(actions);

            return notification.Object;
        }

        private static Mock<IInfoBar> CreateInfoBar()
        {
            var infoBar = new Mock<IInfoBar>();

            infoBar.SetupAdd(x => x.Closed += (_, _) => { });
            infoBar.SetupAdd(x => x.ButtonClick += (_, _) => { });

            infoBar.SetupRemove(x => x.Closed -= (_, _) => { });
            infoBar.SetupRemove(x => x.ButtonClick -= (_, _) => { });

            return infoBar;
        }

        private static Mock<IInfoBarManager> CreateInfoBarManager(INotification notification, IInfoBar infoBar)
        {
            var infoBarManager = new Mock<IInfoBarManager>();

            SetupInfoBarManager(infoBarManager, notification, infoBar);

            return infoBarManager;
        }

        private static void SetupInfoBarManager(Mock<IInfoBarManager> infoBarManager, INotification notification, IInfoBar infoBar)
        {
            infoBarManager
                .Setup(x => x.AttachInfoBarToMainWindow(
                    notification.Message,
                    SonarLintImageMoniker.OfficialSonarLintMoniker,
                    It.IsAny<string[]>()))
                .Returns(infoBar);
        }

        private static void VerifyInfoBarCreatedCorrectly(Mock<IInfoBarManager> infoBarManager, INotification notification)
        {
            infoBarManager.Verify(x => x.AttachInfoBarToMainWindow(
                    notification.Message,
                    SonarLintImageMoniker.OfficialSonarLintMoniker,
                    It.IsAny<string[]>()),
                Times.Once);

            var expectedButtonTexts = notification.Actions.Select(x => x.CommandText).ToArray();
            var actualButtonTexts = infoBarManager.Invocations.First().Arguments[2] as IEnumerable<string>;

            actualButtonTexts.Should().BeEquivalentTo(expectedButtonTexts);
        }

        private static void VerifyInfoBarRemoved(Mock<IInfoBarManager> infoBarManager, Mock<IInfoBar> infoBar)
        {
            VerifyUnsubscribedFromInfoBarEvents(infoBar);
            infoBarManager.Verify(x => x.DetachInfoBar(infoBar.Object));
        }

        private static void VerifyUnsubscribedFromInfoBarEvents(Mock<IInfoBar> infoBar)
        {
            infoBar.VerifyRemove(x => x.Closed -= It.IsAny<EventHandler>(), Times.Once);
            infoBar.VerifyRemove(x => x.ButtonClick -= It.IsAny<EventHandler<InfoBarButtonClickedEventArgs>>(), Times.Once);
        }

        private static void VerifySubscribedToInfoBarEvents(Mock<IInfoBar> infoBar)
        {
            infoBar.VerifyAdd(x => x.Closed += It.IsAny<EventHandler>(), Times.Once);
            infoBar.VerifyAdd(x => x.ButtonClick += It.IsAny<EventHandler<InfoBarButtonClickedEventArgs>>(), Times.Once);
        }
    }
}

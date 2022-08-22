/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Core.UnitTests.Notifications
{
    [TestClass]
    public class NotificationServiceTests
    {
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
            var infoBarManager = CreateInfoBarManager(notification, Mock.Of<IInfoBar>());

            var threadHandling = new Mock<IThreadHandling>();
            Action runOnUiAction = null;
            threadHandling
                .Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback((Action callbackAction) => runOnUiAction = callbackAction);

            var testSubject = CreateTestSubject(infoBarManager.Object, threadHandling.Object);
            testSubject.ShowNotification(notification);

            infoBarManager.Invocations.Count.Should().Be(0);
            threadHandling.Verify(x => x.RunOnUIThread(It.IsAny<Action>()), Times.Once);

            runOnUiAction.Should().NotBeNull();
            runOnUiAction();

            VerifyInfoBarCreatedCorrectly(infoBarManager, notification);

            infoBarManager.VerifyNoOtherCalls();
            threadHandling.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_InfoBarCreatedCorrectly()
        {
            var notification = CreateNotification(actions: new INotificationAction[]
            {
                new NotificationAction("notification1", () => { }),
                new NotificationAction("notification2", () => { }),
                new NotificationAction("notification2", () => { })
            });

            var infoBarManager = CreateInfoBarManager(notification, Mock.Of<IInfoBar>());

            var testSubject = CreateTestSubject(infoBarManager.Object);
            testSubject.ShowNotification(notification);

            VerifyInfoBarCreatedCorrectly(infoBarManager, notification);

            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_NotificationWithSameIdAlreadyShown_InfoBarIsUnchanged()
        {
            var notification1 = CreateNotification(id: "some id");
            var notification2 = CreateNotification(id: "some id");

            var infoBar1 = CreateInfoBar();
            var infoBar2 = CreateInfoBar();

            var infoBarManager = new Mock<IInfoBarManager>();
            SetupInfoBarManager(infoBarManager, notification1, infoBar1.Object);
            SetupInfoBarManager(infoBarManager, notification2, infoBar2.Object);

            var testSubject = CreateTestSubject(infoBarManager.Object);

            testSubject.ShowNotification(notification1);

            VerifyInfoBarCreatedCorrectly(infoBarManager, notification1);
            VerifySubscribedToInfoBarEvents(infoBar1);
            infoBarManager.Invocations.Count.Should().Be(1);

            testSubject.ShowNotification(notification2);

            infoBarManager.Invocations.Count.Should().Be(1);
            infoBar2.Invocations.Count.Should().Be(0);
            infoBar1.VerifyNoOtherCalls();
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_UnknownInfoBarButtonClicked_NoException()
        {
            var callback = new Mock<Action<string>>();

            var notification = CreateNotification(actions: 
                new NotificationAction("notification1", () => callback.Object("action1")));

            var infoBar = new Mock<IInfoBar>();

            var infoBarManager = CreateInfoBarManager(notification, infoBar.Object);

            var testSubject = CreateTestSubject(infoBarManager.Object);
            testSubject.ShowNotification(notification);

            callback.Invocations.Count.Should().Be(0);

            infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs("unknown notification"));

            callback.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void ShowNotification_InfoBarButtonClicked_CorrectActionInvoked()
        {
            var callback = new Mock<Action<string>>();

            var notification = CreateNotification(actions: new INotificationAction[]
            {
                new NotificationAction("notification1", () => callback.Object("action1")),
                new NotificationAction("notification2", () => callback.Object("action2")),
                new NotificationAction("notification3", () => callback.Object("action3"))
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

            VerifyInfoBarRemoved(infoBar1, infoBarManager);
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

            VerifyInfoBarRemoved(infoBar, infoBarManager);

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

            VerifyInfoBarRemoved(infoBar, infoBarManager);

            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowNotification_NonCriticalException_ExceptionCaught()
        {
            var notification = CreateNotification();

            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButtons(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), SonarLintImageMoniker.OfficialSonarLintMoniker))
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
                .Setup(x => x.AttachInfoBarWithButtons(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), SonarLintImageMoniker.OfficialSonarLintMoniker))
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
                new NotificationAction("action", () => throw new NotImplementedException("this is a test"))
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
                new NotificationAction("action", () => throw new StackOverflowException("this is a test"))
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
            IThreadHandling threadHandling = null,
            ILogger logger = null)
        {
            infoBarManager ??= Mock.Of<IInfoBarManager>();
            threadHandling ??= new NoOpThreadHandler();
            logger ??= Mock.Of<ILogger>();

            return new NotificationService(infoBarManager, threadHandling, logger);
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
                .Setup(x => x.AttachInfoBarWithButtons(
                    NotificationService.ErrorListToolWindowGuid,
                    notification.Message,
                    It.IsAny<IEnumerable<string>>(),
                    SonarLintImageMoniker.OfficialSonarLintMoniker))
                .Returns(infoBar);
        }

        private static void VerifyInfoBarCreatedCorrectly(Mock<IInfoBarManager> infoBarManager, INotification notification)
        {
            infoBarManager.Verify(x => x.AttachInfoBarWithButtons(
                    NotificationService.ErrorListToolWindowGuid,
                    notification.Message,
                    It.IsAny<IEnumerable<string>>(),
                    SonarLintImageMoniker.OfficialSonarLintMoniker),
                Times.Once);

            var expectedButtonTexts = notification.Actions.Select(x => x.CommandText).ToArray();
            var actualButtonTexts = infoBarManager.Invocations.First().Arguments[2] as IEnumerable<string>;

            actualButtonTexts.Should().BeEquivalentTo(expectedButtonTexts);
        }

        private static void VerifyInfoBarRemoved(Mock<IInfoBar> infoBar, Mock<IInfoBarManager> infoBarManager)
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

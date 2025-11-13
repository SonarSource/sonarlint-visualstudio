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
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Notification;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Notification
{
    [TestClass]
    public class SloopRestartFailedNotificationServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SloopRestartFailedNotificationService, ISloopRestartFailedNotificationService>(
                MefTestHelpers.CreateExport<INotificationService>(),
                MefTestHelpers.CreateExport<IOutputWindowService>(),
                MefTestHelpers.CreateExport<ISLCoreLocator>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SloopRestartFailedNotificationService>();
        }

        [TestMethod]
        public void Show_CreatesNotificationAndCallsShow()
        {
            INotification notification = null;

            var notificationService = Substitute.For<INotificationService>();
            notificationService.When(ns => ns.ShowNotification(Arg.Any<INotification>())).Do(n => notification = (INotification)n.Args()[0]);

            var testSubject = new SloopRestartFailedNotificationService(notificationService, Substitute.For<IOutputWindowService>(), Substitute.For<ISLCoreLocator>());

            testSubject.Show(Substitute.For<Action>());

            notificationService.Received(1).ShowNotification(notification);

            notification.Should().NotBeNull();
            notification.Id.Should().Be("sonarlint.sloop.restart.failed");
            notification.Message.Should().Be(SLCoreStrings.SloopRestartFailedNotificationService_GoldBarMessage);
            notification.CloseOnSolutionClose.Should().Be(false);
        }

        [TestMethod]
        public void Show_CreatesNotificationWithRestartAndShowLogsActions()
        {
            var restartAction = Substitute.For<Action>();
            INotification notification = null;

            var notificationService = Substitute.For<INotificationService>();
            var outputWindowService = Substitute.For<IOutputWindowService>();
            var locator = Substitute.For<ISLCoreLocator>();
            locator.IsCustomJreSet().Returns(true); // disables UseAutoDetected
            locator.IsGlobalJreAvailable().Returns(true);

            notificationService.When(ns => ns.ShowNotification(Arg.Any<INotification>())).Do(n => notification = (INotification)n.Args()[0]);

            var testSubject = new SloopRestartFailedNotificationService(notificationService, outputWindowService, locator);
            testSubject.Show(restartAction);

            notificationService.Received(1).ShowNotification(notification);
            notification.Should().NotBeNull();
            notification.Actions.Should().HaveCount(2);
            notification.Actions.Select(a => a.CommandText).Should().Contain([
                SLCoreStrings.SloopRestartFailedNotificationService_Restart,
                SLCoreStrings.Infobar_ShowLogs
            ]);

            // Restart action
            var restart = notification.Actions.First(a => a.CommandText == SLCoreStrings.SloopRestartFailedNotificationService_Restart);
            restart.Action(notification);
            restartAction.Received(1).Invoke();

            // Show logs action
            var showLogs = notification.Actions.First(a => a.CommandText == SLCoreStrings.Infobar_ShowLogs);
            showLogs.Action(notification);
            outputWindowService.Received(1).Show();
        }

        [TestMethod]
        public void Show_CreatesNotificationWithUseAutoDetectedAction_WhenApplicable()
        {
            var restartAction = Substitute.For<Action>();
            INotification notification = null;

            var notificationService = Substitute.For<INotificationService>();
            var outputWindowService = Substitute.For<IOutputWindowService>();
            var locator = Substitute.For<ISLCoreLocator>();
            locator.IsCustomJreSet().Returns(false); // enables UseAutoDetected
            locator.IsGlobalJreAvailable().Returns(true);

            notificationService.When(ns => ns.ShowNotification(Arg.Any<INotification>())).Do(n => notification = (INotification)n.Args()[0]);

            var testSubject = new SloopRestartFailedNotificationService(notificationService, outputWindowService, locator);
            testSubject.Show(restartAction);

            notificationService.Received(1).ShowNotification(notification);
            notification.Should().NotBeNull();
            notification.Actions.Select(a => a.CommandText).Should().Contain(SLCoreStrings.SloopRestartFailedNotificationService_UseAutoDetected);

            var useAutoDetected = notification.Actions.First(a => a.CommandText == SLCoreStrings.SloopRestartFailedNotificationService_UseAutoDetected);
            useAutoDetected.Action(notification);
            locator.Received(1).SetCustomJreToGlobal();
            restartAction.Received(1).Invoke();
        }

        [TestMethod]
        public void Show_DoesNotAddUseAutoDetectedAction_WhenNotAvailable()
        {
            var notificationService = Substitute.For<INotificationService>();
            var outputWindowService = Substitute.For<IOutputWindowService>();
            var locator = Substitute.For<ISLCoreLocator>();
            locator.IsCustomJreSet().Returns(false);
            locator.IsGlobalJreAvailable().Returns(false); // disables UseAutoDetected

            INotification notification = null;
            notificationService.When(ns => ns.ShowNotification(Arg.Any<INotification>())).Do(n => notification = (INotification)n.Args()[0]);

            var testSubject = new SloopRestartFailedNotificationService(notificationService, outputWindowService, locator);
            testSubject.Show(() => { });

            notificationService.Received(1).ShowNotification(notification);
            notification.Should().NotBeNull();
            notification.Actions.Select(a => a.CommandText).Should().NotContain(SLCoreStrings.SloopRestartFailedNotificationService_UseAutoDetected);
        }
    }
}

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

using System.Linq;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.SLCore.Notification;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Notification
{
    [TestClass]
    public class SloopRestartFailedNotificationServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SloopRestartFailedNotificationService, ISloopRestartFailedNotificationService>(MefTestHelpers.CreateExport<INotificationService>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SloopRestartFailedNotificationService>();
        }

        [TestMethod]
        public void Show_CreatesNotificationAndCallsShow()
        {
            bool actionIsRun = false;
            INotification notification = null;
            Action act = () => { actionIsRun = true; };

            var notificationService = Substitute.For<INotificationService>();
            notificationService.When(ns => ns.ShowNotification(Arg.Any<INotification>())).Do(n => notification = (INotification)n.Args()[0]);

            var testSubject = new SloopRestartFailedNotificationService(notificationService);

            testSubject.Show(act);

            notificationService.Received(1).ShowNotification(notification);

            notification.Should().NotBeNull();
            notification.Id.Should().Be("sonarlint.sloop.restart.failed");
            notification.Message.Should().Be("SonarLint background service failed to start");
            notification.Actions.Should().HaveCount(1);

            notification.Actions.First().Action(notification);

            actionIsRun.Should().BeTrue();
        }
    }
}

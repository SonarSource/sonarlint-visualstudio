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

using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.Core.UnitTests.Notifications
{
    [TestClass]
    public class DoNotShowAgainNotificationActionTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<DoNotShowAgainNotificationAction, IDoNotShowAgainNotificationAction>(
                MefTestHelpers.CreateExport<IDisabledNotificationsStorage>());
        }

        [TestMethod]
        public void Initialize_DefaultValuesCorrect()
        {
            var testSubject = new DoNotShowAgainNotificationAction(Mock.Of<IDisabledNotificationsStorage>());

            testSubject.CommandText.Should().Be(CoreStrings.Notifications_DontShowAgainAction);
            testSubject.ShouldDismissAfterAction.Should().BeTrue();
        }

        [TestMethod]
        public void Action_NullNotification_ArgumentNullException()
        {
            var disabledNotificationsStorage = new Mock<IDisabledNotificationsStorage>();

            var testSubject = new DoNotShowAgainNotificationAction(disabledNotificationsStorage.Object);

            Action act = () => testSubject.Action(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("notification");

            disabledNotificationsStorage.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void Action_DisablesTheGivenNotification()
        {
            var disabledNotificationsStorage = new Mock<IDisabledNotificationsStorage>();

            var notification = new Mock<INotification>();
            notification.SetupGet(x => x.Id).Returns("some id");

            var testSubject = new DoNotShowAgainNotificationAction(disabledNotificationsStorage.Object);

            testSubject.Action(notification.Object);

            disabledNotificationsStorage.Verify(x=> x.DisableNotification("some id"), Times.Once);
            disabledNotificationsStorage.VerifyNoOtherCalls();
        }
    }
}

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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.SLCore.NodeJS.Notifications;

namespace SonarLint.VisualStudio.SLCore.UnitTests.NodeJS.Notifications
{
    [TestClass]
    public class UnsupportedNodeVersionNotificationServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<UnsupportedNodeVersionNotificationService, IUnsupportedNodeVersionNotificationService>(
                MefTestHelpers.CreateExport<INotificationService>(),
                MefTestHelpers.CreateExport<IDoNotShowAgainNotificationAction>(),
                MefTestHelpers.CreateExport<IBrowserService>());
        }

        [DataTestMethod]
        [DataRow(SLCore.Common.Models.Language.JS, "321", "123", "123")]
        [DataRow(SLCore.Common.Models.Language.CSS, "99.00.11", "9876.5432", "9876.5432")]
        [DataRow(SLCore.Common.Models.Language.JS, "321", null, "Not found")]
        [DataRow(SLCore.Common.Models.Language.TS, "5.4.3.2.1", null, "Not found")]
        public void Show_ShowsCorrectMessageAndNotificationId(SLCore.Common.Models.Language language, string expectedVersion, string actualVersion, string displayActualVersion)
        {
            INotification createdNotification = null;
            var notificationService = CreateNotificationService(n => createdNotification = n);

            var testSubject = CreateTestSubject(notificationService.Object);

            notificationService.Invocations.Count.Should().Be(0);
            createdNotification.Should().BeNull();

            testSubject.Show(language.ToString(), expectedVersion, actualVersion);

            notificationService.Verify(x => x.ShowNotification(It.IsAny<INotification>()), Times.Once);
            notificationService.VerifyNoOtherCalls();

            createdNotification.Should().NotBeNull();
            createdNotification.Id.Should().Be("sonarlint.nodejs.min.version.not.found");
            createdNotification.Message.Should().Be($"SonarLint: {language} analysis failed. Could not find a Node.js runtime (required: >={expectedVersion}, actual: {displayActualVersion}) on your computer.");
            createdNotification.Actions.Count().Should().Be(2);
        }

        [TestMethod]
        public void Show_HasDoNotShowAgainAction()
        {
            INotification createdNotification = null;
            var notificationService = CreateNotificationService(n => createdNotification = n);

            var doNotShowAgainNotificationAction = Mock.Of<IDoNotShowAgainNotificationAction>();

            var testSubject = CreateTestSubject(notificationService.Object, doNotShowAgainNotificationAction);

            testSubject.Show(default, default);

            notificationService.Verify(x => x.ShowNotification(It.IsAny<INotification>()), Times.Once);
            notificationService.VerifyNoOtherCalls();

            createdNotification.Should().NotBeNull();
            createdNotification.Actions.Last().Should().Be(doNotShowAgainNotificationAction);
        }

        [TestMethod]
        public void Show_ShowMoreInfoAction_OpensBrowser()
        {
            INotification createdNotification = null;
            var notificationService = CreateNotificationService(n => createdNotification = n);

            var browserService = new Mock<IBrowserService>();

            var testSubject = CreateTestSubject(notificationService.Object, browserService: browserService.Object);

            testSubject.Show(default, default);

            notificationService.Verify(x => x.ShowNotification(It.IsAny<INotification>()), Times.Once);
            notificationService.VerifyNoOtherCalls();

            createdNotification.Should().NotBeNull();
            var firstAction = createdNotification.Actions.First();

            firstAction.CommandText.Should().Be(NotificationStrings.NotificationShowMoreInfoAction);
            firstAction.Action.Should().NotBeNull();

            browserService.Invocations.Count.Should().Be(0);

            firstAction.Action(null);

            browserService.Verify(x=> x.Navigate(DocumentationLinks.LanguageSpecificRequirements_JsTs), Times.Once);
            browserService.VerifyNoOtherCalls();
        }

        private static UnsupportedNodeVersionNotificationService CreateTestSubject(
            INotificationService notificationService,
            IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction = null,
            IBrowserService browserService = null)
        {
            return new UnsupportedNodeVersionNotificationService(notificationService, doNotShowAgainNotificationAction, browserService);
        }

        private Mock<INotificationService> CreateNotificationService(Action<INotification> callback)
        {
            var notificationService = new Mock<INotificationService>();
            notificationService
                .Setup(x => x.ShowNotification(It.IsAny<INotification>()))
                .Callback(callback);

            return notificationService;
        }
    }
}

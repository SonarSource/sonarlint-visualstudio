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
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.ConnectedMode.Migration.Wizard;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.TestInfrastructure;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class MigrationPromptTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MigrationPrompt, IMigrationPrompt>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<INotificationService>(),
                MefTestHelpers.CreateExport<IMigrationWizardController>(),
                MefTestHelpers.CreateExport<IBrowserService>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void CheckIsNonSharedMefComponent()
        {
            MefTestHelpers.CheckIsNonSharedMefComponent<MigrationPrompt>();
        }

        [TestMethod]
        public void Ctor_VerifySubscribed()
        {
            var migrationWizardController = new Mock<IMigrationWizardController>();
            _ = CreateTestSubject(migrationWizardController: migrationWizardController.Object);

            migrationWizardController.VerifyAdd(x => x.MigrationWizardFinished += It.IsAny<EventHandler>(), Times.Once);
        }

        [TestMethod]
        public async Task ShowAsync_VerifyNotificationIsCreatedWithExpectedParameters()
        {
            var notificationService = new Mock<INotificationService>();
            INotification notification = null;
            notificationService
                .Setup(x => x.ShowNotification(It.IsAny<INotification>()))
                .Callback((INotification n) => notification = n);

            var serviceProvider = SetUpServiceProviderWithSolution("path_to_solution");

            var testSubject = CreateTestSubject(serviceProvider: serviceProvider, notificationService: notificationService.Object);
            await testSubject.ShowAsync();

            notificationService.Verify(x => x.ShowNotification(notification), Times.Once);
            notification.Id.Should().Be("ConnectedModeMigration_path_to_solution");
            notification.Message.Should().Be(MigrationStrings.MigrationPrompt_Message);
            notification.Actions.Count().Should().Be(2);

            notification.Actions.First().CommandText.Should().Be(MigrationStrings.MigrationPrompt_MigrateButton);
            notification.Actions.Last().CommandText.Should().Be(MigrationStrings.MigrationPrompt_LearnMoreButton);
        }

        [TestMethod]
        public async Task ShowAsync_ShowNotificationIsCalledOnMainThread()
        {
            var notificationService = new Mock<INotificationService>();
            var threadHandling = new Mock<IThreadHandling>();
            Action runOnUiAction = null;
            threadHandling
                .Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback((Action callbackAction) => runOnUiAction = callbackAction);

            var serviceProvider = SetUpServiceProviderWithSolution();

            var testSubject = CreateTestSubject(serviceProvider: serviceProvider, notificationService: notificationService.Object, threadHandling: threadHandling.Object);
            await testSubject.ShowAsync();

            threadHandling.Verify(x => x.RunOnUIThread(It.IsAny<Action>()), Times.Once);

            notificationService.Invocations.Should().HaveCount(0);

            runOnUiAction.Should().NotBeNull();
            runOnUiAction();

            notificationService.Verify(x => x.ShowNotification(It.IsAny<INotification>()), Times.Once);
        }

        [TestMethod]
        public async Task MigrateButtonClicked_StartMigrationWizard()
        {
            var notificationService = new Mock<INotificationService>();
            INotification notification = null;
            notificationService
                .Setup(x => x.ShowNotification(It.IsAny<INotification>()))
                .Callback((INotification n) => notification = n);

            var migrationWizardController = new Mock<IMigrationWizardController>();

            var serviceProvider = SetUpServiceProviderWithSolution();

            var testSubject = CreateTestSubject(serviceProvider: serviceProvider, notificationService: notificationService.Object, migrationWizardController: migrationWizardController.Object);

            await testSubject.ShowAsync();

            notification.Actions.First().CommandText.Should().Be(MigrationStrings.MigrationPrompt_MigrateButton);
            notification.Actions.First().Action(null);

            migrationWizardController.Verify(x => x.StartMigrationWizard(), Times.Once);
        }

        [TestMethod]
        public async Task LearnMoreButtonClicked_ShowInBrowserCalled()
        {
            var notificationService = new Mock<INotificationService>();
            INotification notification = null;
            notificationService
                .Setup(x => x.ShowNotification(It.IsAny<INotification>()))
                .Callback((INotification n) => notification = n);

            var browserService = new Mock<IBrowserService>();

            var serviceProvider = SetUpServiceProviderWithSolution();

            var testSubject = CreateTestSubject(serviceProvider: serviceProvider, notificationService: notificationService.Object, browserService: browserService.Object);

            await testSubject.ShowAsync();

            notification.Actions.LastOrDefault().CommandText.Should().Be(MigrationStrings.MigrationPrompt_LearnMoreButton);
            notification.Actions.LastOrDefault().Action(null);

            browserService.Verify(x => x.Navigate(MigrationStrings.LearnMoreUrl), Times.Once);
        }

        [TestMethod]
        public void FinishMigrationRaised_ClearIsCalled()
        {
            var notificationService = new Mock<INotificationService>();

            var migrationWizardController = new Mock<IMigrationWizardController>();

            var serviceProvider = SetUpServiceProviderWithSolution();

            _ = CreateTestSubject(serviceProvider: serviceProvider, notificationService: notificationService.Object, migrationWizardController: migrationWizardController.Object);

            migrationWizardController.Raise(x => x.MigrationWizardFinished += null, EventArgs.Empty);
            notificationService.Verify(x => x.RemoveNotification(), Times.Once);
        }

        [TestMethod]
        public void Clear_RemoveNotificationIsCalled()
        {
            var notificationService = new Mock<INotificationService>();

            var testSubject = CreateTestSubject(notificationService: notificationService.Object);
            testSubject.Clear();

            notificationService.Verify(x => x.RemoveNotification(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromEvents()
        {
            var notificationService = new Mock<INotificationService>();
            var migrationWizardController = new Mock<IMigrationWizardController>();
            var testSubject = CreateTestSubject(notificationService: notificationService.Object, migrationWizardController: migrationWizardController.Object);

            testSubject.Dispose();
            notificationService.Invocations.Clear();

            migrationWizardController.Raise(x => x.MigrationWizardFinished += null, EventArgs.Empty);

            notificationService.Verify(x => x.RemoveNotification(), Times.Never);
        }

        private IServiceProvider SetUpServiceProviderWithSolution(string pathToSolution = "")
        {
            var serviceProvider = new Mock<IServiceProvider>();

            var solution = new Mock<IVsSolution>();
            object path = pathToSolution as object;
            solution.Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out path));

            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(solution.Object);

            return serviceProvider.Object;
        }

        private MigrationPrompt CreateTestSubject(IServiceProvider serviceProvider = null,
            INotificationService notificationService = null,
            IMigrationWizardController migrationWizardController = null,
            IBrowserService browserService = null,
            IThreadHandling threadHandling = null)
        {
            serviceProvider ??= Mock.Of<IServiceProvider>();
            notificationService ??= Mock.Of<INotificationService>();
            migrationWizardController ??= Mock.Of<IMigrationWizardController>();
            browserService ??= Mock.Of<IBrowserService>();
            threadHandling ??= new NoOpThreadHandler();

            var migrationPrompt = new MigrationPrompt(serviceProvider, notificationService, migrationWizardController, browserService, threadHandling);

            return migrationPrompt;
        }
    }
}

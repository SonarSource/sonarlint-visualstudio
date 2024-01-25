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

using System;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.ConnectedMode.Migration.Wizard;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.TestInfrastructure;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class MigrationPromptTests
    {
        private static BoundSonarQubeProject AnyBoundProject = new BoundSonarQubeProject(new Uri("http://localhost:9000"), "any-key", "any-name");

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MigrationPrompt, IMigrationPrompt>(
                MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
                MefTestHelpers.CreateExport<INotificationService>(),
                MefTestHelpers.CreateExport<IMigrationWizardController>(),
                MefTestHelpers.CreateExport<IBrowserService>());
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
        [DataRow(true)]
        [DataRow(false)]
        public async Task ShowAsync_VerifyNotificationIsCreatedWithExpectedParameters(bool hasNewBindingFiles)
        {
            var notificationService = new Mock<INotificationService>();
            INotification notification = null;
            notificationService
                .Setup(x => x.ShowNotification(It.IsAny<INotification>()))
                .Callback((INotification n) => notification = n);

            var solutionInfoProvider = CreateSolutionInfoProvider("path_to_solution");

            var testSubject = CreateTestSubject(solutionInfoProvider: solutionInfoProvider, notificationService: notificationService.Object);
            await testSubject.ShowAsync(AnyBoundProject, hasNewBindingFiles);

            notificationService.Verify(x => x.ShowNotification(notification), Times.Once);
            notification.Id.Should().Be("ConnectedModeMigration_path_to_solution");
            notification.Message.Should().Be(hasNewBindingFiles ? MigrationStrings.MigrationPrompt_AlreadyConnected_Message : MigrationStrings.MigrationPrompt_Message);
            notification.Actions.Count().Should().Be(2);

            notification.Actions.First().CommandText.Should().Be(MigrationStrings.MigrationPrompt_MigrateButton);
            notification.Actions.Last().CommandText.Should().Be(MigrationStrings.MigrationPrompt_LearnMoreButton);
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

            var boundProject = new BoundSonarQubeProject(new Uri("http://any"), "my-key", "my-name");

            var testSubject = CreateTestSubject(notificationService: notificationService.Object, migrationWizardController: migrationWizardController.Object);

            await testSubject.ShowAsync(boundProject, false);

            notification.Actions.First().CommandText.Should().Be(MigrationStrings.MigrationPrompt_MigrateButton);
            notification.Actions.First().Action(null);

            migrationWizardController.Verify(x => x.StartMigrationWizard(boundProject), Times.Once);
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

            var testSubject = CreateTestSubject(notificationService: notificationService.Object, browserService: browserService.Object);

            await testSubject.ShowAsync(AnyBoundProject, false);

            notification.Actions.LastOrDefault().CommandText.Should().Be(MigrationStrings.MigrationPrompt_LearnMoreButton);
            notification.Actions.LastOrDefault().Action(null);

            browserService.Verify(x => x.Navigate(DocumentationLinks.MigrateToConnectedModeV7), Times.Once);
        }

        [TestMethod]
        public void FinishMigrationRaised_ClearIsCalled()
        {
            var notificationService = new Mock<INotificationService>();

            var migrationWizardController = new Mock<IMigrationWizardController>();

            _ = CreateTestSubject(notificationService: notificationService.Object, migrationWizardController: migrationWizardController.Object);

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

        private ISolutionInfoProvider CreateSolutionInfoProvider(string pathToSolution = "")
        {
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            solutionInfoProvider.Setup(x => x.GetFullSolutionFilePathAsync()).ReturnsAsync(pathToSolution);

            return solutionInfoProvider.Object;
        }

        private MigrationPrompt CreateTestSubject(ISolutionInfoProvider solutionInfoProvider = null,
            INotificationService notificationService = null,
            IMigrationWizardController migrationWizardController = null,
            IBrowserService browserService = null)
        {
            solutionInfoProvider ??= Mock.Of<ISolutionInfoProvider>();
            notificationService ??= Mock.Of<INotificationService>();
            migrationWizardController ??= Mock.Of<IMigrationWizardController>();
            browserService ??= Mock.Of<IBrowserService>();

            var migrationPrompt = new MigrationPrompt(solutionInfoProvider, notificationService, migrationWizardController, browserService);

            return migrationPrompt;
        }
    }
}

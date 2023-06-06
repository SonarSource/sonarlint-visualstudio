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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.TestInfrastructure;

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
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void ShowAsync_VerifyNotificationIsCreatedWithExpectedParameters()
        {
            var notificationService = new Mock<INotificationService>();
            INotification notification = null;
            notificationService
                .Setup(x => x.ShowNotification(It.IsAny<INotification>()))
                .Callback((INotification n) => notification = n);

            var serviceProvider = SetUpServiceProviderWithSolution("path_to_solution");

            var testSubject = new MigrationPrompt(serviceProvider, notificationService.Object, new NoOpThreadHandler());
            testSubject.ShowAsync().Forget();

            notificationService.Verify(x => x.ShowNotification(notification), Times.Once);
            notification.Id.Should().Be("migration_path_to_solution");
            notification.Message.Should().Be(Resources.Migration_MigrationPrompt_Message);
            notification.Actions.Count().Should().Be(2);

            notification.Actions.First().CommandText.Should().Be(Resources.Migration_MigrationPrompt_MigrateButton);
            notification.Actions.Last().CommandText.Should().Be(Resources.Migration_MigrationPrompt_LearnMoreButton);
        }

        [TestMethod]
        public void ShowAsync_ShowNotificationIsCalledOnMainThread()
        {
            var notificationService = new Mock<INotificationService>();
            var threadHandling = new Mock<IThreadHandling>();
            Action runOnUiAction = null;
            threadHandling
                .Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback((Action callbackAction) => runOnUiAction = callbackAction);

            var serviceProvider = SetUpServiceProviderWithSolution();

            var testSubject = new MigrationPrompt(serviceProvider, notificationService.Object, threadHandling.Object);
            testSubject.ShowAsync().Forget();

            threadHandling.Verify(x => x.RunOnUIThread(It.IsAny<Action>()), Times.Once);

            runOnUiAction.Should().NotBeNull();
            runOnUiAction();

            notificationService.Verify(x => x.ShowNotification(It.IsAny<INotification>()), Times.Once);
        }

        [TestMethod]
        public void Clear_RemoveNotificationIsCalled()
        {
            var notificationService = new Mock<INotificationService>();

            var testSubject = new MigrationPrompt(Mock.Of<IServiceProvider>(), notificationService.Object, new NoOpThreadHandler()); ;
            testSubject.Clear();

            notificationService.Verify(x => x.RemoveNotification(), Times.Once);
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
    }
}

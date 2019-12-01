/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BindingWorkflowTests
    {
        private ConfigurableHost host;
        private Mock<IBindingProcess> mockBindingProcess;

        private BindingWorkflow testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            this.host = new ConfigurableHost(new ConfigurableServiceProvider(), Dispatcher.CurrentDispatcher);
            this.mockBindingProcess = new Mock<IBindingProcess>();

            this.testSubject = new BindingWorkflow(host, mockBindingProcess.Object);
        }

        #region Tests

        [TestMethod]
        public void Ctor_ArgChecks()
        {
            // 1. Null host
            Action act = () => new BindingWorkflow(null, mockBindingProcess.Object);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("host");

            // 2. Null binding process
            act = () => new BindingWorkflow(host, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingProcess");

        }

        [TestMethod]
        public async Task BindingWorkflow_DownloadQualityProfile_Success()
        {
            // Arrange
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            SetDownloadQPResult(true);

            // Act
            await testSubject.DownloadQualityProfileAsync(controller, notifications, new[] { Language.VBNET }, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);

            // duncanp
            //notifications.AssertProgress(0.0, 1.0);
            //notifications.AssertProgressMessages(Strings.DownloadingQualityProfileProgressMessage, string.Empty);
        }

        [TestMethod]
        public async Task BindingWorkflow_DownloadQualityProfile_Fails_WorkflowAborted()
        {
            // Arrange
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            SetDownloadQPResult(false);

            // Act
            await testSubject.DownloadQualityProfileAsync(controller, notifications, new[] { Language.VBNET }, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
        }

        private void SetDownloadQPResult(bool result)
        {
            mockBindingProcess.Setup(x => x.DownloadQualityProfileAsync(It.IsAny<IProgressStepExecutionEvents>(),
                It.IsAny<IEnumerable<Language>>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(result));
        }

        [TestMethod]
        public void BindingWorkflow_InstallPackages_NoError()
        {
            // Arrange
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var cts = new CancellationTokenSource();

            // Act
            testSubject.InstallPackages(progressEvents, cts.Token);

            // Assert
            mockBindingProcess.Verify(x => x.InstallPackages(progressEvents, cts.Token), Times.Once);

        }

        [TestMethod]
        public void BindingWorkflow_InitializeSolutionBindingOnUIThread_NoError()
        {
            // Arrange
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            // Act
            testSubject.InitializeSolutionBindingOnUIThread(progressEvents);

            // Assert
            mockBindingProcess.Verify(x => x.InitializeSolutionBindingOnUIThread(), Times.Once);
            progressEvents.AssertProgressMessages(Strings.RuleSetGenerationProgressMessage);
        }

        [TestMethod]
        public void BindingWorkflow_PrepareSolutionBinding_Passthrough()
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            testSubject.PrepareSolutionBinding(cts.Token);

            // Assert
            mockBindingProcess.Verify(x => x.PrepareSolutionBinding(cts.Token), Times.Once);
        }

        [TestMethod]
        public void BindingWorkflow_FinishSolutionBindingOnUIThread_Succeeds()
        {
            // Arrange
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var cts = new CancellationTokenSource();

            mockBindingProcess.Setup(x => x.FinishSolutionBindingOnUIThread()).Returns(true);

            // Act
            testSubject.FinishSolutionBindingOnUIThread(controller, cts.Token);

            // Assert
            mockBindingProcess.Verify(x => x.FinishSolutionBindingOnUIThread(), Times.Once);
            controller.NumberOfAbortRequests.Should().Be(0);
        }

        [TestMethod]
        public void BindingWorkflow_FinishSolutionBindingOnUIThread_Fails()
        {
            // Arrange
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var cts = new CancellationTokenSource();

            mockBindingProcess.Setup(x => x.FinishSolutionBindingOnUIThread()).Returns(false);

            // Act
            testSubject.FinishSolutionBindingOnUIThread(controller, cts.Token);

            // Assert
            mockBindingProcess.Verify(x => x.FinishSolutionBindingOnUIThread(), Times.Once);
            controller.NumberOfAbortRequests.Should().Be(1);
        }

        [TestMethod]
        public void BindingWorkflow_PrepareToInstallPackages_Passthrough()
        {
            // Arrange
            // Act
            testSubject.PrepareToInstallPackages();

            // Assert
            mockBindingProcess.Verify(x => x.PrepareToInstallPackages(), Times.Once);
        }

        [TestMethod]
        public void BindingWorkflow_EmitBindingCompleteMessage()
        {
            // Arrange

            // Test case 1: process succeeded
            mockBindingProcess.Setup(x => x.BindOperationSucceeded).Returns(true);
            var notificationsOk = new ConfigurableProgressStepExecutionEvents();

            // Act
            testSubject.EmitBindingCompleteMessage(notificationsOk);

            // Assert
            notificationsOk.AssertProgressMessages(string.Format(CultureInfo.CurrentCulture, Strings.FinishedSolutionBindingWorkflowSuccessful));

            // Test case 2: process failed
            // Arrange
            mockBindingProcess.Setup(x => x.BindOperationSucceeded).Returns(false);
            var notificationsFail = new ConfigurableProgressStepExecutionEvents();
            
            // Act
            testSubject.EmitBindingCompleteMessage(notificationsFail);

            // Assert
            notificationsFail.AssertProgressMessages(string.Format(CultureInfo.CurrentCulture, Strings.FinishedSolutionBindingWorkflowNotAllPackagesInstalled));
        }

        [TestMethod]
        public void BindingWorkflow_PromptSaveSolutionIfDirty()
        {
            // Arrange
            var controller = new ConfigurableProgressController();

            // Case 1: Users saves the changes
            mockBindingProcess.Setup(x => x.PromptSaveSolutionIfDirty()).Returns(true);

            // Act
            testSubject.PromptSaveSolutionIfDirty(controller, CancellationToken.None);
            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);

            // Case 2: Users cancels the save
            mockBindingProcess.Setup(x => x.PromptSaveSolutionIfDirty()).Returns(false);
            // Act
            testSubject.PromptSaveSolutionIfDirty(controller, CancellationToken.None);
            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
        }

        [TestMethod]
        public void BindingWorkflow_SilentSaveSolutionIfDirty()
        {
            // Act
            testSubject.SilentSaveSolutionIfDirty();

            // Assert
            mockBindingProcess.Verify(x => x.SilentSaveSolutionIfDirty(), Times.Once);
        }

        [TestMethod]
        public void BindingWorkflow_DiscoverProjects_Succeed_WorkflowNotAborted()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            mockBindingProcess.Setup(x => x.DiscoverProjects()).Returns(true);

            // Act
            testSubject.DiscoverProjects(controller, progressEvents);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);
            progressEvents.AssertProgressMessages(Strings.DiscoveringSolutionProjectsProgressMessage);
        }

        [TestMethod]
        public void BindingWorkflow_DiscoverProjects_Fails_WorkflowAborted()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            mockBindingProcess.Setup(x => x.DiscoverProjects()).Returns(false);

            // Act
            testSubject.DiscoverProjects(controller, progressEvents);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
            progressEvents.AssertProgressMessages(Strings.DiscoveringSolutionProjectsProgressMessage);
        }

        #endregion Tests
    }
}

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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.TestInfrastructure;

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
            await testSubject.DownloadQualityProfileAsync(controller, notifications, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);
        }

        [TestMethod]
        public async Task BindingWorkflow_DownloadQualityProfile_Fails_WorkflowAborted()
        {
            // Arrange
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            SetDownloadQPResult(false);

            // Act
            await testSubject.DownloadQualityProfileAsync(controller, notifications, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
        }

        private void SetDownloadQPResult(bool result)
        {
            mockBindingProcess.Setup(x => x.DownloadQualityProfileAsync(It.IsAny<IProgress<FixedStepsProgress>>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(result));
        }

        [TestMethod]
        public async Task BindingWorkflow_SaveServerExclusions_Success()
        {
            // Arrange
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            SetSaveServerExclusionsResult(true);

            // Act
            await testSubject.SaveServerExclusionsAsync(controller, notifications, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);
            mockBindingProcess.Verify(bp => bp.SaveServerExclusionsAsync(CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public async Task BindingWorkflow_SaveServerExclusions_Fails_WorkflowAborted()
        {
            // Arrange
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            SetSaveServerExclusionsResult(false);

            // Act
            await testSubject.SaveServerExclusionsAsync(controller, notifications, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
            mockBindingProcess.Verify(bp => bp.SaveServerExclusionsAsync(CancellationToken.None), Times.Once);
        }

        private void SetSaveServerExclusionsResult(bool result)
        {
            mockBindingProcess.Setup(x => x.SaveServerExclusionsAsync(
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(result));
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

        #endregion Tests
    }
}

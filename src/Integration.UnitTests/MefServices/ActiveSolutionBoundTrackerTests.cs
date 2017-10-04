/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Linq.Expressions;
using System.Threading;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ActiveSolutionBoundTrackerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;
        private ConfigurableActiveSolutionTracker activeSolutionTracker;
        private ConfigurableHost host;
        private ConfigurableErrorListInfoBarController errorListController;
        private ConfigurableSolutionBindingInformationProvider solutionBindingInformationProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider(false);
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            var mefExport1 = MefTestHelpers.CreateExport<IHost>(this.host);

            this.activeSolutionTracker = new ConfigurableActiveSolutionTracker();
            var mefExport2 = MefTestHelpers.CreateExport<IActiveSolutionTracker>(this.activeSolutionTracker);

            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExport1, mefExport2);

            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel, replaceExisting: true);

            this.solutionMock = new SolutionMock();
            this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);

            this.errorListController = new ConfigurableErrorListInfoBarController();
            this.serviceProvider.RegisterService(typeof(IErrorListInfoBarController), this.errorListController);

            this.solutionBindingInformationProvider = new ConfigurableSolutionBindingInformationProvider();
            this.serviceProvider.RegisterService(typeof(ISolutionBindingInformationProvider), this.solutionBindingInformationProvider);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_ArgChecks()
        {
            // Arrange
            Exceptions.Expect<ArgumentNullException>(() =>
                new ActiveSolutionBoundTracker(null, new ConfigurableActiveSolutionTracker()));
            Exceptions.Expect<ArgumentNullException>(() =>
                new ActiveSolutionBoundTracker(this.host, null));
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Unbound()
        {
            // Arrange
            host.VisualStateManager.ClearBoundProject();

            // Act
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker);

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeFalse("Unbound solution should report false activation");
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(1);
            this.errorListController.ResetCalledCount.Should().Be(0);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Bound()
        {
            // Arrange
            this.solutionBindingInformationProvider.SolutionBound = true;
            this.host.VisualStateManager.SetBoundProject(new SonarQubeProject("", ""));

            // Act
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker);

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeTrue("Bound solution should report true activation");
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(1);
            this.errorListController.ResetCalledCount.Should().Be(0);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Changes()
        {
            var boundProject = new BoundSonarQubeProject(new Uri("http://localhost:9000"), "key");

            var solutionBinding = new ConfigurableSolutionBindingSerializer();

            bool serviceIsConnected = false;
            var mockSqService = new Mock<ISonarQubeService>();

            Expression<Action<ISonarQubeService>> connectMethod = x => x.ConnectAsync(It.IsAny<ConnectionInformation>(), It.IsAny<CancellationToken>());
            Expression<Action<ISonarQubeService>> disconnectMethod = x => x.Disconnect();

            mockSqService.SetupGet(x => x.IsConnected).Returns(() => serviceIsConnected);
            mockSqService.Setup(disconnectMethod).Callback(() => serviceIsConnected = false).Verifiable();
            mockSqService.Setup(connectMethod).Callback(() => serviceIsConnected = true).Verifiable();
            this.host.SonarQubeService = mockSqService.Object;

            this.serviceProvider.RegisterService(typeof(ISolutionBindingSerializer), solutionBinding);

            solutionBinding.CurrentBinding = boundProject;
            this.solutionBindingInformationProvider.SolutionBound = true;
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker);
            var solutionBindingChangedEventCount = 0;
            testSubject.SolutionBindingChanged += (obj, args) => { solutionBindingChangedEventCount++; };

            // Sanity
            testSubject.IsActiveSolutionBound.Should().BeTrue("Initially bound");
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(1);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(0, "no events raised during construction");

            // Case 1: Clear bound project
            solutionBinding.CurrentBinding = null;
            this.solutionBindingInformationProvider.SolutionBound = false;
            // Act
            host.VisualStateManager.ClearBoundProject();

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeFalse("Unbound solution should report false activation");
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(1);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(1, "Unbind should trigger reanalysis");

            // Connect not called
            // Disconnect not called
            mockSqService.Verify(disconnectMethod, Times.Never);
            mockSqService.Verify(connectMethod, Times.Never);

            // Case 2: Set bound project
            solutionBinding.CurrentBinding = boundProject;
            this.solutionBindingInformationProvider.SolutionBound = true;
            // Act
            host.VisualStateManager.SetBoundProject(new SonarQubeProject("", ""));

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeTrue("Bound solution should report true activation");
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(1);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(2, "Bind should trigger reanalysis");

            // Notifications from the Team Explorer should not trigger connect/disconnect
            mockSqService.Verify(disconnectMethod, Times.Never);
            mockSqService.Verify(connectMethod, Times.Never);

            // Case 3: Solution unloaded
            solutionBinding.CurrentBinding = null;
            this.solutionBindingInformationProvider.SolutionBound = false;
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeFalse("Should respond to solution change event and report unbound");
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(2);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(3, "Solution change should trigger reanalysis");

            // Closing an unbound solution should not call disconnect/connect
            mockSqService.Verify(disconnectMethod, Times.Never);
            mockSqService.Verify(connectMethod, Times.Never);

            // Case 4: Load a bound solution
            solutionBinding.CurrentBinding = boundProject;
            this.solutionBindingInformationProvider.SolutionBound = true;
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeTrue("Bound respond to solution change event and report bound");
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(3);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(4, "Solution change should trigger reanalysis");

            // Loading a bound solution should call connect
            mockSqService.Verify(disconnectMethod, Times.Never);
            mockSqService.Verify(connectMethod, Times.Exactly(1));

            // Case 5: Close a bound solution
            solutionBinding.CurrentBinding = null;
            this.solutionBindingInformationProvider.SolutionBound = false;
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(4);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(5, "Solution change should trigger reanalysis");

            // Closing a bound solution should call disconnect
            mockSqService.Verify(disconnectMethod, Times.Exactly(1));
            mockSqService.Verify(connectMethod, Times.Exactly(1));

            // Case 6: Dispose and change
            // Act
            testSubject.Dispose();
            solutionBinding.CurrentBinding = null;
            this.solutionBindingInformationProvider.SolutionBound = true;
            host.VisualStateManager.ClearBoundProject();

            // Assert
            solutionBindingChangedEventCount.Should().Be(5, "Once disposed should stop raising the event");
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(3);
            this.errorListController.ResetCalledCount.Should().Be(1);
            mockSqService.Verify(disconnectMethod, Times.Exactly(1));
            mockSqService.Verify(connectMethod, Times.Exactly(1));
        }
    }
}
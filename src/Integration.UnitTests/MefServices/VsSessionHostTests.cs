﻿/*
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

using Moq;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.MefServices;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class VsSessionHostTests
    {
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableStateManager stateManager;
        private ConfigurableProgressStepRunner stepRunner;
        private ConfigurableConfigurationProvider configProvider;
        private Mock<ISharedBindingSuggestionService> sharedBindingSuggestionService;
        private Mock<IConnectedModeWindowEventListener> connectedModeWindowEventListener;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.sonarQubeServiceMock = new Mock<ISonarQubeService>();
            this.stepRunner = new ConfigurableProgressStepRunner();
            this.configProvider = new ConfigurableConfigurationProvider();
            sharedBindingSuggestionService = new Mock<ISharedBindingSuggestionService>();
            connectedModeWindowEventListener = new Mock<IConnectedModeWindowEventListener>();
        }

        #region Tests

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsSessionHost, IHost>(
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IConnectedModeWindowEventListener>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Ctor_SubscribesToActiveSolutionChanged()
        {
            var trackerMock = new Mock<IActiveSolutionTracker>();

            var testSubject = CreateTestSubject(trackerMock.Object);

            trackerMock.VerifyAdd(x => x.ActiveSolutionChanged += It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Once);
            connectedModeWindowEventListener.Verify(x => x.SubscribeToConnectedModeWindowEvents(testSubject), Times.Once);
        }
        
        [TestMethod]
        public void Dispose_UnsubscribesFromActiveSolutionChanged()
        {
            var trackerMock = new Mock<IActiveSolutionTracker>();
            var testSubject = CreateTestSubject(trackerMock.Object);

            testSubject.Dispose();

            trackerMock.VerifyRemove(x => x.ActiveSolutionChanged -= It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Once);
            connectedModeWindowEventListener.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void VsSessionHost_SetActiveSection()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);

            // Case 1: Invalid args
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetActiveSection(null));

            // Case 2: Valid args
            var section1 = ConfigurableSectionController.CreateDefault();
            var section2 = ConfigurableSectionController.CreateDefault();
            bool refresh1Called = false;
            section1.RefreshCommand = new RelayCommand<ConnectionInformation>(c => refresh1Called = true);
            bool refresh2Called = false;
            section2.RefreshCommand = new RelayCommand<ConnectionInformation>(c => refresh2Called = true);

            // Act (set section1)
            testSubject.SetActiveSection(section1);
            refresh1Called.Should().BeFalse();
            refresh2Called.Should().BeFalse();

            // Assert
            testSubject.ActiveSection.Should().Be(section1);

            // Act (set section2)
            testSubject.ClearActiveSection();
            testSubject.SetActiveSection(section2);

            // Assert
            testSubject.ActiveSection.Should().Be(section2);
            refresh1Called.Should().BeFalse();
            refresh2Called.Should().BeFalse();
        }

        [TestMethod]
        public void VsSessionHost_SetActiveSection_TransferState()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();

            // Act
            testSubject.SetActiveSection(section);

            // Assert
            testSubject.ActiveSection.ViewModel.State.Should().Be(stateManager.ManagedState);
        }

        [TestMethod]
        public void VsSessionHost_SetActiveSection_ChangeHost()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();

            // Sanity
            this.stepRunner.CurrentHost.Should().BeNull();

            // Act
            testSubject.SetActiveSection(section);

            // Assert
            this.stepRunner.CurrentHost.Should().Be(section.ProgressHost);
        }

        [TestMethod]
        public void VsSessionHost_ClearActiveSection_ClearState()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();
            testSubject.SetActiveSection(section);

            // Act
            testSubject.ClearActiveSection();

            // Assert
            testSubject.ActiveSection.Should().BeNull();
            section.ViewModel.State.Should().BeNull();
        }

        [TestMethod]
        public void VsSessionHost_ActiveSectionChangedEvent()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();
            ISectionController otherSection = ConfigurableSectionController.CreateDefault();
            int changed = 0;
            testSubject.ActiveSectionChanged += (o, e) => changed++;

            // Act (1st set)
            testSubject.SetActiveSection(section);

            // Assert
            changed.Should().Be(1, "ActiveSectionChanged event was expected to fire");

            // Act (clear)
            testSubject.ClearActiveSection();

            // Assert
            changed.Should().Be(2, "ActiveSectionChanged event was expected to fire");

            // Act (2nd set)
            testSubject.SetActiveSection(otherSection);

            // Assert
            changed.Should().Be(3, "ActiveSectionChanged event was expected to fire");

            // Act (clear)
            testSubject.ClearActiveSection();

            // Assert
            changed.Should().Be(4, "ActiveSectionChanged event was expected to fire");

            // Act (clear again)
            testSubject.ClearActiveSection();

            // Assert
            changed.Should().Be(4, "ActiveSectionChanged event was not expected to fire, since already cleared");
        }

        [TestMethod]
        public void VsSessionHost_SyncCommandFromActiveSectionDuringActiveSectionChanges()
        {
            // Arrange
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();
            int syncCalled = 0;
            this.stateManager.SyncCommandFromActiveSectionAction = () => syncCalled++;

            // Case 1: SetActiveSection
            this.stateManager.ExpectActiveSection = true;

            // Act
            testSubject.SetActiveSection(section);

            // Assert
            syncCalled.Should().Be(1, "SyncCommandFromActiveSection wasn't called during section activation");

            // Case 2: ClearActiveSection section
            this.stateManager.ExpectActiveSection = false;

            // Act
            testSubject.ClearActiveSection();

            // Assert
            syncCalled.Should().Be(2, "SyncCommandFromActiveSection wasn't called during section deactivation");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_NoOpenSolutionScenario()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            this.CreateTestSubject(tracker);

            this.stateManager.BoundProjectKey = "bla";
            this.stateManager.SetBoundProject(new Uri("http://localhost"), null, "bla");

            // Sanity
            this.stateManager.AssignedProjectKey.Should().Be("bla");
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(0);

            // Act
            tracker.SimulateActiveSolutionChanged(isSolutionOpen: false);

            // Assert
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(1);
            this.stateManager.AssignedProjectKey.Should().BeNull();
            this.stateManager.BoundProjectKey.Should().BeNull("Expecting the key to be reset to null");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_BoundSolutionWithNoActiveSectionScenario()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            SetConfiguration(new BoundServerProject("solution", "bla", new ServerConnection.SonarQube(new Uri("http://bound"))), SonarLintMode.LegacyConnected);
            this.stateManager.SetBoundProject(new Uri("http://bound"), null, "bla");

            // Sanity
            this.stateManager.AssignedProjectKey.Should().Be("bla");
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(0);

            // Act (simulate solution opened event)
            tracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

            // Assert that nothing has changed (should defer all the work to when the section is connected)
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(1);
            this.stateManager.AssignedProjectKey.Should().Be("bla");
            this.stateManager.BoundProjectKey.Should().BeNull("The key should only be set when there's active section to allow marking it once fetched all the projects");

            // Act (set active section)
            var section = ConfigurableSectionController.CreateDefault();
            bool refreshCalled = false;
            section.RefreshCommand = new RelayCommand<ConnectionInformation>(c => refreshCalled = true);
            testSubject.SetActiveSection(section);

            // Assert (section has refreshed, no further aborts were required)
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(1);
            this.stateManager.BoundProjectKey.Should().Be("bla", "Key was not set, will not be able to mark project as bound after refresh");
            this.stateManager.AssignedProjectKey.Should().Be("bla");
            refreshCalled.Should().BeTrue("Expected the refresh command to be called");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_BoundSolutionWithActiveSectionScenario()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            this.stateManager.SetBoundProject(new Uri("http://bound"), "org1", "bla");
            SetConfiguration(new BoundServerProject("solution", "bla", new ServerConnection.SonarQube(new Uri("http://bound"))), SonarLintMode.LegacyConnected);
            var section = ConfigurableSectionController.CreateDefault();
            bool refreshCalled = false;
            section.RefreshCommand = new RelayCommand<ConnectionInformation>(c => refreshCalled = true);
            testSubject.SetActiveSection(section);

            // Sanity
            this.stateManager.AssignedProjectKey.Should().Be("bla");
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(0);

            // Act (simulate solution opened event)
            tracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

            // Assert
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(1);
            this.stateManager.BoundProjectKey.Should().Be("bla", "Key was not set, will not be able to mark project as bound after refresh");
            this.stateManager.AssignedProjectKey.Should().Be("bla");
            refreshCalled.Should().BeTrue("Expected the refresh command to be called");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_ErrorInReadingBinding()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);

            this.stateManager.SetBoundProject(new Uri("http://bound"), null, "bla");
            SetConfiguration(new BoundServerProject("solution", "bla", new ServerConnection.SonarQube(new Uri("http://bound"))), SonarLintMode.LegacyConnected);
            var section = ConfigurableSectionController.CreateDefault();
            testSubject.SetActiveSection(section);

            // Sanity
            this.stateManager.AssignedProjectKey.Should().Be("bla");
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(0);

            // Introduce an error
            this.configProvider.GetConfigurationAction = () => { throw new Exception("boom"); };

            // Act (i.e. simulate loading a different solution)
            using (new AssertIgnoreScope()) // Ignore exception assert
            {
                tracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
            }

            // Assert
            this.stateManager.AssignedProjectKey.Should().BeNull();
        }

        #endregion Tests

        #region Helpers

        private VsSessionHost CreateTestSubject(IActiveSolutionTracker tracker = null)
        {
            this.stateManager = new ConfigurableStateManager();
            var host = new VsSessionHost(stateManager,
                this.stepRunner,
                this.sonarQubeServiceMock.Object,
                tracker ?? new ConfigurableActiveSolutionTracker(),
                this.configProvider,
                connectedModeWindowEventListener.Object,
                Mock.Of<ILogger>());

            this.stateManager.Host = host;

            return host;
        }

        private void SetConfiguration(BoundServerProject project, SonarLintMode mode)
        {
            this.configProvider.ProjectToReturn = project;
            this.configProvider.ModeToReturn = mode;
            this.configProvider.FolderPathToReturn = "c:\\test\\";
        }

        #endregion Helpers
    }
}

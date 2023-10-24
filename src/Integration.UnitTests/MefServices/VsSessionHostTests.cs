﻿/*
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
using FluentAssertions;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Connection;
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
        private Mock<ISharedBindingConfigProvider> sharedBindingConfigProviderMock;
        private Mock<ICredentialStoreService> credentialStoreServiceMock;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.sonarQubeServiceMock = new Mock<ISonarQubeService>();
            this.stepRunner = new ConfigurableProgressStepRunner();
            this.configProvider = new ConfigurableConfigurationProvider();
            sharedBindingConfigProviderMock = new Mock<ISharedBindingConfigProvider>();
            credentialStoreServiceMock = new Mock<ICredentialStoreService>();
        }

        #region Tests

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsSessionHost, IHost>(
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<ISharedBindingConfigProvider>(),
                MefTestHelpers.CreateExport<ICredentialStoreService>(),
                MefTestHelpers.CreateExport<ILogger>());
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
            SetConfiguration(new BoundSonarQubeProject(new Uri("http://bound"), "bla", "projectName"), SonarLintMode.LegacyConnected);
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
            SetConfiguration(new BoundSonarQubeProject(new Uri("http://bound"), "bla", "projectName"), SonarLintMode.LegacyConnected);
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
            SetConfiguration(new BoundSonarQubeProject(new Uri("http://bound"), "bla", "projectName"), SonarLintMode.LegacyConnected);
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

        [TestMethod]
        public void ResetBinding_SharedConfigSetWhenUnbound()
        {
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = CreateTestSubject(tracker);
            var section = ConfigurableSectionController.CreateDefault();
            testSubject.SetActiveSection(section);
            sharedBindingConfigProviderMock.Setup(x => x.GetSharedBinding())
                .Returns(new SharedBindingConfigModel { ProjectKey = "abcd" });
            
            this.stateManager.BoundProjectKey.Should().Be(null);
            
            tracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
            
            testSubject.SharedBindingConfig.Should().NotBeNull();
            testSubject.VisualStateManager.HasSharedBinding.Should().BeTrue();
            ((ConfigurableStateManager)testSubject.VisualStateManager).ResetConnectionConfigCalled.Should().BeTrue();
        }
        
        [TestMethod]
        public void ResetBinding_SharedConfigNotSetWhenNull()
        {
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = CreateTestSubject(tracker);
            var section = ConfigurableSectionController.CreateDefault();
            testSubject.SetActiveSection(section);
            sharedBindingConfigProviderMock.Setup(x => x.GetSharedBinding())
                .Returns((SharedBindingConfigModel)null);
            
            this.stateManager.BoundProjectKey.Should().Be(null);
            
            tracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
            
            testSubject.SharedBindingConfig.Should().BeNull();
            testSubject.VisualStateManager.HasSharedBinding.Should().BeFalse();
            this.stateManager.BoundProjectKey.Should().Be(null);
            ((ConfigurableStateManager)testSubject.VisualStateManager).ResetConnectionConfigCalled.Should().BeTrue();
        }
        
        [TestMethod]
        public void InitializeBinding_SharedConfigSetWhenUnbound()
        {
            var testSubject = CreateTestSubject();
            var section = ConfigurableSectionController.CreateDefault();
            sharedBindingConfigProviderMock.Setup(x => x.GetSharedBinding())
                .Returns(new SharedBindingConfigModel { ProjectKey = "abcd" });
            
            this.stateManager.BoundProjectKey.Should().Be(null);
            
            testSubject.SetActiveSection(section);
            
            testSubject.SharedBindingConfig.Should().NotBeNull();
            testSubject.VisualStateManager.HasSharedBinding.Should().BeTrue();
            ((ConfigurableStateManager)testSubject.VisualStateManager).ResetConnectionConfigCalled.Should().BeTrue();
        }
        
        [TestMethod]
        public void ResetBinding_SharedConfigRemovedWhenBound()
        {
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = CreateTestSubject(tracker);
            var section = ConfigurableSectionController.CreateDefault();
            sharedBindingConfigProviderMock.Setup(x => x.GetSharedBinding())
                .Returns(new SharedBindingConfigModel { ProjectKey = "abcd" });
            testSubject.SetActiveSection(section);

            testSubject.SharedBindingConfig.Should().NotBeNull();
            
            this.stateManager.SetBoundProject(new Uri("http://bound"), null, "bla");
            SetConfiguration(new BoundSonarQubeProject(new Uri("http://bound"), "bla", "projectName"), SonarLintMode.Connected);
            
            tracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

            testSubject.SharedBindingConfig.Should().BeNull();
            this.stateManager.BoundProjectKey.Should().Be("bla");
        }

        [DataRow(null)]
        [DataRow("http://localhost:9000")]
        [DataRow("https://sonarqube.io")]
        [DataTestMethod]
        public void GetCredentialsForSharedConfig_CallsCredentialServiceOnlyWhenSharedConfigExists(string serverUri)
        {
            var testSubject = CreateTestSubject();
            var section = ConfigurableSectionController.CreateDefault();
            sharedBindingConfigProviderMock.Setup(x => x.GetSharedBinding())
                .Returns(serverUri != null ? new SharedBindingConfigModel { Uri = serverUri} : null);
            testSubject.SetActiveSection(section);
            var credential = new Credential("a");
            credentialStoreServiceMock.Setup(x => x.ReadCredentials(It.IsAny<TargetUri>())).Returns(credential);

            var result = testSubject.GetCredentialsForSharedConfig();
            
            if (serverUri == null)
            {
                credentialStoreServiceMock.Verify(x => x.ReadCredentials(It.IsAny<TargetUri>()), Times.Never);
                result.Should().BeNull();
            }
            else
            {
                credentialStoreServiceMock.Verify(x => x.ReadCredentials(It.IsAny<TargetUri>()), Times.Once);
                result.Should().BeSameAs(credential);
            }
        }

        #endregion Tests

        #region Helpers

        private VsSessionHost CreateTestSubject(ConfigurableActiveSolutionTracker tracker = null)
        {
            this.stateManager = new ConfigurableStateManager();
            var host = new VsSessionHost(stateManager,
                this.stepRunner,
                this.sonarQubeServiceMock.Object,
                tracker ?? new ConfigurableActiveSolutionTracker(),
                this.configProvider,
                sharedBindingConfigProviderMock.Object,
                credentialStoreServiceMock.Object,
                Mock.Of<ILogger>());

            this.stateManager.Host = host;

            return host;
        }

        private void SetConfiguration(BoundSonarQubeProject project, SonarLintMode mode)
        {
            this.configProvider.ProjectToReturn = project;
            this.configProvider.ModeToReturn = mode;
            this.configProvider.FolderPathToReturn = "c:\\test\\";
        }

        #endregion Helpers
    }
}

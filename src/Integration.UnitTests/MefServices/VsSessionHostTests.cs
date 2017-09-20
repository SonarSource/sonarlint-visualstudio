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
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class VsSessionHostTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableStateManager stateManager;
        private ConfigurableProgressStepRunner stepRunner;
        private ConfigurableSolutionBindingSerializer solutionBinding;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
            this.serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.stepRunner = new ConfigurableProgressStepRunner();
            this.solutionBinding = new ConfigurableSolutionBindingSerializer();

            var projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystem);

            var host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);

            var propertyManager = new ProjectPropertyManager(host);
            var mefExports = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #region Tests

        [TestMethod]
        public void VsSessionHost_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new VsSessionHost(null, new Integration.Service.SonarQubeServiceWrapper(this.serviceProvider), new ConfigurableActiveSolutionTracker()));
            Exceptions.Expect<ArgumentNullException>(() => new VsSessionHost(this.serviceProvider, null, new ConfigurableActiveSolutionTracker()));
            Exceptions.Expect<ArgumentNullException>(() => new VsSessionHost(this.serviceProvider, new Integration.Service.SonarQubeServiceWrapper(this.serviceProvider), null));
            Exceptions.Expect<ArgumentNullException>(() => new VsSessionHost(this.serviceProvider, null, null, new Integration.Service.SonarQubeServiceWrapper(this.serviceProvider), new ConfigurableActiveSolutionTracker(), null));

            using (var host = new VsSessionHost(this.serviceProvider,
                                                new Integration.Service.SonarQubeServiceWrapper(this.serviceProvider),
                                                new ConfigurableActiveSolutionTracker()))
            {
                host.Should().NotBeNull("Not expecting this to fail, just to make the static analyzer happy");
            }
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
            section1.RefreshCommand = new RelayCommand(() => refresh1Called = true);
            bool refresh2Called = false;
            section2.RefreshCommand = new RelayCommand(() => refresh2Called = true);

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
            // Previous binding information that should be cleared once there's no solution
            var boundProject = new Integration.Service.SonarQubeProject { Key = "bla" };
            this.stateManager.BoundProjectKey = boundProject.Key;
            this.stateManager.SetBoundProject(boundProject);

            // Sanity
            this.stateManager.BoundProject.Should().Be(boundProject);
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(0);

            // Act
            tracker.SimulateActiveSolutionChanged();

            // Assert
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(1);
            this.stateManager.BoundProject.Should().BeNull();
            this.stateManager.BoundProjectKey.Should().BeNull("Expecting the key to be reset to null");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_BoundSolutionWithNoActiveSectionScenario()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            var boundProject = new Integration.Service.SonarQubeProject { Key = "bla" };
            this.solutionBinding.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://bound"), boundProject.Key);
            this.stateManager.SetBoundProject(boundProject);

            // Sanity
            this.stateManager.BoundProject.Should().Be(boundProject);
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(0);

            // Act (simulate solution opened event)
            tracker.SimulateActiveSolutionChanged();

            // Assert that nothing has changed (should defer all the work to when the section is connected)
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(1);
            this.stateManager.BoundProject.Should().Be(boundProject);
            this.stateManager.BoundProjectKey.Should().BeNull("The key should only be set when there's active section to allow marking it once fetched all the projects");

            // Act (set active section)
            var section = ConfigurableSectionController.CreateDefault();
            bool refreshCalled = false;
            section.RefreshCommand = new RelayCommand(() => refreshCalled = true);
            testSubject.SetActiveSection(section);

            // Assert (section has refreshed, no further aborts were required)
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(1);
            this.stateManager.BoundProjectKey.Should().Be(boundProject.Key, "Key was not set, will not be able to mark project as bound after refresh");
            this.stateManager.BoundProject.Should().Be(boundProject);
            refreshCalled.Should().BeTrue("Expected the refresh command to be called");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_BoundSolutionWithActiveSectionScenario()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            var boundProject = new Integration.Service.SonarQubeProject { Key = "bla" };
            this.stateManager.SetBoundProject(boundProject);
            this.solutionBinding.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://bound"), boundProject.Key);
            var section = ConfigurableSectionController.CreateDefault();
            bool refreshCalled = false;
            section.RefreshCommand = new RelayCommand(() => refreshCalled = true);
            testSubject.SetActiveSection(section);

            // Sanity
            this.stateManager.BoundProject.Should().Be(boundProject);
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(0);

            // Act (simulate solution opened event)
            tracker.SimulateActiveSolutionChanged();

            // Assert
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(1);
            this.stateManager.BoundProjectKey.Should().Be(boundProject.Key, "Key was not set, will not be able to mark project as bound after refresh");
            this.stateManager.BoundProject.Should().Be(boundProject);
            refreshCalled.Should().BeTrue("Expected the refresh command to be called");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_ErrorInReadingSolutionBinding()
        {
            // Arrange
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            var boundProject = new Integration.Service.SonarQubeProject { Key = "bla" };
            this.stateManager.SetBoundProject(boundProject);
            this.solutionBinding.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://bound"), boundProject.Key);
            var section = ConfigurableSectionController.CreateDefault();
            testSubject.SetActiveSection(section);

            // Sanity
            this.stateManager.BoundProject.Should().Be(boundProject);
            this.stepRunner.AbortAllNumberOfCalls.Should().Be(0);

            // Introduce an error
            this.solutionBinding.ReadSolutionBindingAction = () => { throw new Exception("boom"); };

            // Act (i.e. simulate loading a different solution)
            using (new AssertIgnoreScope()) // Ignore exception assert
            {
                tracker.SimulateActiveSolutionChanged();
            }

            // Assert
            this.stateManager.BoundProject.Should().BeNull();
        }

        [TestMethod]
        public void VsSessionHost_IServiceProvider_GetService()
        {
            // Arrange
            var testSubject = new VsSessionHost(this.serviceProvider, new Integration.Service.SonarQubeServiceWrapper(this.serviceProvider), new ConfigurableActiveSolutionTracker());
            ConfigurableVsShell shell = new ConfigurableVsShell();
            shell.RegisterPropertyGetter((int)__VSSPROPID2.VSSPROPID_InstallRootDir, () => this.TestContext.TestRunDirectory);
            this.serviceProvider.RegisterService(typeof(SVsShell), shell);

            // Local services
            // Act + Assert
            foreach (Type serviceType in VsSessionHost.SupportedLocalServices)
            {
                testSubject.GetService(serviceType).Should().NotBeNull();
            }

            testSubject.GetService<ISourceControlledFileSystem>().Should().Be(testSubject.GetService<IFileSystem>());

            // VS-services
            // Sanity
            testSubject.GetService(typeof(VsSessionHostTests)).Should().BeNull("Not expecting any service at this point");

            // Arrange
            this.serviceProvider.RegisterService(typeof(VsSessionHostTests), this);

            // Act + Assert
            testSubject.GetService(typeof(VsSessionHostTests)).Should().Be(this, "Unexpected service was returned, expected to use the service provider");
        }

        #endregion Tests

        #region Helpers

        private VsSessionHost CreateTestSubject(ConfigurableActiveSolutionTracker tracker)
        {
            this.stateManager = new ConfigurableStateManager();
            var host = new VsSessionHost(this.serviceProvider,
                stateManager,
                this.stepRunner,
                this.sonarQubeService,
                tracker?? new ConfigurableActiveSolutionTracker(),
                Dispatcher.CurrentDispatcher);

            this.stateManager.Host = host;

            host.ReplaceInternalServiceForTesting<ISolutionBindingSerializer>(this.solutionBinding);

            return host;
        }

        #endregion Helpers
    }
}
//-----------------------------------------------------------------------
// <copyright file="VsSessionHostTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Windows.Threading;

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

            using (var host = new VsSessionHost(this.serviceProvider,
                                                new Integration.Service.SonarQubeServiceWrapper(this.serviceProvider),
                                                new ConfigurableActiveSolutionTracker()))
            {
                Assert.IsNotNull(host, "Not expecting this to fail, just to make the static analyzer happy");
            }
        }

        [TestMethod]
        public void VsSessionHost_SetActiveSection()
        {
            // Setup
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
            Assert.IsFalse(refresh1Called, "Refresh should only be called when bound project was found");
            Assert.IsFalse(refresh2Called, "Refresh should only be called when bound project was found");

            // Verify
            Assert.AreSame(section1, testSubject.ActiveSection);

            // Act (set section2)
            testSubject.ClearActiveSection();
            testSubject.SetActiveSection(section2);

            // Verify
            Assert.AreSame(section2, testSubject.ActiveSection);
            Assert.IsFalse(refresh1Called, "Refresh should only be called when bound project was found");
            Assert.IsFalse(refresh2Called, "Refresh should only be called when bound project was found");
        }

        [TestMethod]
        public void VsSessionHost_SetActiveSection_TransferState()
        {
            // Setup
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();

            // Act
            testSubject.SetActiveSection(section);

            // Verify
            Assert.AreSame(stateManager.ManagedState, testSubject.ActiveSection.ViewModel.State);
        }

        [TestMethod]
        public void VsSessionHost_SetActiveSection_ChangeHost()
        {
            // Setup
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();

            // Sanity
            this.stepRunner.AssertNoCurrentHost();

            // Act
            testSubject.SetActiveSection(section);

            // Verify
            this.stepRunner.AssertCurrentHost(section.ProgressHost);
        }

        [TestMethod]
        public void VsSessionHost_ClearActiveSection_ClearState()
        {
            // Setup
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();
            testSubject.SetActiveSection(section);

            // Act
            testSubject.ClearActiveSection();

            // Verify
            Assert.IsNull(testSubject.ActiveSection);
            Assert.IsNull(section.ViewModel.State);
        }

        [TestMethod]
        public void VsSessionHost_ActiveSectionChangedEvent()
        {
            // Setup
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();
            ISectionController otherSection = ConfigurableSectionController.CreateDefault();
            int changed = 0;
            testSubject.ActiveSectionChanged += (o, e) => changed++;

            // Act (1st set)
            testSubject.SetActiveSection(section);

            // Verify
            Assert.AreEqual(1, changed, "ActiveSectionChanged event was expected to fire");

            // Act (clear)
            testSubject.ClearActiveSection();

            // Verify
            Assert.AreEqual(2, changed, "ActiveSectionChanged event was expected to fire");

            // Act (2nd set)
            testSubject.SetActiveSection(otherSection);

            // Verify
            Assert.AreEqual(3, changed, "ActiveSectionChanged event was expected to fire");

            // Act (clear)
            testSubject.ClearActiveSection();

            // Verify
            Assert.AreEqual(4, changed, "ActiveSectionChanged event was expected to fire");

            // Act (clear again)
            testSubject.ClearActiveSection();

            // Verify
            Assert.AreEqual(4, changed, "ActiveSectionChanged event was not expected to fire, since already cleared");
        }

        [TestMethod]
        public void VsSessionHost_SyncCommandFromActiveSectionDuringActiveSectionChanges()
        {
            // Setup
            VsSessionHost testSubject = this.CreateTestSubject(null);
            ISectionController section = ConfigurableSectionController.CreateDefault();
            int syncCalled = 0;
            this.stateManager.SyncCommandFromActiveSectionAction = () => syncCalled++;

            // Case 1: SetActiveSection
            this.stateManager.ExpectActiveSection = true;

            // Act
            testSubject.SetActiveSection(section);

            // Verify
            Assert.AreEqual(1, syncCalled, "SyncCommandFromActiveSection wasn't called during section activation");

            // Case 2: ClearActiveSection section
            this.stateManager.ExpectActiveSection = false;

            // Act
            testSubject.ClearActiveSection();

            // Verify
            Assert.AreEqual(2, syncCalled, "SyncCommandFromActiveSection wasn't called during section deactivation");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_NoOpenSolutionScenario()
        {
            // Setup
            var tracker = new ConfigurableActiveSolutionTracker();
            this.CreateTestSubject(tracker);
            // Previous binding information that should be cleared once there's no solution
            var boundProject = new Integration.Service.ProjectInformation { Key = "bla" };
            this.stateManager.BoundProjectKey = boundProject.Key;
            this.stateManager.SetBoundProject(boundProject);

            // Sanity
            this.stateManager.AssertBoundProject(boundProject);
            this.stepRunner.AssertAbortAllCalled(0);

            // Act
            tracker.SimulateActiveSolutionChanged();

            // Verify
            this.stepRunner.AssertAbortAllCalled(1);
            this.stateManager.AssertNoBoundProject();
            Assert.IsNull(this.stateManager.BoundProjectKey, "Expecting the key to be reset to null");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_BoundSolutionWithNoActiveSectionScenario()
        {
            // Setup
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            var boundProject = new Integration.Service.ProjectInformation { Key = "bla" };
            this.solutionBinding.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://bound"), boundProject.Key);
            this.stateManager.SetBoundProject(boundProject);

            // Sanity
            this.stateManager.AssertBoundProject(boundProject);
            this.stepRunner.AssertAbortAllCalled(0);

            // Act (simulate solution opened event)
            tracker.SimulateActiveSolutionChanged();

            // Verify that nothing has changed (should defer all the work to when the section is connected)
            this.stepRunner.AssertAbortAllCalled(1);
            this.stateManager.AssertBoundProject(boundProject);
            Assert.IsNull(this.stateManager.BoundProjectKey, "The key should only be set when there's active section to allow marking it once fetched all the projects");

            // Act (set active section)
            var section = ConfigurableSectionController.CreateDefault();
            bool refreshCalled = false;
            section.RefreshCommand = new RelayCommand(() => refreshCalled = true);
            testSubject.SetActiveSection(section);

            // Verify (section has refreshed, no further aborts were required)
            this.stepRunner.AssertAbortAllCalled(1);
            Assert.AreEqual(boundProject.Key, this.stateManager.BoundProjectKey, "Key was not set, will not be able to mark project as bound after refresh");
            this.stateManager.AssertBoundProject(boundProject);
            Assert.IsTrue(refreshCalled, "Expected the refresh command to be called");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_BoundSolutionWithActiveSectionScenario()
        {
            // Setup
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            var boundProject = new Integration.Service.ProjectInformation { Key = "bla" };
            this.stateManager.SetBoundProject(boundProject);
            this.solutionBinding.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://bound"), boundProject.Key);
            var section = ConfigurableSectionController.CreateDefault();
            bool refreshCalled = false;
            section.RefreshCommand = new RelayCommand(() => refreshCalled = true);
            testSubject.SetActiveSection(section);

            // Sanity
            this.stateManager.AssertBoundProject(boundProject);
            this.stepRunner.AssertAbortAllCalled(0);

            // Act (simulate solution opened event)
            tracker.SimulateActiveSolutionChanged();

            // Verify
            this.stepRunner.AssertAbortAllCalled(1);
            Assert.AreEqual(boundProject.Key, this.stateManager.BoundProjectKey, "Key was not set, will not be able to mark project as bound after refresh");
            this.stateManager.AssertBoundProject(boundProject);
            Assert.IsTrue(refreshCalled, "Expected the refresh command to be called");
        }

        [TestMethod]
        public void VsSessionHost_ResetBinding_ErrorInReadingSolutionBinding()
        {
            // Setup
            var tracker = new ConfigurableActiveSolutionTracker();
            var testSubject = this.CreateTestSubject(tracker);
            var boundProject = new Integration.Service.ProjectInformation { Key = "bla" };
            this.stateManager.SetBoundProject(boundProject);
            this.solutionBinding.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://bound"), boundProject.Key);
            var section = ConfigurableSectionController.CreateDefault();
            testSubject.SetActiveSection(section);

            // Sanity
            this.stateManager.AssertBoundProject(boundProject);
            this.stepRunner.AssertAbortAllCalled(0);

            // Introduce an error
            this.solutionBinding.ReadSolutionBindingAction = () => { throw new Exception("boom"); };

            // Act (i.e. simulate loading a different solution)
            using (new AssertIgnoreScope()) // Ignore exception assert
            {
                tracker.SimulateActiveSolutionChanged();
            }

            // Verify
            this.stateManager.AssertNoBoundProject();
        }


        [TestMethod]
        public void VsSessionHost_IServiceProvider_GetService()
        {
            // Setup
            var testSubject = new VsSessionHost(this.serviceProvider, new Integration.Service.SonarQubeServiceWrapper(this.serviceProvider), new ConfigurableActiveSolutionTracker());
            ConfigurableVsShell shell = new ConfigurableVsShell();
            shell.RegisterPropertyGetter((int)__VSSPROPID2.VSSPROPID_InstallRootDir, () => this.TestContext.TestRunDirectory);
            this.serviceProvider.RegisterService(typeof(SVsShell), shell);

            // Local services
            // Act + Verify
            foreach (Type serviceType in VsSessionHost.SupportedLocalServices)
            {
                Assert.IsNotNull(testSubject.GetService(serviceType));
            }

            Assert.AreSame(testSubject.GetService<IFileSystem>(), testSubject.GetService<ISourceControlledFileSystem>());

            // VS-services
            // Sanity
            Assert.IsNull(testSubject.GetService(typeof(VsSessionHostTests)), "Not expecting any service at this point");

            // Setup
            this.serviceProvider.RegisterService(typeof(VsSessionHostTests), this);

            // Act + Verify
            Assert.AreSame(this, testSubject.GetService(typeof(VsSessionHostTests)), "Unexpected service was returned, expected to use the service provider");
        }
        #endregion

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

        #endregion
    }
}

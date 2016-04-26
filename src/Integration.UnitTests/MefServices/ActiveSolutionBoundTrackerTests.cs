//-----------------------------------------------------------------------
// <copyright file="ActiveSolutionBoundTrackerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Linq;
using System.Windows.Threading;

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
        public void ActiveSolutionBoundTracker_ArgChecls()
        {
            // Setup
            Exceptions.Expect<ArgumentNullException>(() =>
                new ActiveSolutionBoundTracker(null, new ConfigurableActiveSolutionTracker()));
            Exceptions.Expect<ArgumentNullException>(() =>
                new ActiveSolutionBoundTracker(this.host, null));
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Unbound()
        {
            // Setup
            host.VisualStateManager.ClearBoundProject();

            // Act
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker);

            // Verify
            Assert.IsFalse(testSubject.IsActiveSolutionBound, "Unbound solution should report false activation");
            this.errorListController.AssertRefreshCalled(1);
            this.errorListController.AssertResetCalled(0);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Bound()
        {
            // Setup
            this.solutionBindingInformationProvider.SolutionBound = true;
            this.host.VisualStateManager.SetBoundProject(new ProjectInformation());

            // Act
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker);

            // Verify
            Assert.IsTrue(testSubject.IsActiveSolutionBound, "Bound solution should report true activation");
            this.errorListController.AssertRefreshCalled(1);
            this.errorListController.AssertResetCalled(0);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Changes()
        {
            var solutionBinding = new ConfigurableSolutionBindingSerializer
            {
                CurrentBinding = new BoundSonarQubeProject()
            };
            this.serviceProvider.RegisterService(typeof(ISolutionBindingSerializer), solutionBinding);
            this.solutionBindingInformationProvider.SolutionBound = true;
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker);
            var reanalysisEventCalledCount = 0;
            testSubject.SolutionBindingChanged += (obj, args) => { reanalysisEventCalledCount++; };

            // Sanity
            Assert.IsTrue(testSubject.IsActiveSolutionBound, "Initially bound");
            this.errorListController.AssertRefreshCalled(1);
            this.errorListController.AssertResetCalled(0);
            Assert.AreEqual(0, reanalysisEventCalledCount, "No reanalysis forced");

            // Case 1: Clear bound project
            solutionBinding.CurrentBinding = null;
            this.solutionBindingInformationProvider.SolutionBound = false;
            // Act
            host.VisualStateManager.ClearBoundProject();

            // Verify
            Assert.IsFalse(testSubject.IsActiveSolutionBound, "Unbound solution should report false activation");
            this.errorListController.AssertRefreshCalled(1);
            this.errorListController.AssertResetCalled(0);
            Assert.AreEqual(1, reanalysisEventCalledCount, "Unbind should trigger reanalysis");

            // Case 2: Set bound project
            solutionBinding.CurrentBinding = new BoundSonarQubeProject();
            this.solutionBindingInformationProvider.SolutionBound = true;
            // Act
            host.VisualStateManager.SetBoundProject(new ProjectInformation());

            // Verify
            Assert.IsTrue(testSubject.IsActiveSolutionBound, "Bound solution should report true activation");
            this.errorListController.AssertRefreshCalled(1);
            this.errorListController.AssertResetCalled(0);
            Assert.AreEqual(2, reanalysisEventCalledCount, "Bind should trigger reanalysis");

            // Case 3: Solution unloaded
            solutionBinding.CurrentBinding = null;
            this.solutionBindingInformationProvider.SolutionBound = false;
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Verify
            Assert.IsFalse(testSubject.IsActiveSolutionBound, "Should respond to solution change event and report unbound");
            this.errorListController.AssertRefreshCalled(2);
            this.errorListController.AssertResetCalled(0);
            Assert.AreEqual(3, reanalysisEventCalledCount, "Solution change should trigger reanalysis");

            // Case 4: Solution loaded
            solutionBinding.CurrentBinding = new BoundSonarQubeProject();
            this.solutionBindingInformationProvider.SolutionBound = true;
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Verify
            Assert.IsTrue(testSubject.IsActiveSolutionBound, "Bound respond to solution change event and report bound");
            this.errorListController.AssertRefreshCalled(3);
            this.errorListController.AssertResetCalled(0);
            Assert.AreEqual(4, reanalysisEventCalledCount, "Solution change should trigger reanalysis");

            // Case 5: Dispose and change
            // Act
            testSubject.Dispose();
            solutionBinding.CurrentBinding = null;
            this.solutionBindingInformationProvider.SolutionBound = true;
            host.VisualStateManager.ClearBoundProject();

            // Verify
            Assert.AreEqual(4, reanalysisEventCalledCount, "Once disposed should stop raising the event");
            this.errorListController.AssertRefreshCalled(3);
            this.errorListController.AssertResetCalled(1);
        }
    }
}

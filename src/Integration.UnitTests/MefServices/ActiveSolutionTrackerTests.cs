//-----------------------------------------------------------------------
// <copyright file="ActiveSolutionTrackerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ActiveSolutionTrackerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock();
            this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);
        }

        [TestMethod]
        public void ActiveSolutionTracker_Dispose()
        {
            // Setup
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;
            testSubject.Dispose();

            // Act
            this.solutionMock.SimulateSolutionClose();
            this.solutionMock.SimulateSolutionOpen();

            // Verify
            Assert.AreEqual(0, counter, nameof(testSubject.ActiveSolutionChanged) + " was not expected to be raised since disposed");
        }

        [TestMethod]
        public void ActiveSolutionTracker_RaiseEventOnSolutionOpen()
        {
            // Setup
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;

            // Act
            this.solutionMock.SimulateSolutionOpen();

            // Verify
            Assert.AreEqual(1, counter, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
        }

        [TestMethod]
        public void ActiveSolutionTracker_RaiseEventOnSolutionClose()
        {
            // Setup
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;

            // Act
            this.solutionMock.SimulateSolutionClose();

            // Verify
            Assert.AreEqual(1, counter, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
        }

        [TestMethod]
        public void ActiveSolutionTracker_DontRaiseEventOnProjectChanges()
        {
            // Setup
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;
            var project = this.solutionMock.AddOrGetProject("project", isLoaded:false);

            // Act
            this.solutionMock.SimulateProjectLoad(project);
            this.solutionMock.SimulateProjectUnload(project);
            this.solutionMock.SimulateProjectOpen(project);
            this.solutionMock.SimulateProjectClose(project);

            // Verify
            Assert.AreEqual(0, counter, nameof(testSubject.ActiveSolutionChanged) + " was not expected to be raised");
        }
    }
}

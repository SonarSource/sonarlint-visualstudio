//-----------------------------------------------------------------------
// <copyright file="TransferableVisualStateTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.UnitTests.State
{
    [TestClass]
    public class TransferableVisualStateTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void TransferableVisualState_DefaultState()
        {
            // Setup
            var testSubject = new TransferableVisualState();

            // Verify
            Assert.IsFalse(testSubject.HasBoundProject);
            Assert.IsFalse(testSubject.IsBusy);
            Assert.IsNotNull(testSubject.ConnectedServers);
            Assert.AreEqual(0, testSubject.ConnectedServers.Count);
        }

        [TestMethod]
        public void TransferableVisualState_BoundProjectManagement()
        {
            // Setup
            var testSubject = new TransferableVisualState();
            var server = new ServerViewModel(new Integration.Service.ConnectionInformation(new System.Uri("http://server")));
            var project1 = new ProjectViewModel(server, new Integration.Service.ProjectInformation());
            var project2 = new ProjectViewModel(server, new Integration.Service.ProjectInformation());

            // Act (bind to something)
            testSubject.SetBoundProject(project1);

            // Verify
            Assert.IsTrue(testSubject.HasBoundProject);
            Assert.IsTrue(project1.IsBound);
            Assert.IsFalse(project2.IsBound);
            Assert.IsFalse(server.ShowAllProjects);

            // Act (bind to something else)
            testSubject.SetBoundProject(project2);

            // Verify
            Assert.IsTrue(testSubject.HasBoundProject);
            Assert.IsFalse(project1.IsBound);
            Assert.IsTrue(project2.IsBound);
            Assert.IsFalse(server.ShowAllProjects);

            // Act(clear binding)
            testSubject.ClearBoundProject();

            // Verify
            Assert.IsFalse(testSubject.HasBoundProject);
            Assert.IsFalse(project1.IsBound);
            Assert.IsFalse(project2.IsBound);
            Assert.IsTrue(server.ShowAllProjects);
        }
    }
}

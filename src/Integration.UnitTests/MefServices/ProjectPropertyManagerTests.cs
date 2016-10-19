//-----------------------------------------------------------------------
// <copyright file="ProjectPropertyManagerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectPropertyManagerTests
    {
        #region Test boilerplate

        private const string TestPropertyName = "MyTestProperty";

        private ConfigurableVsProjectSystemHelper projectSystem;
        private IHost host;

        [TestInitialize]
        public void TestInitialize()
        {
            var provider = new ConfigurableServiceProvider();
            this.projectSystem = new ConfigurableVsProjectSystemHelper(provider);

            provider.RegisterService(typeof(IProjectSystemHelper), projectSystem);
            this.host = new ConfigurableHost(provider, Dispatcher.CurrentDispatcher);
            var propertyManager = new ProjectPropertyManager(host);
            var mefModel = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);

            provider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #endregion

        #region Tests

        [TestMethod]
        public void ProjectPropertyManager_Ctor_NullArgChecks()
        {
            // Test case 1: missing IHost (MEF failure) throws exception
            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => new ProjectPropertyManager((IHost)null));

            // Test case 2: missing IHost's local services does not fail, only asserts
            // Setup
            var emptyHost = new ConfigurableHost(new ConfigurableServiceProvider(false), Dispatcher.CurrentDispatcher);
            using (new AssertIgnoreScope())
            {
                // Act + Verify
                new ProjectPropertyManager(emptyHost);
            }
        }

        [TestMethod]
        public void ProjectPropertyManager_GetSelectedProject_NoSelectedProjects_ReturnsEmpty()
        {
            // Setup
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act
            IEnumerable<Project> actualProjects = testSubject.GetSelectedProjects();

            // Verify
            Assert.IsFalse(actualProjects.Any(), "Expected no projects to be returned");
        }

        [TestMethod]
        public void ProjectPropertyManager_GetSelectedProjects_HasSelectedProjects_ReturnsProjects()
        {
            // Setup
            var p1 = new ProjectMock("p1.proj");
            var p2 = new ProjectMock("p2.proj");
            var p3 = new ProjectMock("p3.proj");
            p1.SetCSProjectKind();
            p2.SetVBProjectKind();
            // p3 is unknown kind
            var expectedProjects = new ProjectMock[] { p1, p2, p3 };
            this.projectSystem.SelectedProjects = expectedProjects;

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act
            Project[] actualProjects = testSubject.GetSelectedProjects().ToArray();

            // Verify
            CollectionAssert.AreEquivalent(expectedProjects, actualProjects, "Unexpected selected projects");
        }

        [TestMethod]
        public void ProjectPropertyManager_GetBooleanProperty()
        {
            // Setup
            var project = new ProjectMock("foo.proj");

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Test case 1: no property -> null
            // Setup
            project.ClearBuildProperty(TestPropertyName);

            // Act + Verify
            Assert.IsNull(testSubject.GetBooleanProperty(project, TestPropertyName), "Expected null for missing property value");

            // Test case 2: bad property -> null
            // Setup
            project.SetBuildProperty(TestPropertyName, "NotABool");

            // Act + Verify
            Assert.IsNull(testSubject.GetBooleanProperty(project, TestPropertyName), "Expected null for bad property value");

            // Test case 3: true property -> true
            // Setup
            project.SetBuildProperty(TestPropertyName, true.ToString());

            // Act + Verify
            Assert.IsTrue(testSubject.GetBooleanProperty(project, TestPropertyName).Value, "Expected true for 'true' property value");

            // Test case 4: false property -> false
            // Setup
            project.SetBuildProperty(TestPropertyName, false.ToString());

            // Act + Verify
            Assert.IsFalse(testSubject.GetBooleanProperty(project, TestPropertyName).Value, "Expected true for 'true' property value");
        }

        [TestMethod]
        public void ProjectPropertyManager_SetBooleanProperty()
        {
            // Setup
            var project = new ProjectMock("foo.proj");

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Test case 1: true -> property is set true
            // Setup
            testSubject.SetBooleanProperty(project, TestPropertyName, true);

            // Act + Verify
            Assert.AreEqual(true.ToString(), project.GetBuildProperty(TestPropertyName),
                ignoreCase: true, message: "Expected property value true for property true");

            // Test case 2: false -> property is set false
            // Setup
            testSubject.SetBooleanProperty(project, TestPropertyName, false);

            // Act + Verify
            Assert.AreEqual(false.ToString(), project.GetBuildProperty(TestPropertyName),
                ignoreCase: false, message: "Expected property value true for property true");

            // Test case 3: null -> property is cleared
            // Setup
            testSubject.SetBooleanProperty(project, TestPropertyName, null);

            // Act + Verify
            Assert.IsNull(project.GetBuildProperty(TestPropertyName), "Expected property value null for property false");
        }

        [TestMethod]
        public void ProjectPropertyManager_GetBooleanProperty_NullArgChecks()
        {
            // Setup
            var project = new ProjectMock("foo.proj");
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetBooleanProperty(null, "prop"));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetBooleanProperty(project, null));
        }

        [TestMethod]
        public void ProjectPropertyManager_SetBooleanProperty_NullArgChecks()
        {
            // Setup
            var project = new ProjectMock("foo.proj");
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetBooleanProperty(null, "prop", true));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetBooleanProperty(project, null, true));
        }

        #endregion

        #region Test helpers

        private ProjectPropertyManager CreateTestSubject()
        {
            return new ProjectPropertyManager(this.host);
        }

        #endregion
    }
}

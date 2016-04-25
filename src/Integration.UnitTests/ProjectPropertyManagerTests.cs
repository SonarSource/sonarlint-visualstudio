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
using System.Linq;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectPropertyManagerTests
    {
        #region Test boilerplate

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
            "S1848:Objects should not be created to be dropped immediately without being used",
            Justification = "Only testing constructor does not throw exception. Do not need to use the resulting instance.",
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Integration.UnitTests.ProjectPropertyManagerTests.ProjectPropertyManager_Ctor_NullArgChecks")]
        public void ProjectPropertyManager_Ctor_NullArgChecks()
        {
            // Test case 1: missing IHost (MEF failure) throws exception
            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => new ProjectPropertyManager((IHost)null));

            // Test case 2: missing IHost's local services does not fail, only asserts
            // Setup
            var host = new ConfigurableHost(new ConfigurableServiceProvider(false), Dispatcher.CurrentDispatcher);
            using (new AssertIgnoreScope())
            {
                // Act + Verify
                new ProjectPropertyManager(host);
            }
        }

        [TestMethod]
        public void ProjectPropertyManager_GetSelectedProjects()
        {
            // Setup
            var p1 = new ProjectMock("p1.proj");
            var p2 = new ProjectMock("p2.proj");
            var p3 = new ProjectMock("p3.proj");
            var expectedProjects = new ProjectMock[] { p1, p2, p3 };
            this.projectSystem.SelectedProjects = expectedProjects;

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act
            Project[] actualProjects = testSubject.GetSelectedProjects().ToArray();

            // Verify
            CollectionAssert.AreEquivalent(expectedProjects, actualProjects, "Unexpected selected projects");
        }

        [TestMethod]
        public void ProjectPropertyManager_GetExcludedProperty()
        {
            // Setup
            var project = new ProjectMock("foo.proj");

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Test case 1: no property -> false
            // Setup
            project.ClearBuildProperty(ProjectPropertyManager.ExcludeProperty);

            // Act + Verify
            Assert.IsFalse(testSubject.GetExcludedProperty(project), "Expected exclude false for missing exclude property value");

            // Test case 2: bad property -> false
            // Setup
            project.SetBuildProperty(ProjectPropertyManager.ExcludeProperty, "NotABool");

            // Act + Verify
            Assert.IsFalse(testSubject.GetExcludedProperty(project), "Expected exclude false for bad exclude property value");

            // Test case 3: true property -> true
            // Setup
            project.SetBuildProperty(ProjectPropertyManager.ExcludeProperty, true.ToString());

            // Act + Verify
            Assert.IsTrue(testSubject.GetExcludedProperty(project), "Expected exclude true for 'true' exclude property value");
        }

        [TestMethod]
        public void ProjectPropertyManager_SetExcludedProperty()
        {
            // Setup
            var project = new ProjectMock("foo.proj");

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Test case 1: false -> property is cleared
            // Setup
            testSubject.SetExcludedProperty(project, false);

            // Act + Verify
            Assert.IsNull(project.GetBuildProperty(ProjectPropertyManager.ExcludeProperty), "Expected property value null for exclude false");

            // Test case 2: true -> property is set true
            // Setup
            testSubject.SetExcludedProperty(project, true);

            // Act + Verify
            Assert.AreEqual(true.ToString(), project.GetBuildProperty(ProjectPropertyManager.ExcludeProperty),
                ignoreCase: true, message: "Expected property value true for exclude true");
        }

        [TestMethod]
        public void ProjectPropertyManager_GetTestProjectProperty()
        {
            // Setup
            var project = new ProjectMock("foo.proj");

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Test case 1: no property -> null
            // Setup
            project.ClearBuildProperty(ProjectPropertyManager.TestProperty);

            // Act + Verify
            Assert.IsNull(testSubject.GetTestProjectProperty(project), "Expected null for missing test property value");

            // Test case 2: bad property -> null
            // Setup
            project.SetBuildProperty(ProjectPropertyManager.TestProperty, "NotABool");

            // Act + Verify
            Assert.IsNull(testSubject.GetTestProjectProperty(project), "Expected null for bad test property value");

            // Test case 3: true property -> true
            // Setup
            project.SetBuildProperty(ProjectPropertyManager.TestProperty, true.ToString());

            // Act + Verify
            Assert.IsTrue(testSubject.GetTestProjectProperty(project).Value, "Expected true for 'true' test property value");

            // Test case 4: false property -> false 
            // Setup
            project.SetBuildProperty(ProjectPropertyManager.TestProperty, false.ToString());

            // Act + Verify
            Assert.IsFalse(testSubject.GetTestProjectProperty(project).Value, "Expected true for 'true' test property value");
        }

        [TestMethod]
        public void ProjectPropertyManager_SetTestProjectProperty()
        {
            // Setup
            var project = new ProjectMock("foo.proj");

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Test case 1: true -> property is set true
            // Setup
            testSubject.SetTestProjectProperty(project, true);

            // Act + Verify
            Assert.AreEqual(true.ToString(), project.GetBuildProperty(ProjectPropertyManager.TestProperty),
                ignoreCase: true, message: "Expected property value true for test property true");

            // Test case 2: false -> property is set false
            // Setup
            testSubject.SetTestProjectProperty(project, false);

            // Act + Verify
            Assert.AreEqual(false.ToString(), project.GetBuildProperty(ProjectPropertyManager.TestProperty),
                ignoreCase: false, message: "Expected property value true for test property true");

            // Test case 3: null -> property is cleared
            // Setup
            testSubject.SetTestProjectProperty(project, null);

            // Act + Verify
            Assert.IsNull(project.GetBuildProperty(ProjectPropertyManager.TestProperty), "Expected property value null for test property false");
        }

        [TestMethod]
        public void ProjectPropertyManager_GetExcludedProperty_NullArgChecks()
        {
            // Setup
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetExcludedProperty(null));
        }

        [TestMethod]
        public void ProjectPropertyManager_SetExcludedProperty_NullArgChecks()
        {
            // Setup
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetExcludedProperty(null, true));
        }

        [TestMethod]
        public void ProjectPropertyManager_GetTestProjectProperty_NullArgChecks()
        {
            // Setup
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetTestProjectProperty(null));
        }

        [TestMethod]
        public void ProjectPropertyManager_SetTestProjectProperty_NullArgChecks()
        {
            // Setup
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetTestProjectProperty(null, true));
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

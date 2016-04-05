//-----------------------------------------------------------------------
// <copyright file="ProjectSystemFilterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class ProjectSystemFilterTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystem;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystem);
        }

        #region Tests 

        [TestMethod]
        public void ProjectSystemFilter_IsAccepted_ArgumentChecks()
        {
            // Setup
            var testSubject = this.CreateTestSubject();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.IsAccepted(null));
        }

        [TestMethod]
        public void ProjectSystemFilter_IsAccepted_UnsupportedLanguage_IsFalse()
        {
            // Setup
            var testSubject = this.CreateTestSubject();

            var project = new ProjectMock("unsupported.vbproj");
            project.SetVBProjectKind();

            // Act
            var result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsFalse(result, "Project of unsupported language type should not be accepted");
        }

        [TestMethod]
        public void ProjectSystemFilter_IsAccepted_SonarExcludeProperty_ReturnsPropertyValue()
        {
            // Setup
            var testSubject = this.CreateTestSubject();

            var project = new ProjectMock("supported.csproj");
            project.SetCSProjectKind();

            // Test case 1: missing property-> is accepted
            // Act
            var result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsTrue(result, "Project with missing property SonarQubeExclude should be accepted");

            // Test case 2: property non-bool -> is accepted
            // Setup
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, string.Empty);

            // Act
            result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsTrue(result, "Project with non-bool property SonarQubeExclude should be accepted");

            // Test case 3: property non-bool, non-empty -> is accepted
            // Setup
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "abc");

            // Act
            result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsTrue(result, "Project with non-bool property SonarQubeExclude should be accepted");

            // Test case 4: property true -> not accepted
            // Setup
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "true");
            
            // Act
            result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsFalse(result, "Project with property SonarQubeExclude=false should NOT be accepted");

            // Test case 5: property false -> is accepted
            // Setup
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "false");

            // Act
            result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsTrue(result, "Project with property SonarQubeExclude=true should be accepted");
        }

        [TestMethod]
        public void ProjectSystemFilter_IsAccepted_SonarTestProjectProperty_ReturnsPropertyValue()
        {
            // Setup
            var testSubject = this.CreateTestSubject();

            var project = new ProjectMock("supported.csproj");
            project.SetCSProjectKind();

            // Test case 1: missing property -> accepted
            // Act
            var result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsTrue(result, "Project with missing property SonarQubeTestProject should be accepted");

            // Test case 2: non-bool property -> accepted
            // Setup
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, string.Empty);

            // Act
            result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsTrue(result, "Project with non-bool property SonarQubeTestProject should be accepted");

            // Test case 3: property true -> not accepted
            // Setup
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "true");

            // Act
            result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsFalse(result, "Project with property SonarQubeTestProject=false should NOT be accepted");

            // Test case 4: property false -> is accepted
            // Setup
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "false");

            // Act
            result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsTrue(result, "Project with property SonarQubeTestProject=true should be accepted");
        }

        [TestMethod]
        public void ProjectSystemFilter_IsAccepted_IsKnownTestProject_ReturnsFalse()
        {
            // Setup
            var testSubject = this.CreateTestSubject();

            var project = new ProjectMock("knownproject.csproj");
            project.SetCSProjectKind();
            project.SetTestProject();

            // Act
            var result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsFalse(result, "Project of known test project type should NOT be accepted");
        }

        [TestMethod]
        public void ProjectSystemFilter_IsAccepted_MatchesTestRegex_ReturnsFalse()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            testSubject.SetTestRegex(new Regex(".*barfoo.*", RegexOptions.None, TimeSpan.FromSeconds(1)));

            var project = new ProjectMock("foobarfoobar.csproj");
            project.SetCSProjectKind();

            // Act
            var result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsFalse(result, "Project with name that matches test regex should NOT be accepted");
        }

        [TestMethod]
        public void ProjectSystemFilter_IsAccepted_DoesNotMatchTestRegex_ReturnsTrue()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            testSubject.SetTestRegex(new Regex(".*notfound.*", RegexOptions.None, TimeSpan.FromSeconds(1)));

            var project = new ProjectMock("foobarfoobar.csproj");
            project.SetCSProjectKind();

            // Act
            var result = testSubject.IsAccepted(project);

            // Verify
            Assert.IsTrue(result, "Project with name that does not match test regex should be accepted");
        }

        #endregion

        #region Helpers

        private ProjectSystemFilter CreateTestSubject()
        {
            var host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            return new ProjectSystemFilter(host);
        }

        #endregion
    }
}

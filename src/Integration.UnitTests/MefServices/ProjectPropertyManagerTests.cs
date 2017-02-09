/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        #endregion Test boilerplate

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
            actualProjects.Any().Should().BeFalse("Expected no projects to be returned");
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
            testSubject.GetBooleanProperty(project, TestPropertyName).Should().BeNull("Expected null for missing property value");

            // Test case 2: bad property -> null
            // Setup
            project.SetBuildProperty(TestPropertyName, "NotABool");

            // Act + Verify
            testSubject.GetBooleanProperty(project, TestPropertyName).Should().BeNull("Expected null for bad property value");

            // Test case 3: true property -> true
            // Setup
            project.SetBuildProperty(TestPropertyName, true.ToString());

            // Act + Verify
            testSubject.GetBooleanProperty(project, TestPropertyName).Value.Should().BeTrue("Expected true for 'true' property value");

            // Test case 4: false property -> false
            // Setup
            project.SetBuildProperty(TestPropertyName, false.ToString());

            // Act + Verify
            testSubject.GetBooleanProperty(project, TestPropertyName).Value.Should().BeFalse("Expected true for 'true' property value");
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
            project.GetBuildProperty(TestPropertyName).Should().Be(true.ToString(), "Expected property value true for property true");

            // Test case 2: false -> property is set false
            // Setup
            testSubject.SetBooleanProperty(project, TestPropertyName, false);

            // Act + Verify
            project.GetBuildProperty(TestPropertyName).Should().Be(false.ToString(), "Expected property value true for property true");

            // Test case 3: null -> property is cleared
            // Setup
            testSubject.SetBooleanProperty(project, TestPropertyName, null);

            // Act + Verify
            project.GetBuildProperty(TestPropertyName).Should().BeNull("Expected property value null for property false");
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

        #endregion Tests

        #region Test helpers

        private ProjectPropertyManager CreateTestSubject()
        {
            return new ProjectPropertyManager(this.host);
        }

        #endregion Test helpers
    }
}
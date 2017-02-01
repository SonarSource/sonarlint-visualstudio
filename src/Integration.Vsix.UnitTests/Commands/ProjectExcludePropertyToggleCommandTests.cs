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

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Windows.Threading;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    [TestClass]
    public class ProjectExcludePropertyToggleCommandTests
    {
        #region Test boilerplate

        private ConfigurableVsProjectSystemHelper projectSystem;
        private IServiceProvider serviceProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            var provider = new ConfigurableServiceProvider();
            this.projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            provider.RegisterService(typeof(IProjectSystemHelper), this.projectSystem);

            var host = new ConfigurableHost(provider, Dispatcher.CurrentDispatcher);
            var propertyManager = new ProjectPropertyManager(host);
            var mefExports = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            provider.RegisterService(typeof(SComponentModel), mefModel);

            this.serviceProvider = provider;
        }

        #endregion

        #region Tests

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_Ctor()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ProjectExcludePropertyToggleCommand(null));
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_Invoke_SingleProject_TogglesValue()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var testSubject = new ProjectExcludePropertyToggleCommand(serviceProvider);
            var project = new ProjectMock("projecty.csproj");
            project.SetCSProjectKind();
            this.projectSystem.SelectedProjects = new[] { project };

            // Test case 1: true --toggle--> clears property
            this.SetExcludeProperty(project, true);

            // Act
            testSubject.Invoke(command, null);

            // Verify
            this.VerifyExcludeProperty(project, null);

            // Test case 2: no property --toggle--> true
            this.SetExcludeProperty(project, null);

            // Act
            testSubject.Invoke(command, null);

            // Verify
            this.VerifyExcludeProperty(project, true);
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_Invoke_MultipleProjects_ConsistentPropValues_TogglesValues()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var testSubject = new ProjectExcludePropertyToggleCommand(this.serviceProvider);

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();
            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            // Test case 1: all not set --toggle--> all true
            // Act
            testSubject.Invoke(command, null);

            // Verify
            this.VerifyExcludeProperty(p1, true);
            this.VerifyExcludeProperty(p2, true);

            // Test case 2: all true --toggle--> all not set
            // Setup
            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, true);

            // Act
            testSubject.Invoke(command, null);

            // Verify
            this.VerifyExcludeProperty(p1, null);
            this.VerifyExcludeProperty(p2, null);
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_Invoke_MultipleProjects_MixedPropValues_SetIsExcludedTrue()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var testSubject = new ProjectExcludePropertyToggleCommand(this.serviceProvider);

            var p1 = new ProjectMock("trueProj.proj");
            var p2 = new ProjectMock("nullProj.proj");
            var p3 = new ProjectMock("trueProj.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();
            p3.SetCSProjectKind();
            this.projectSystem.SelectedProjects = new[] { p1, p2, p3 };

            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, null);
            this.SetExcludeProperty(p3, false);

            // Act
            testSubject.Invoke(command, null);

            // Verify
            this.VerifyExcludeProperty(p1, true);
            this.VerifyExcludeProperty(p2, true);
            this.VerifyExcludeProperty(p3, true);
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MissingPropertyManager_IsDisabledIsHidden()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var localProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);

            ProjectExcludePropertyToggleCommand testSubject;
            using (new AssertIgnoreScope()) // we want to be missing the MEF service
            {
                testSubject = new ProjectExcludePropertyToggleCommand(localProvider);
            }

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_SingleProject_SupportedProject_IsEnabledIsVisible()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectExcludePropertyToggleCommand(serviceProvider);

            var project = new ProjectMock("mcproject.csproj");
            project.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { project };

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Enabled.Should().BeTrue("Expected command to be enabled");
            command.Visible.Should().BeTrue("Expected command to be visible");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_SingleProject_UnsupportedProject_IsDisabledIsHidden()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectExcludePropertyToggleCommand(serviceProvider);

            var project = new ProjectMock("mcproject.csproj");

            this.projectSystem.SelectedProjects = new[] { project };

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_SingleProject_CheckedStateReflectsValues()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectExcludePropertyToggleCommand(this.serviceProvider);

            var project = new ProjectMock("face.proj");
            project.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { project };

            // Test case 1: no property -> not checked
            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Checked.Should().BeFalse("Expected command to be unchecked");

            // Test case 1: true -> is checked
            this.SetExcludeProperty(project, true);

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Checked.Should().BeTrue("Expected command to be checked");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_ConsistentPropValues_CheckedStateReflectsValues()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectExcludePropertyToggleCommand(this.serviceProvider);

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            // Test case 1: no property -> not checked
            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Checked.Should().BeFalse("Expected command to be unchecked");

            // Test case 2: all true -> is checked
            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, true);

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Checked.Should().BeTrue("Expected command to be checked");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_MixedPropValues_IsUnchecked()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectExcludePropertyToggleCommand(this.serviceProvider);

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            var p3 = new ProjectMock("good3.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();
            p3.SetCSProjectKind();
            this.projectSystem.SelectedProjects = new[] { p1, p2, p3 };

            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, null);
            this.SetExcludeProperty(p3, false);

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Checked.Should().BeFalse("Expected command to be unchecked");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_AllSupportedProjects_IsEnabledIsVisible()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectExcludePropertyToggleCommand(this.serviceProvider);

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new [] { p1, p2 };

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Enabled.Should().BeTrue("Expected command to be enabled");
            command.Visible.Should().BeTrue("Expected command to be visible");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_MixedSupportedProject_IsDisabledIsHidden()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectExcludePropertyToggleCommand(this.serviceProvider);

            var unsupportedProject = new ProjectMock("bad.proj");
            var supportedProject = new ProjectMock("good.proj");
            supportedProject.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { unsupportedProject, supportedProject };

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        #endregion

        #region Test helpers

        private void VerifyExcludeProperty(ProjectMock project, bool? expected)
        {
            bool? actual = this.GetExcludeProperty(project);
            actual.Should().Be(expected);
        }

        private bool? GetExcludeProperty(ProjectMock project)
        {
            string valueString = project.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey);
            bool value;
            if (bool.TryParse(valueString, out value))
            {
                return value;
            }

            return null;
        }

        private void SetExcludeProperty(ProjectMock project, bool? value)
        {
            if (value.GetValueOrDefault(false))
            {
                project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, value.Value.ToString());
            }
            else
            {
                project.ClearBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey);
            }
        }

        #endregion
    }
}

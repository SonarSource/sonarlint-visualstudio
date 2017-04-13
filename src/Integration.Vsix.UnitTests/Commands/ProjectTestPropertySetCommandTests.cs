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
using System.Collections;
using System.Collections.Generic;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    [TestClass]
    public class ProjectTestPropertySetCommandTests
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

        #endregion Test boilerplate

        #region Tests

        [TestMethod]
        public void ProjectTestPropertySetCommand_Ctor()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ProjectTestPropertySetCommand(null, true));

            new ProjectTestPropertySetCommand(this.serviceProvider, null);
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_Invoke_SingleProject_SetsValue()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var project = new ProjectMock("project.csproj");
            project.SetCSProjectKind();
            this.projectSystem.SelectedProjects = new[] { project };

            // Test case 1: test (true)
            // Arrange
            var testSubject1 = new ProjectTestPropertySetCommand(serviceProvider, true);

            // Act
            testSubject1.Invoke(command, null);

            // Assert
            this.VerifyTestProperty(project, true);

            // Test case 2: non-test (false)
            var testSubject2 = new ProjectTestPropertySetCommand(serviceProvider, false);

            // Act
            testSubject2.Invoke(command, null);

            // Assert
            this.VerifyTestProperty(project, false);

            // Test case 3: auto detect (null)
            var testSubject3 = new ProjectTestPropertySetCommand(serviceProvider, null);

            // Act
            testSubject3.Invoke(command, null);

            // Assert
            this.VerifyTestProperty(project, null);
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_Invoke_MultipleProjects_MixedPropValues_SetsValues()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            var p3 = new ProjectMock("good2.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();
            p3.SetCSProjectKind();
            this.projectSystem.SelectedProjects = new[] { p1, p2, p3 };

            // Test case 1: test (true)
            // Arrange
            var testSubject1 = new ProjectTestPropertySetCommand(serviceProvider, true);
            this.SetTestProperty(p1, null);
            this.SetTestProperty(p2, true);
            this.SetTestProperty(p3, false);

            // Act
            testSubject1.Invoke(command, null);

            // Assert
            this.VerifyTestProperty(p1, true);
            this.VerifyTestProperty(p2, true);
            this.VerifyTestProperty(p3, true);

            // Test case 2: non-test (false)
            var testSubject2 = new ProjectTestPropertySetCommand(serviceProvider, false);
            this.SetTestProperty(p1, null);
            this.SetTestProperty(p2, true);
            this.SetTestProperty(p3, false);

            // Act
            testSubject2.Invoke(command, null);

            // Assert
            this.VerifyTestProperty(p1, false);
            this.VerifyTestProperty(p2, false);
            this.VerifyTestProperty(p3, false);

            // Test case 3: auto detect (null)
            var testSubject3 = new ProjectTestPropertySetCommand(serviceProvider, null);
            this.SetTestProperty(p1, null);
            this.SetTestProperty(p2, true);
            this.SetTestProperty(p3, false);

            // Act
            testSubject3.Invoke(command, null);

            // Assert
            this.VerifyTestProperty(p1, null);
            this.VerifyTestProperty(p2, null);
            this.VerifyTestProperty(p3, null);
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_QueryStatus_MissingPropertyManager_IsDisabledIsHidden()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var localProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);

            ProjectTestPropertySetCommand testSubject;
            using (new AssertIgnoreScope()) // we want to be missing the MEF service
            {
                testSubject = new ProjectTestPropertySetCommand(localProvider, null);
            }

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_QueryStatus_SingleProject_SupportedProject_IsEnabledIsVisible()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectTestPropertySetCommand(serviceProvider, null);

            var project = new ProjectMock("mcproject.csproj");
            project.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { project };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeTrue("Expected command to be enabled");
            command.Visible.Should().BeTrue("Expected command to be visible");
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_QueryStatus_SingleProject_UnsupportedProject_IsDisabledIsHidden()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectTestPropertySetCommand(serviceProvider, null);

            var project = new ProjectMock("mcproject.csproj");

            this.projectSystem.SelectedProjects = new[] { project };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_QueryStatus_SingleProject_CheckedStateReflectsValues()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var project = new ProjectMock("face.proj");
            project.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { project };

            foreach (ProjectTestPropertySetCommand testSubject in this.CreateCommands())
            {
                // Test case 1: property is null
                // Arrange
                this.SetTestProperty(project, null);

                // Act
                testSubject.QueryStatus(command, null);

                // Assert
                this.VerifyCommandCheckedStatus(command, testSubject, null);

                // Test case 2: property is true
                this.SetTestProperty(project, true);

                // Act
                testSubject.QueryStatus(command, null);

                // Assert
                this.VerifyCommandCheckedStatus(command, testSubject, true);

                // Test case 3: property is false
                this.SetTestProperty(project, false);

                // Act
                testSubject.QueryStatus(command, null);

                // Assert
                this.VerifyCommandCheckedStatus(command, testSubject, false);
            }
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_QueryStatus_MultipleProjects_ConsistentPropValues_CheckedStateReflectsValues()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            foreach (ProjectTestPropertySetCommand testSubject in this.CreateCommands())
            {
                // Test case 1: properties are both null
                // Arrange
                this.SetTestProperty(p1, null);
                this.SetTestProperty(p2, null);

                // Act
                testSubject.QueryStatus(command, null);

                // Assert
                this.VerifyCommandCheckedStatus(command, testSubject, null);

                // Test case 2: properties are both true
                this.SetTestProperty(p1, true);
                this.SetTestProperty(p2, true);

                // Act
                testSubject.QueryStatus(command, null);

                // Assert
                this.VerifyCommandCheckedStatus(command, testSubject, true);

                // Test case 3: properties are both false
                this.SetTestProperty(p1, false);
                this.SetTestProperty(p2, false);

                // Act
                testSubject.QueryStatus(command, null);

                // Assert
                this.VerifyCommandCheckedStatus(command, testSubject, false);
            }
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_QueryStatus_MultipleProjects_MixedPropValues_IsUnchecked()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            var p3 = new ProjectMock("good3.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();
            p3.SetCSProjectKind();
            this.projectSystem.SelectedProjects = new[] { p1, p2, p3 };

            this.SetTestProperty(p1, true);
            this.SetTestProperty(p2, null);
            this.SetTestProperty(p3, false);

            foreach (ProjectTestPropertySetCommand testSubject in this.CreateCommands())
            {
                // Act
                testSubject.QueryStatus(command, null);

                // Assert
                command.Checked.Should().BeFalse($"Expected command[{testSubject.CommandPropertyValue}] to be unchecked");
            }
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_QueryStatus_MultipleProjects_AllSupportedProjects_IsEnabledIsVisible()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectTestPropertySetCommand(this.serviceProvider, null);

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new [] { p1, p2 };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeTrue("Expected command to be enabled");
            command.Visible.Should().BeTrue("Expected command to be visible");
        }

        [TestMethod]
        public void ProjectTestPropertySetCommand_QueryStatus_MultipleProjects_MixedSupportedProjects_IsDisabledIsHidden()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectTestPropertySetCommand(this.serviceProvider, null);

            var unsupportedProject = new ProjectMock("bad.proj");
            var supportedProject = new ProjectMock("good.proj");
            supportedProject.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { unsupportedProject, supportedProject };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        #endregion Tests

        #region Test helpers

        private void VerifyTestProperty(ProjectMock project, bool? expected)
        {
            bool? actual = this.GetTestProperty(project);
            actual.Should().Be(expected, $"Expected property to be {expected}");
        }

        private bool? GetTestProperty(ProjectMock project)
        {
            string valueString = project.GetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey);
            bool value;
            if (bool.TryParse(valueString, out value))
            {
                return value;
            }

            return null;
        }

        private void SetTestProperty(ProjectMock project, bool? value)
        {
            if (value.HasValue)
            {
                project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, value.Value.ToString());
            }
            else
            {
                project.ClearBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey);
            }
        }

        private void VerifyCommandCheckedStatus(OleMenuCommand command, ProjectTestPropertySetCommand testSubject, bool? actualPropertyValue)
        {
            bool? testSubjectCmdValue = testSubject.CommandPropertyValue;
            bool propertySameAsCommand = (testSubjectCmdValue == actualPropertyValue);

            if (propertySameAsCommand)
            {
                command.Checked.Should().BeTrue($"Expected command[{testSubjectCmdValue}] to be checked when property is '{actualPropertyValue}'");
            }
            else
            {
                command.Checked.Should().BeFalse($"Expected command[{testSubjectCmdValue}] to be unchecked when property is '{actualPropertyValue}'");
            }
        }

        private TestProjectTestPropSetCommandWrapper CreateCommands()
        {
            return TestProjectTestPropSetCommandWrapper.Create(this.serviceProvider);
        }

        private class TestProjectTestPropSetCommandWrapper : IEnumerable<ProjectTestPropertySetCommand>
        {
            public ProjectTestPropertySetCommand NullCommand { get; }
            public ProjectTestPropertySetCommand TrueCommand { get; }
            public ProjectTestPropertySetCommand FalseCommand { get; }

            private TestProjectTestPropSetCommandWrapper(ProjectTestPropertySetCommand nullCmd,
                ProjectTestPropertySetCommand trueCmd,
                ProjectTestPropertySetCommand falseCmd)
            {
                this.NullCommand = nullCmd;
                this.TrueCommand = trueCmd;
                this.FalseCommand = falseCmd;
            }

            public static TestProjectTestPropSetCommandWrapper Create(IServiceProvider serviceProvider)
            {
                return new TestProjectTestPropSetCommandWrapper(
                    nullCmd: new ProjectTestPropertySetCommand(serviceProvider, null),
                    trueCmd: new ProjectTestPropertySetCommand(serviceProvider, true),
                    falseCmd: new ProjectTestPropertySetCommand(serviceProvider, false));
            }

            #region IEnumerable<ProjectTestPropertySetCommand>

            private IEnumerable<ProjectTestPropertySetCommand> AllCommands()
            {
                return new[] { this.NullCommand, this.TrueCommand, this.FalseCommand };
            }

            public IEnumerator<ProjectTestPropertySetCommand> GetEnumerator()
            {
                return this.AllCommands().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.AllCommands().GetEnumerator();
            }

            #endregion IEnumerable<ProjectTestPropertySetCommand>
        }

        #endregion Test helpers
    }
}
﻿//-----------------------------------------------------------------------
// <copyright file="ProjectExcludePropertyToggleCommandTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Windows.Threading;

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

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();
            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, null);

            // Act
            testSubject.Invoke(command, null);

            // Verify
            this.VerifyExcludeProperty(p1, true);
            this.VerifyExcludeProperty(p2, true);
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_SingleProject_Supported_IsEnabledIsVisible()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var testSubject = new ProjectExcludePropertyToggleCommand(serviceProvider);

            var project = new ProjectMock("mcproject.csproj");
            project.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { project };

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            Assert.IsTrue(command.Enabled, "Expected command to be enbled");
            Assert.IsTrue(command.Visible, "Expected command to be visible");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_SingleProject_Unsupported_IsDisabledIsHidden()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var testSubject = new ProjectExcludePropertyToggleCommand(serviceProvider);

            var project = new ProjectMock("mcproject.csproj");

            this.projectSystem.SelectedProjects = new[] { project };

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            Assert.IsFalse(command.Enabled, "Expected command to be disabled");
            Assert.IsFalse(command.Visible, "Expected command to be hidden");
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
            Assert.IsFalse(command.Checked, "Expected command to be unchecked");

            // Test case 1: true -> is checked
            this.SetExcludeProperty(project, true);

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            Assert.IsTrue(command.Checked, "Expected command to be checked");
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
            Assert.IsFalse(command.Checked, "Expected command to be unchecked");

            // Test case 2: all true -> is checked
            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, true);

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            Assert.IsTrue(command.Checked, "Expected command to be checked");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_MixedPropValues_IsUnchecked()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectExcludePropertyToggleCommand(this.serviceProvider);

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            p1.SetCSProjectKind();
            p2.SetCSProjectKind();
            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p1, null);

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            Assert.IsFalse(command.Checked, "Expected command to be unchecked");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_AllSupported_IsEnabledIsVisible()
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
            Assert.IsTrue(command.Enabled, "Expected command to be enabled");
            Assert.IsTrue(command.Visible, "Expected command to be visible");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_MixedSupport_IsDisabledIsHidden()
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
            Assert.IsFalse(command.Enabled, "Expected command to be disabled");
            Assert.IsFalse(command.Visible, "Expected command to be hidden");
        }

        #endregion

        #region Test helpers

        private void VerifyExcludeProperty(ProjectMock project, bool? expected)
        {
            bool? actual = this.GetExcludeProperty(project);
            Assert.AreEqual(expected, actual, $"Expected property to be {expected}");
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

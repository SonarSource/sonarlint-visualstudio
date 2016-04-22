//-----------------------------------------------------------------------
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
            var host = new ConfigurableHost(provider, Dispatcher.CurrentDispatcher);
            this.projectSystem = new ConfigurableVsProjectSystemHelper(provider);

            var mefExports = MefTestHelpers.CreateExport<IHost>(host);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);

            provider.RegisterService(typeof(IProjectSystemHelper), this.projectSystem);
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
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, true.ToString());

            // Act
            testSubject.Invoke(command, null);

            // Verify
            Assert.IsNull(project.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey), "Expected property to be cleared");

            // Test case 2: no property --toggle--> true
            project.ClearBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey);

            // Act
            testSubject.Invoke(command, null);

            // Verify
            bool propValue = bool.Parse(project.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey));
            Assert.IsTrue(propValue, "Expected property to be true");
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
            bool p1PropValue = bool.Parse(p1.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey));
            bool p2PropValue = bool.Parse(p2.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey));

            Assert.IsTrue(p1PropValue, "Expected exclusison property to be set true for project 1");
            Assert.IsTrue(p2PropValue, "Expected exclusison property to be set true for project 2");

            // Test case 2: all true --toggle--> all not set
            // Setup
            p1.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, true.ToString());
            p1.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, true.ToString());

            // Act
            testSubject.Invoke(command, null);

            // Verify
            Assert.IsNull(p1.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey),
                "Expected exclusison property to cleared for project 1");
            Assert.IsNull(p2.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey),
                "Expected exclusison property to cleared for project 2");
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

            p1.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, true.ToString());
            /* p2 property not set */

            // Act
            testSubject.Invoke(command, null);

            // Verify
            bool p1PropValue = bool.Parse(p1.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey));
            bool p2PropValue = bool.Parse(p1.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey));

            Assert.IsTrue(p1PropValue, "Expected exclusison property to be set true for project 1");
            Assert.IsTrue(p2PropValue, "Expected exclusison property to be set true for project 2");
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
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, true.ToString());

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
            p1.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, true.ToString());
            p2.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, true.ToString());

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

            p1.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, true.ToString());
            /* p2 property not set */

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
    }
}

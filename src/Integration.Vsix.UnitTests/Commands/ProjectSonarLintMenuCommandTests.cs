//-----------------------------------------------------------------------
// <copyright file="ProjectSonarLintMenuCommandTests.cs" company="SonarSource SA and Microsoft Corporation">
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
    public class ProjectSonarLintMenuCommandTests
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
        public void ProjectSonarLintMenuCommand_QueryStatus_NoProjects_IsDisableIsHidden()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectSonarLintMenuCommand(serviceProvider);
            
            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            Assert.IsFalse(command.Enabled, "Expected command to be disabled");
            Assert.IsFalse(command.Visible, "Expected command to be hidden");
        }

        [TestMethod]
        public void ProjectSonarLintMenuCommand_QueryStatus_HasUnsupportedProject_IsDisabledIsHidden()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectSonarLintMenuCommand(serviceProvider);

            var p1 = new ProjectMock("cs.proj");
            p1.SetCSProjectKind();
            var p2 = new ProjectMock("cpp.proj");

            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            Assert.IsFalse(command.Enabled, "Expected command to be disabled");
            Assert.IsFalse(command.Visible, "Expected command to be hidden");
        }

        [TestMethod]
        public void ProjectSonarLintMenuCommand_QueryStatus_AllSupportedProjects_IsEnabledIsVisible()
        {
            // Setup
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = new ProjectSonarLintMenuCommand(serviceProvider);

            var p1 = new ProjectMock("cs1.proj");
            p1.SetCSProjectKind();
            var p2 = new ProjectMock("cs2.proj");
            p2.SetCSProjectKind();

            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            // Act
            testSubject.QueryStatus(command, null);

            // Verify
            Assert.IsTrue(command.Enabled, "Expected command to be enabled");
            Assert.IsTrue(command.Visible, "Expected command to be visible");
        }

        #endregion
    }
}

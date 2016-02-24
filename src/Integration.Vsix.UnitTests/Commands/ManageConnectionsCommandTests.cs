//-----------------------------------------------------------------------
// <copyright file="ManageConnectionsCommandTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.ComponentModel.Design;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    [TestClass]
    public class ManageConnectionsCommandTests
    {
        [TestMethod]
        public void ManageConnectionsCommand_Ctor()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ManageConnectionsCommand(null));
        }

        [TestMethod]
        public void ManageConnectionsCommand_Invoke()
        {
            // Setup
            OleMenuCommand command = CreateRandomOleMenuCommand();
            var teController = new ConfigurableTeamExplorerController();
            var serviceProvider = CreateServiceProviderWithMefExports<ITeamExplorerController>(teController);

            var testSubject = new ManageConnectionsCommand(serviceProvider);

            // Test case 1: was disabled
            command.Enabled = false;

            // Act
            using (new AssertIgnoreScope()) // Invoked when disabled
            {
                testSubject.Invoke(command, null);
            }

            // Verify
            teController.AssertExpectedNumCallsShowConnectionsPage(0);
            

            // Test case 2: was enabled
            command.Enabled = true;

            // Act
            testSubject.Invoke(command, null);

            // Verify
            teController.AssertExpectedNumCallsShowConnectionsPage(1);
        }


        [TestMethod]
        public void ManageConnectionsCommand_QueryStatus()
        {
            // Setup
            OleMenuCommand command = CreateRandomOleMenuCommand();

            // Test case 1: no TE controller
            // Setup
            IServiceProvider sp1 = CreateServiceProviderWithEmptyComponentModel();
            command.Enabled = false;

            ManageConnectionsCommand testSubject1;
            using (new AssertIgnoreScope()) // TE service is missing from MEF
            {
                testSubject1 = new ManageConnectionsCommand(sp1);
            }

            // Act
            testSubject1.QueryStatus(command, null);

            // Verify
            Assert.IsFalse(command.Enabled, "Expected the command to be disabled on QueryStatus when no TE controller");


            // Test case 2: has TE controller
            // Setup
            var teController = new ConfigurableTeamExplorerController();
            var sp2 = CreateServiceProviderWithMefExports<ITeamExplorerController>(teController);

            var testSubject2 = new ManageConnectionsCommand(sp2);

            // Act
            testSubject2.QueryStatus(command, null);

            // Verify
            Assert.IsTrue(command.Enabled, "Expected the command to be disabled on QueryStatus when does have TE controller");
        }

        #region Helpers

        private static IServiceProvider CreateServiceProviderWithMefExports<T>(T instance)
        {
            var serviceProvider = new ConfigurableServiceProvider();
            var mefExports = MefTestHelpers.CreateExport<T>(instance);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);

            serviceProvider.RegisterService(typeof(SComponentModel), mefModel, replaceExisting: true);

            return serviceProvider;
        }

        private static IServiceProvider CreateServiceProviderWithEmptyComponentModel()
        {
            var serviceProvider = new ConfigurableServiceProvider();
            var mefModel = new ConfigurableComponentModel(serviceProvider);

            serviceProvider.RegisterService(typeof(SComponentModel), mefModel, replaceExisting: true);

            return serviceProvider;
        }

        private static Random random = new Random();
        private static OleMenuCommand CreateRandomOleMenuCommand()
        {
            return new OleMenuCommand((s, e) => { }, new CommandID(Guid.NewGuid(), random.Next()));
        }

        #endregion
    }
}

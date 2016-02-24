//-----------------------------------------------------------------------
// <copyright file="PackageCommandManagerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.ComponentModel.Design;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class PackageCommandManagerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableMenuCommandService menuService;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.menuService = new ConfigurableMenuCommandService();

            var teController = new ConfigurableTeamExplorerController();
            var mefExports = MefTestHelpers.CreateExport<ITeamExplorerController>(teController);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);

            this.serviceProvider.RegisterService(typeof(IMenuCommandService), this.menuService);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #region Tests

        [TestMethod]
        public void PackageCommandManager_Ctor_NullArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new PackageCommandManager(null));
        }

        [TestMethod]
        public void PackageCommandManager_Ctor_MissingMenuService_ThrowsException()
        {
            Exceptions.Expect<ArgumentException>(() => new PackageCommandManager(new ConfigurableServiceProvider(false)));
        }

        [TestMethod]
        public void PackageCommandManager_Initialize()
        {
            // Setup
            var testSubject = new PackageCommandManager(serviceProvider);
            var expectedCommandId = new CommandID(new Guid(CommonGuids.CommandSet), (int)PackageCommandId.ManageConnections);
            
            // Act
            testSubject.Initialize();

            // Verify
            var command = menuService.Commands.Single();
            Assert.AreEqual(expectedCommandId, command.Key, "Unexpected CommandID");
        }

        [TestMethod]
        public void PackageCommandManager_RegisterCommand()
        {
            // Setup
            int cmdId = 42;
            Guid cmdSetGuid = new Guid(CommonGuids.CommandSet);
            CommandID commandIdObject = new CommandID(cmdSetGuid, cmdId);
            var command = new ConfigurableVsCommand(serviceProvider);

            var testSubject = new PackageCommandManager(serviceProvider);

            // Act
            testSubject.RegisterCommand(cmdId, command);

            // Verify
            var registeredCommand = menuService.Commands.Single().Value;
            Assert.AreEqual(commandIdObject, registeredCommand.CommandID, $"Unexpected CommandID");
        }

        #endregion
    }
}

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
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Threading;

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
            this.serviceProvider.RegisterService(typeof(IMenuCommandService), this.menuService);

            var projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystem);

            var host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);

            var propManager = new ProjectPropertyManager(host);
            var propManagerExport = MefTestHelpers.CreateExport<IProjectPropertyManager>(propManager);

            var teController = new ConfigurableTeamExplorerController();
            var teExport = MefTestHelpers.CreateExport<ITeamExplorerController>(teController);

            var mefModel = ConfigurableComponentModel.CreateWithExports(teExport, propManagerExport);
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

            var cmdSet = new Guid(CommonGuids.CommandSet);
            IList<CommandID> allCommands = Enum.GetValues(typeof(PackageCommandId))
                                               .Cast<int>()
                                               .Select(x => new CommandID(cmdSet, x))
                                               .ToList();

            // Act
            testSubject.Initialize();

            // Verify
            Assert.AreEqual(allCommands.Count, menuService.Commands.Count, "Unexpected number of commands");

            IList<CommandID> missingCommands = allCommands.Except(menuService.Commands.Select(x => x.Key)).ToList();
            IEnumerable<string> missingCommandNames = missingCommands.Select(x => Enum.GetName(typeof(PackageCommandId), x));
            Assert.IsTrue(!missingCommands.Any(), $"Missing commands: {string.Join(", ", missingCommandNames)}");
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

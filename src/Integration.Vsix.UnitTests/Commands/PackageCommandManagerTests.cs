﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.IssueVisualization.Helpers;

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
        public void PackageCommandManager_Initialize()
        {
            // Arrange
            var testSubject = new PackageCommandManager(this.menuService);

            var cmdSet = new Guid(CommonGuids.SonarLintMenuCommandSet);
            IList<CommandID> allCommands = Enum.GetValues(typeof(PackageCommandId))
                                               .Cast<int>()
                                               .Select(x => new CommandID(cmdSet, x))
                                               .ToList();

            // Act
            testSubject.Initialize(serviceProvider.GetMefService<ITeamExplorerController>(),
                serviceProvider.GetMefService<IProjectPropertyManager>(),
                Mock.Of<IProjectToLanguageMapper>(),
                Mock.Of<IOutputWindowService>(),
                Mock.Of<IShowInBrowserService>(),
                Mock.Of<IBrowserService>());

            // Assert
            menuService.Commands.Should().HaveCountGreaterOrEqualTo(allCommands.Count, "Unexpected number of commands");

            IList<CommandID> missingCommands = allCommands.Except(menuService.Commands.Select(x => x.Key)).ToList();
            IEnumerable<string> missingCommandNames = missingCommands.Select(x => Enum.GetName(typeof(PackageCommandId), x));
            (!missingCommands.Any()).Should().BeTrue($"Missing commands: {string.Join(", ", missingCommandNames)}");
        }

        [TestMethod]
        [DataRow(CommonGuids.SonarLintMenuCommandSet)]
        [DataRow(CommonGuids.HelpMenuCommandSet)]
        public void PackageCommandManager_RegisterCommandInCorrectCommandSet(string cmdSet)
        {
            // Arrange
            int cmdId = 42;
            Guid cmdSetGuid = new Guid(cmdSet);
            CommandID commandIdObject = new CommandID(cmdSetGuid, cmdId);
            var command = new ConfigurableVsCommand();

            var testSubject = new PackageCommandManager(this.menuService);

            // Act
            testSubject.RegisterCommand(cmdSet, cmdId, command);

            // Assert
            var registeredCommand = menuService.Commands.Single().Value;
            registeredCommand.CommandID.Should().Be(commandIdObject, $"Unexpected CommandID");
        }

        [TestMethod]
        public void PackageCommandManager_RegisterCommandUsingDefaultCommandSet()
        {
            // Arrange
            int cmdId = 42;
            Guid cmdSetGuid = new Guid(CommonGuids.SonarLintMenuCommandSet);
            CommandID commandIdObject = new CommandID(cmdSetGuid, cmdId);
            var command = new ConfigurableVsCommand();

            var testSubject = new PackageCommandManager(this.menuService);

            // Act
            testSubject.RegisterCommand(cmdId, command);

            // Assert
            var registeredCommand = menuService.Commands.Single().Value;
            registeredCommand.CommandID.Should().Be(commandIdObject, $"Unexpected CommandID");
        }

        #endregion Tests
    }
}

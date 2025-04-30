/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Design;
using Microsoft.VisualStudio.ComponentModelHost;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.IssueVisualization.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class PackageCommandManagerTests
    {
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
            var testSubject = CreateTestSubject(out var menuService);

            var cmdSet = new Guid(CommonGuids.SonarLintMenuCommandSet);
            IList<CommandID> allCommands = Enum.GetValues(typeof(PackageCommandId))
                .Cast<int>()
                .Select(x => new CommandID(cmdSet, x))
                .ToList();

            var serviceProvider = GetMockedServiceProvider();

            // Act
            testSubject.Initialize(Substitute.For<PackageCommandManager.ShowOptionsPage>(), serviceProvider);

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

            var testSubject = CreateTestSubject(out var menuService);

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

            var testSubject = CreateTestSubject(out var menuService);

            // Act
            testSubject.RegisterCommand(cmdId, command);

            // Assert
            var registeredCommand = menuService.Commands.Single().Value;
            registeredCommand.CommandID.Should().Be(commandIdObject, $"Unexpected CommandID");
        }

        #endregion Tests

        private static PackageCommandManager CreateTestSubject(out ConfigurableMenuCommandService menuService)
        {
            menuService = new ConfigurableMenuCommandService();
            return new PackageCommandManager(menuService);
        }

        private IServiceProvider GetMockedServiceProvider()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var componentModel = Substitute.For<IComponentModel>();
            serviceProvider.GetService(typeof(SComponentModel)).Returns(componentModel);
            componentModel.GetExtensions<IConnectedModeUIManager>().Returns([Substitute.For<IConnectedModeUIManager>()]);
            componentModel.GetExtensions<IConnectedModeUIServices>().Returns([Substitute.For<IConnectedModeUIServices>()]);
            componentModel.GetExtensions<IActiveSolutionBoundTracker>().Returns([Substitute.For<IActiveSolutionBoundTracker>()]);
            componentModel.GetExtensions<IShowInBrowserService>().Returns([Substitute.For<IShowInBrowserService>()]);
            componentModel.GetExtensions<IOutputWindowService>().Returns([Substitute.For<IOutputWindowService>()]);
            componentModel.GetExtensions<IProjectPropertyManager>().Returns([Substitute.For<IProjectPropertyManager>()]);
            return serviceProvider;
        }
    }
}

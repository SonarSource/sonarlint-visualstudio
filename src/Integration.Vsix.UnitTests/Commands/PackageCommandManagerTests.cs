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

using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
 using Xunit;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class PackageCommandManagerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableMenuCommandService menuService;

        public PackageCommandManagerTests()
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

        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new PackageCommandManager(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Ctor_WithMissingMenuService_ThrowsArgumentException()
        {
            // Arrange + Act
            Action act = () => new PackageCommandManager(new ConfigurableServiceProvider(false));

            // Assert
            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void PackageCommandManager_Initialize()
        {
            // Arrange
            var testSubject = new PackageCommandManager(serviceProvider);

            var cmdSet = new Guid(CommonGuids.CommandSet);
            IList<CommandID> allCommands = Enum.GetValues(typeof(PackageCommandId))
                                               .Cast<int>()
                                               .Select(x => new CommandID(cmdSet, x))
                                               .ToList();

            // Act
            testSubject.Initialize();

            // Assert
            allCommands.Count.Should().Be( menuService.Commands.Count, "Unexpected number of commands");

            IList<CommandID> missingCommands = allCommands.Except(menuService.Commands.Select(x => x.Key)).ToList();
            IEnumerable<string> missingCommandNames = missingCommands.Select(x => Enum.GetName(typeof(PackageCommandId), x));
            missingCommands.Any().Should().BeTrue($"Missing commands: {string.Join(", ", missingCommandNames)}");
        }

        [Fact]
        public void PackageCommandManager_RegisterCommand()
        {
            // Arrange
            int cmdId = 42;
            Guid cmdSetGuid = new Guid(CommonGuids.CommandSet);
            CommandID commandIdObject = new CommandID(cmdSetGuid, cmdId);
            var command = new ConfigurableVsCommand(serviceProvider);

            var testSubject = new PackageCommandManager(serviceProvider);

            // Act
            testSubject.RegisterCommand(cmdId, command);

            // Assert
            var registeredCommand = menuService.Commands.Single().Value;
            commandIdObject.Should().Be( registeredCommand.CommandID, $"Unexpected CommandID");
        }

        #endregion
    }
}

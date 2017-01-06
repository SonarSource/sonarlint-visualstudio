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

using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.ComponentModel.Design;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class PackageCommandManager
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IMenuCommandService menuService;

        public PackageCommandManager(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;

            this.menuService = this.serviceProvider.GetService<IMenuCommandService>();
            if (this.menuService == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.MissingService, nameof(IMenuCommandService)), nameof(serviceProvider));
            }
        }

        public void Initialize()
        {
            // Buttons
            this.RegisterCommand((int)PackageCommandId.ManageConnections, new ManageConnectionsCommand(this.serviceProvider));
            this.RegisterCommand((int)PackageCommandId.ProjectExcludePropertyToggle, new ProjectExcludePropertyToggleCommand(this.serviceProvider));
            this.RegisterCommand((int)PackageCommandId.ProjectTestPropertyAuto, new ProjectTestPropertySetCommand(this.serviceProvider, null));
            this.RegisterCommand((int)PackageCommandId.ProjectTestPropertyTrue, new ProjectTestPropertySetCommand(this.serviceProvider, true));
            this.RegisterCommand((int)PackageCommandId.ProjectTestPropertyFalse, new ProjectTestPropertySetCommand(this.serviceProvider, false));

            // Menus
            this.RegisterCommand((int)PackageCommandId.ProjectSonarLintMenu, new ProjectSonarLintMenuCommand(this.serviceProvider));
        }

        internal /* testing purposes */ OleMenuCommand RegisterCommand(int commandId, VsCommandBase command)
        {
            return this.AddCommand(new Guid(CommonGuids.CommandSet), commandId, command.Invoke, command.QueryStatus);
        }

        private OleMenuCommand AddCommand(Guid commandGroupGuid, int commandId, EventHandler invokeHandler, EventHandler beforeQueryStatus)
        {
            CommandID idObject = new CommandID(commandGroupGuid, commandId);
            OleMenuCommand command = new OleMenuCommand(invokeHandler, delegate { }, beforeQueryStatus, idObject);
            this.menuService.AddCommand(command);
            return command;
        }
    }
}

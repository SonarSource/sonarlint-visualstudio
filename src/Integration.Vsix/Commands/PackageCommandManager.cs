/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.Vsix.Commands;
using SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu;
using SonarLint.VisualStudio.Integration.Vsix.Commands.HelpMenu;
using SonarLint.VisualStudio.IssueVisualization.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class PackageCommandManager
    {
        internal delegate void ShowOptionsPage(Type optionsPageToOpen);

        private readonly IMenuCommandService menuService;

        public PackageCommandManager(IMenuCommandService menuService)
        {
            this.menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
        }

        public void Initialize(ITeamExplorerController teamExplorerController,
            IProjectPropertyManager projectPropertyManager,
            IOutputWindowService outputWindowService,
            IShowInBrowserService showInBrowserService,
            IBrowserService browserService,
            ShowOptionsPage showOptionsPage,
            IConnectedModeServices connectedModeServices,
            IConnectedModeBindingServices connectedModeBindingServices)
        {
            RegisterCommand((int)PackageCommandId.ProjectExcludePropertyToggle, new ProjectExcludePropertyToggleCommand(projectPropertyManager));
            RegisterCommand((int)PackageCommandId.ProjectTestPropertyAuto, new ProjectTestPropertySetCommand(projectPropertyManager, null));
            RegisterCommand((int)PackageCommandId.ProjectTestPropertyTrue, new ProjectTestPropertySetCommand(projectPropertyManager, true));
            RegisterCommand((int)PackageCommandId.ProjectTestPropertyFalse, new ProjectTestPropertySetCommand(projectPropertyManager, false));

            // Menus
            RegisterCommand((int)PackageCommandId.ProjectSonarLintMenu, new ProjectSonarLintMenuCommand(projectPropertyManager));

            // Commands
            RegisterCommand(CommonGuids.SonarLintMenuCommandSet, OptionsCommand.Id, new OptionsCommand(showOptionsPage));

            // Help menu buttons
            RegisterCommand(CommonGuids.HelpMenuCommandSet, ShowLogsCommand.Id, new ShowLogsCommand(outputWindowService));
            RegisterCommand(CommonGuids.HelpMenuCommandSet, ViewDocumentationCommand.Id, new ViewDocumentationCommand(showInBrowserService));
            RegisterCommand(CommonGuids.HelpMenuCommandSet, AboutCommand.Id, new AboutCommand(browserService));
            RegisterCommand(CommonGuids.HelpMenuCommandSet, ShowCommunityPageCommand.Id, new ShowCommunityPageCommand(showInBrowserService));

            // Connected mode buttons
            RegisterCommand(CommonGuids.ConnectedModeMenuCommandSet, ManageConnectionsCommand.Id, new ManageConnectionsCommand(teamExplorerController));
            RegisterCommand(CommonGuids.ConnectedModeMenuCommandSet, NewConnectedModeCommand.Id, new NewConnectedModeCommand(connectedModeServices, connectedModeBindingServices));
            RegisterCommand(CommonGuids.ConnectedModeMenuCommandSet, SaveSharedConnectionCommand.Id, new SaveSharedConnectionCommand(connectedModeServices.ConfigurationProvider, connectedModeBindingServices.SharedBindingConfigProvider));
        }

        internal /* testing purposes */ OleMenuCommand RegisterCommand(int commandId, VsCommandBase command)
        {
            return RegisterCommand(CommonGuids.SonarLintMenuCommandSet, commandId, command);
        }

        internal /* testing purposes */ OleMenuCommand RegisterCommand(string commandSetGuid, int commandId, VsCommandBase command)
        {
            return AddCommand(new Guid(commandSetGuid), commandId, command.Invoke, command.QueryStatus);
        }

        private OleMenuCommand AddCommand(Guid commandGroupGuid, int commandId, EventHandler invokeHandler, EventHandler beforeQueryStatus)
        {
            var idObject = new CommandID(commandGroupGuid, commandId);
            var command = new OleMenuCommand(invokeHandler, delegate { }, beforeQueryStatus, idObject);

            menuService.AddCommand(command);

            return command;
        }
    }
}

﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl;
using ErrorHandler = SonarLint.VisualStudio.Core.ErrorHandler;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Commands
{
    internal sealed class IssueVisualizationToolWindowCommand
    {
        public static IssueVisualizationToolWindowCommand Instance { get; private set; }

        private readonly IToolWindowService toolWindowService;
        private readonly IVsMonitorSelection monitorSelection;
        private readonly ILogger logger;
        private readonly uint uiContextCookie;

        internal readonly OleMenuCommand ErrorListMenuItem;

        internal IssueVisualizationToolWindowCommand(IToolWindowService toolWindowService, IMenuCommandService commandService, IVsMonitorSelection monitorSelection, ILogger logger)
        {
            this.toolWindowService = toolWindowService ?? throw new ArgumentNullException(nameof(toolWindowService));
            this.monitorSelection = monitorSelection ?? throw new ArgumentNullException(nameof(monitorSelection));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (commandService == null)
            {
                throw new ArgumentNullException(nameof(commandService));
            }

            var menuCommandId = new CommandID(Constants.CommandSetGuid, Constants.ViewToolWindowCommandId);
            var menuItem = new MenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);

            menuCommandId = new CommandID(Constants.CommandSetGuid, Constants.ErrorListCommandId);
            // We're showing the command in two places in the UI, but we only do a status check when it's called from the Error List context menu.
            ErrorListMenuItem = new OleMenuCommand(Execute, null, ErrorListQueryStatus, menuCommandId);
            commandService.AddCommand(ErrorListMenuItem);

            var uiContextGuid = new Guid(Constants.UIContextGuid);
            monitorSelection.GetCmdUIContextCookie(ref uiContextGuid, out uiContextCookie);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            var componentModel = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var monitorSelection = await package.GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var toolWindowService = componentModel.GetService<IToolWindowService>();
            var logger = componentModel.GetService<ILogger>();

            Instance = new IssueVisualizationToolWindowCommand(toolWindowService, commandService, monitorSelection, logger);
        }

        internal void ErrorListQueryStatus(object sender, EventArgs e)
        {
            try
            {
                if (monitorSelection.IsCmdUIContextActive(uiContextCookie, out var isActive) == VSConstants.S_OK)
                {
                    ErrorListMenuItem.Visible = isActive == 1;
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_QueryStatusVisualizationToolWindowCommand, ex));
            }
        }

        internal void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                toolWindowService.Show(IssueVisualizationToolWindow.ToolWindowId);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_VisualizationToolWindow_Exception, ex));
            }
        }
    }
}

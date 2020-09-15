/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl;
using ErrorHandler = SonarLint.VisualStudio.Core.ErrorHandler;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Commands
{
    internal sealed class IssueVisualizationToolWindowCommand
    {
        public static readonly Guid CommandSet = Constants.CommandSetGuid;
        public const int ViewToolWindowCommandId = 0x0100;
        public const int ErrorListCommandId = 0x0200;

        public static IssueVisualizationToolWindowCommand Instance { get; private set; }

        private readonly AsyncPackage package;
        private readonly IVsMonitorSelection monitorSelection;
        private readonly ILogger logger;
        private readonly uint uiContextCookie;

        internal readonly OleMenuCommand ErrorListMenuItem;

        internal IssueVisualizationToolWindowCommand(AsyncPackage package, IMenuCommandService commandService, IVsMonitorSelection monitorSelection, ILogger logger)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.monitorSelection = monitorSelection ?? throw new ArgumentNullException(nameof(monitorSelection));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (commandService == null)
            {
                throw new ArgumentNullException(nameof(commandService));
            }

            var menuCommandId = new CommandID(CommandSet, ViewToolWindowCommandId);
            var menuItem = new MenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);

            menuCommandId = new CommandID(CommandSet, ErrorListCommandId);
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
            var logger = componentModel.GetService<ILogger>(); 

            Instance = new IssueVisualizationToolWindowCommand(package, commandService, monitorSelection, logger);
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
                var window = package.FindToolWindow(typeof(IssueVisualizationToolWindow), 0, true);

                if (window?.Frame == null)
                {
                    logger.WriteLine(Resources.ERR_VisualizationToolWindow_NoFrame);
                }
                else
                {
                    var vsWindowFrame = (IVsWindowFrame) window.Frame;
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(vsWindowFrame.Show());
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_VisualizationToolWindow_Exception, ex));
            }
        }
    }
}

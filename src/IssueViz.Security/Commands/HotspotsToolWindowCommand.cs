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
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Commands
{
    internal sealed class HotspotsToolWindowCommand
    {
        public static readonly Guid CommandSet = Constants.CommandSetGuid;
        public const int ViewToolWindowCommandId = 0x0100;

        public static HotspotsToolWindowCommand Instance { get; set; }

        private readonly IToolWindowService toolWindowService;
        private readonly ILogger logger;

        internal HotspotsToolWindowCommand(IToolWindowService toolWindowService, IMenuCommandService commandService, ILogger logger)
        {
            this.toolWindowService = toolWindowService ?? throw new ArgumentNullException(nameof(toolWindowService));

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (commandService == null)
            {
                throw new ArgumentNullException(nameof(commandService));
            }

            var menuCommandId = new CommandID(CommandSet, ViewToolWindowCommandId);
            var menuItem = new MenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            var componentModel = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var windowService = componentModel.GetService<IToolWindowService>();
            var logger = componentModel.GetService<ILogger>();

            Instance = new HotspotsToolWindowCommand(windowService, commandService, logger);
        }

        internal /* for testing */ void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                toolWindowService.Show(HotspotsToolWindow.ToolWindowId);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_HotspotsToolWindow_Exception, ex));
            }
        }
    }
}

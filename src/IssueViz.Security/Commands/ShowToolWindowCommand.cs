/*
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Commands
{
    internal sealed class ShowToolWindowCommand
    {
        private readonly Guid toolWindowId;

        private readonly IToolWindowService toolWindowService;
        private readonly ILogger logger;

        internal ShowToolWindowCommand(CommandID commandId, Guid toolWindowId,
            IToolWindowService toolWindowService, IMenuCommandService commandService, ILogger logger)
        {
            this.toolWindowId = toolWindowId;

            this.toolWindowService = toolWindowService ?? throw new ArgumentNullException(nameof(toolWindowService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (commandService == null)
            {
                throw new ArgumentNullException(nameof(commandService));
            }

            var menuItem = new MenuCommand(Execute, commandId);
            commandService.AddCommand(menuItem);
        }

        public static async Task<ShowToolWindowCommand> CreateAsync(AsyncPackage package, CommandID commandID, Guid toolWindowId)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            var componentModel = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var windowService = componentModel.GetService<IToolWindowService>();
            var loggerService = componentModel.GetService<ILogger>();

            return new ShowToolWindowCommand(commandID, toolWindowId, windowService, commandService, loggerService);
        }

        internal /* for testing */ void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                toolWindowService.Show(toolWindowId);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_ShowToolWindow_Exception, toolWindowId, ex));
            }
        }
    }
}

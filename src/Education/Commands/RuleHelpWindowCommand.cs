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
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;

using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Education.Commands
{
    internal sealed class RuleHelpWindowCommand
    {
        public static RuleHelpWindowCommand Instance { get; private set; }

        private const int commandId = 0x100;
        private readonly IToolWindowService toolWindowService;
        private readonly ILogger logger;

        internal static readonly Guid commandSet = new Guid("80127033-1819-4996-8C45-E9C96F75E2A8");

        internal RuleHelpWindowCommand(IMenuCommandService commandService, IToolWindowService toolWindowService, ILogger logger)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            this.toolWindowService = toolWindowService ?? throw new ArgumentNullException(nameof(toolWindowService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var menuCommandID = new CommandID(commandSet, commandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);

            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            var componentModel = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;

            var toolWindowService = componentModel.GetService<IToolWindowService>();
            var logger = componentModel.GetService<ILogger>();

            Instance = new RuleHelpWindowCommand(commandService, toolWindowService, logger);
        }

        internal void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                toolWindowService.Show(RuleHelpToolWindow.ToolWindowId);
            }
            catch (Exception ex) when (!Core.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_RuleHelpToolWindow_Exception, ex));
            }
        }
    }
}

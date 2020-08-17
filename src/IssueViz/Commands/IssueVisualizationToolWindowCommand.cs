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
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Commands
{
    internal sealed class IssueVisualizationToolWindowCommand
    {
        public static readonly Guid CommandSet = Constants.CommandSetGuid;
        public const int CommandId = 0x0100;

        public static IssueVisualizationToolWindowCommand Instance { get; private set; }

        private readonly AsyncPackage package;
        private readonly ILogger logger;

        internal IssueVisualizationToolWindowCommand(AsyncPackage package, IMenuCommandService commandService, ILogger logger)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            var componentModel = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var logger = componentModel.GetService<ILogger>(); 

            Instance = new IssueVisualizationToolWindowCommand(package, commandService, logger);
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

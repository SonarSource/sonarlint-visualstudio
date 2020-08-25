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
using SonarLint.VisualStudio.IssueVisualization.Selection;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Commands
{
    internal sealed class NavigationCommands
    {
        internal const int NextLocationCommandId = 0x1021;
        internal const int PreviousLocationCommandId = 0x1022;

        public static readonly Guid CommandSet = Constants.CommandSetGuid;
        public static NavigationCommands Instance { get; private set; }

        private readonly IAnalysisIssueNavigation analysisIssueNavigation;
        private readonly ILogger logger;

        internal NavigationCommands(IMenuCommandService commandService, IAnalysisIssueNavigation analysisIssueNavigation, ILogger logger)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            this.analysisIssueNavigation = analysisIssueNavigation ?? throw new ArgumentNullException(nameof(analysisIssueNavigation));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var menuCommandID = new CommandID(CommandSet, NextLocationCommandId);
            var menuItem = new MenuCommand(ExecuteGotoNextLocation, menuCommandID);
            commandService.AddCommand(menuItem);

            menuCommandID = new CommandID(CommandSet, PreviousLocationCommandId);
            menuItem = new MenuCommand(ExecuteGotoPreviousLocation, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            var componentModel = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var navigationService = componentModel.GetService<IAnalysisIssueNavigation>();
            var logger = componentModel.GetService<ILogger>();

            Instance = new NavigationCommands(commandService, navigationService, logger);
        }

        internal void ExecuteGotoNextLocation(object sender, EventArgs e)
        {
            SafeNavigate(() => analysisIssueNavigation.GotoNextNavigableLocation());
        }

        internal void ExecuteGotoPreviousLocation(object sender, EventArgs e)
        {
            SafeNavigate(() => analysisIssueNavigation.GotoPreviousNavigableLocation());
        }

        private void SafeNavigate(Action op)
        {
            // We're on the UI thread so unhandled exceptions will crash VS
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                op();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_NavigationException, ex));
            }
        }
    }
}

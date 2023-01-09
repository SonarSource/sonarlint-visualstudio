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
        public static NavigationCommands Instance { get; private set; }

        private readonly IIssueFlowStepNavigator issueFlowStepNavigator;
        private readonly ILogger logger;

        internal NavigationCommands(IMenuCommandService commandService, IIssueFlowStepNavigator issueFlowStepNavigator, ILogger logger)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            this.issueFlowStepNavigator = issueFlowStepNavigator ?? throw new ArgumentNullException(nameof(issueFlowStepNavigator));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var menuCommandID = new CommandID(Constants.CommandSetGuid, Constants.NextLocationCommandId);
            var menuItem = new MenuCommand(ExecuteGotoNextNavigableFlowStep, menuCommandID);
            commandService.AddCommand(menuItem);

            menuCommandID = new CommandID(Constants.CommandSetGuid, Constants.PreviousLocationCommandId);
            menuItem = new MenuCommand(ExecuteGotoPreviousNavigableFlowStep, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            var componentModel = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var issueFlowStepNavigator = componentModel.GetService<IIssueFlowStepNavigator>();
            var logger = componentModel.GetService<ILogger>();

            Instance = new NavigationCommands(commandService, issueFlowStepNavigator, logger);
        }

        internal void ExecuteGotoNextNavigableFlowStep(object sender, EventArgs e)
        {
            SafeNavigate(() => issueFlowStepNavigator.GotoNextNavigableFlowStep());
        }

        internal void ExecuteGotoPreviousNavigableFlowStep(object sender, EventArgs e)
        {
            SafeNavigate(() => issueFlowStepNavigator.GotoPreviousNavigableFlowStep());
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

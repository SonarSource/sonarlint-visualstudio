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
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    internal sealed class MuteIssueCommand
    {
        // Command set guid and command id. Must match those in DaemonCommands.vsct
        public static readonly Guid CommandSet = new Guid("1F83EA11-3B07-45B3-BF39-307FD4F42194");
        public const int CommandId = 0x0400;
        
        private readonly OleMenuCommand menuItem;
        
        public static MuteIssueCommand Instance { get; private set; }
        
        public static async Task InitializeAsync(AsyncPackage package, ILogger logger)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            IMenuCommandService commandService = (IMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));
            Instance = new MuteIssueCommand(commandService);
        }
        
        internal MuteIssueCommand(IMenuCommandService menuCommandService)
        {
            if (menuCommandService == null)
            {
                throw new ArgumentNullException(nameof(menuCommandService));
            }


            var menuCommandID = new CommandID(CommandSet, CommandId);
            menuItem = new OleMenuCommand(Execute, null, QueryStatus, menuCommandID);
            menuCommandService.AddCommand(menuItem);
        }

        private void QueryStatus(object sender, EventArgs e)
        {
            menuItem.Visible = true;
            menuItem.Enabled = true;
        }

        private void Execute(object sender, EventArgs e)
        {
            // will be implemented later
        }
    }
}

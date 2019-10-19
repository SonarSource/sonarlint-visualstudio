/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using System;
using System.ComponentModel.Design;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DisableRuleCommand
    {
        /// <summary>
        /// Command ID. 
        /// </summary>
        public const int CommandId = 0x0200;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1F83EA11-3B07-45B3-BF39-307FD4F42194");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private readonly OleMenuCommand menuItem;


        private readonly IErrorList errorList;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisableRuleCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private DisableRuleCommand(AsyncPackage package, IMenuCommandService commandService, IErrorList errorList)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            this.errorList = errorList;

            var menuCommandID = new CommandID(CommandSet, CommandId);
            menuItem = new OleMenuCommand(Execute, ChangeHandler, QueryStatus, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        private void ChangeHandler(object sender, EventArgs args)
        {
            // TODO
        }

        private void QueryStatus(object sender, EventArgs args)
        {
            bool status = TryGetErrorCodeSync(out _);
            menuItem.Enabled = status;
            menuItem.Visible = status;
        }

        private bool TryGetErrorCodeSync(out string errorCode)
        {
            errorCode = null;
            var selectedItems = errorList?.TableControl?.SelectedEntries;
            if (selectedItems?.Count() == 1)
            {
                var handle = selectedItems.First();

                errorCode = FindErrorCodeForEntry(handle);
            }
            return errorCode != null;
        }

        private string FindErrorCodeForEntry(ITableEntryHandle handle)
        {
            if (handle.TryGetSnapshot(out var snapshot, out int index) &&
                    snapshot.TryGetValue(index, StandardTableKeyNames.BuildTool, out var buildToolObj) &&
                    buildToolObj is string buildTool &&
                    buildTool.Equals("SonarLint") &&
                    snapshot.TryGetValue(index, StandardTableKeyNames.ErrorCode, out var errorCode))
            {
                return errorCode as string;
            }

            return null;
        }


        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DisableRuleCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var vsErrorList = (IVsErrorList)await package.GetServiceAsync(typeof(SVsErrorList));
            var eList = vsErrorList as IErrorList;

            IMenuCommandService commandService = (IMenuCommandService)await package.GetServiceAsync((typeof(IMenuCommandService)));
            Instance = new DisableRuleCommand(package, commandService, eList);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!TryGetErrorCodeSync(out string errorCode))
            {
                errorCode = "{unknown}";
            }

            string message = $"Disabling rule: {errorCode}";
            string title = "Disable SonarLint rule";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

    }
}

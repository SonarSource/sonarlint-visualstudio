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

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.CFamily;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DisableRuleCommand
    {
        // Command set guid and command id. Must match those in DaemonCommands.vsct
        public static readonly Guid CommandSet = new Guid("1F83EA11-3B07-45B3-BF39-307FD4F42194");
        public const int CommandId = 0x0200;

        private readonly OleMenuCommand menuItem;
        private readonly IErrorList errorList;
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package, ILogger logger)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var vsErrorList = (IVsErrorList)await package.GetServiceAsync(typeof(SVsErrorList));
            var eList = vsErrorList as IErrorList;

            var settingsProvider = await package.GetMefServiceAsync<IUserSettingsProvider>();

            IMenuCommandService commandService = (IMenuCommandService)await package.GetServiceAsync((typeof(IMenuCommandService)));
            Instance = new DisableRuleCommand(commandService, eList, settingsProvider, logger);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisableRuleCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="menuCommandService">Command service to add command to, not null.</param>
        internal /* for testing */ DisableRuleCommand(IMenuCommandService menuCommandService, IErrorList errorList,
            IUserSettingsProvider userSettingsProvider, ILogger logger)
        {
            if (menuCommandService == null)
            {
                throw new ArgumentNullException(nameof(menuCommandService));
            }
            this.errorList = errorList ?? throw new ArgumentNullException(nameof(errorList));
            this.userSettingsProvider = userSettingsProvider ?? throw new ArgumentNullException(nameof(userSettingsProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            menuItem = new OleMenuCommand(Execute, null, QueryStatus, menuCommandID);
            menuCommandService.AddCommand(menuItem);
        }

        private void QueryStatus(object sender, EventArgs args)
        {
            try
            {
                bool status = TryGetErrorCodeSync(errorList, out _);
                menuItem.Enabled = status;
                menuItem.Visible = status;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(DaemonStrings.DisableRule_ErrorCheckingCommandStatus, ex.Message);
            }
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
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            string errorCode = null;
            try
            {
                if (TryGetErrorCodeSync(errorList, out errorCode))
                {
                    userSettingsProvider.DisableRule(errorCode);
                    logger.WriteLine(DaemonStrings.DisableRule_DisabledRule, errorCode);
                }

                Debug.Assert(errorCode != null, "Not expecting Execute to be called if the SonarLint error code cannot be determined");
            }
            catch(Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(DaemonStrings.DisableRule_ErrorDisablingRule, errorCode ?? DaemonStrings.DisableRule_UnknownErrorCode, ex.Message);
            }
        }

        /// <summary>
        /// Returns the error code if:
        /// 1) there is only one selected error in the error list and
        /// 2) it is a SonarLint error
        /// </summary>
        internal /* for testing */ static  bool TryGetErrorCodeSync(IErrorList errorList, out string errorCode)
        {
            errorCode = null;
            var selectedItems = errorList?.TableControl?.SelectedEntries;
            if (selectedItems?.Count() == 1)
            {
                var handle = selectedItems.First();
                errorCode = FindErrorCodeForEntry(handle);

                if (!IsDisablingRulesSupported(errorCode))
                {
                    errorCode = null;
                }
            }

            return errorCode != null;
        }

        private static string FindErrorCodeForEntry(ITableEntryHandle handle)
        {
            if (handle.TryGetSnapshot(out var snapshot, out int index) &&
                    snapshot.TryGetValue(index, StandardTableKeyNames.BuildTool, out var buildToolObj) &&
                    buildToolObj is string buildTool &&
                    buildTool.Equals("SonarLint", StringComparison.OrdinalIgnoreCase) &&
                    snapshot.TryGetValue(index, StandardTableKeyNames.ErrorCode, out var errorCode))
            {
                return errorCode as string;
            }

            return null;
        }

        private static readonly string[] supportedLanguages = new[] { CFamily.CFamilyHelper.CPP_LANGUAGE_KEY, CFamily.CFamilyHelper.C_LANGUAGE_KEY };

        private static bool IsDisablingRulesSupported(string errorCode)
        {
            // We don't currently support disabling rules for all languages
            var language = errorCode?.Split(':')?[0];
            return supportedLanguages.Contains(language, CFamilyShared.RuleKeyComparer);
        }
    }
}

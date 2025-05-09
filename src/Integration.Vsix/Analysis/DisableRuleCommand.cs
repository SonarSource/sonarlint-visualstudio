/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
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
        private readonly IGlobalSettingsProvider globalSettingsProvider;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ILogger logger;
        private readonly IErrorListHelper errorListHelper;
        // Strictly speaking we are allowing rules from known repos to be disabled,
        // not "all rules for language X".  However, since we are in control of the
        // rules/repos that are installed in  VSIX, checking the repo key is good
        // enough.

        internal IReadOnlyList<string> SupportedRepos { get; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        [ExcludeFromCodeCoverage]
        public static async Task InitializeAsync(AsyncPackage package, ILogger logger)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var settingsProvider = await package.GetMefServiceAsync<IGlobalSettingsProvider>();
            var tracker = await package.GetMefServiceAsync<IActiveSolutionBoundTracker>();
            var errListHelper = await package.GetMefServiceAsync<IErrorListHelper>();
            var languageProvider = await package.GetMefServiceAsync<ILanguageProvider>();

            IMenuCommandService commandService = (IMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));
            Instance = new DisableRuleCommand(commandService, settingsProvider, tracker, logger, errListHelper, languageProvider);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisableRuleCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="menuCommandService">Command service to add command to, not null.</param>
        internal DisableRuleCommand(
            IMenuCommandService menuCommandService,
            IGlobalSettingsProvider globalSettingsProvider,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ILogger logger,
            IErrorListHelper errorListHelper,
            ILanguageProvider languageProvider)
        {
            if (menuCommandService == null)
            {
                throw new ArgumentNullException(nameof(menuCommandService));
            }
            if (languageProvider == null)
            {
                throw new ArgumentNullException(nameof(languageProvider));
            }

            this.globalSettingsProvider = globalSettingsProvider ?? throw new ArgumentNullException(nameof(globalSettingsProvider));
            this.activeSolutionBoundTracker = activeSolutionBoundTracker ?? throw new ArgumentNullException(nameof(activeSolutionBoundTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.errorListHelper = errorListHelper ?? throw new ArgumentNullException(nameof(errorListHelper));

            SupportedRepos = languageProvider.LanguagesInStandaloneMode.Select(x => x.RepoInfo.Key).ToList();
            var menuCommandID = new CommandID(CommandSet, CommandId);
            menuItem = new OleMenuCommand(Execute, null, QueryStatus, menuCommandID);
            menuCommandService.AddCommand(menuItem);
        }

        private void QueryStatus(object sender, EventArgs args)
        {
            try
            {
                var isVisible = false;
                var isEnabled = false;
                if (errorListHelper.TryGetRuleIdFromSelectedRow(out var ruleId))
                {
                    isVisible = IsSonarRule(ruleId);
                    isEnabled = isVisible && IsDisablingRuleAllowed();
                }

                menuItem.Visible = isVisible;
                menuItem.Enabled = isEnabled;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(AnalysisStrings.DisableRule_ErrorCheckingCommandStatus, ex.Message);
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
            SonarCompositeRuleId ruleId = null;
            try
            {
                if (errorListHelper.TryGetRuleIdFromSelectedRow(out ruleId))
                {
                    globalSettingsProvider.DisableRule(ruleId.ErrorListErrorCode);
                    logger.WriteLine(AnalysisStrings.DisableRule_DisabledRule, ruleId.ErrorListErrorCode);
                }
                Debug.Assert(ruleId != null, "Not expecting Execute to be called if the SonarLint error code cannot be determined");
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(AnalysisStrings.DisableRule_ErrorDisablingRule, ruleId?.ErrorListErrorCode ?? AnalysisStrings.DisableRule_UnknownErrorCode, ex.Message);
            }
        }

        private bool IsSonarRule(SonarCompositeRuleId rule) => SupportedRepos.Contains(rule.RepoKey);

        private bool IsDisablingRuleAllowed()
        {
            // Otherwise, can only disable rules in standalone mode
            return activeSolutionBoundTracker.CurrentConfiguration.Mode == SonarLintMode.Standalone;
        }
    }
}

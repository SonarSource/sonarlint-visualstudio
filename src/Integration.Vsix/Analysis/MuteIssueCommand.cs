/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Core.Transition;
using SonarLint.VisualStudio.Infrastructure.VS;
using MessageBox = SonarLint.VisualStudio.Core.MessageBox;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    [ExcludeFromCodeCoverage]
    internal sealed class MuteIssueCommand
    {
        private readonly IErrorListHelper errorListHelper;
        private readonly IServerIssueFinder serverIssueFinder;
        private readonly IMuteIssuesService muteIssuesService;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IThreadHandling threadHandling;
        private readonly ILogger logger;
        private readonly IMessageBox messageBox;
        private readonly IRoslynIssueLineHashCalculator roslynIssueLineHashCalculator;
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
            Instance = new MuteIssueCommand(commandService,
                await package.GetMefServiceAsync<IErrorListHelper>(),
                await package.GetMefServiceAsync<IRoslynIssueLineHashCalculator>(),
                await package.GetMefServiceAsync<IServerIssueFinder>(),
                await package.GetMefServiceAsync<IMuteIssuesService>(),
                await package.GetMefServiceAsync<IActiveSolutionBoundTracker>(),
                await package.GetMefServiceAsync<IThreadHandling>(),
                new MessageBox(),
                logger);
        }

        internal MuteIssueCommand(IMenuCommandService menuCommandService,
            IErrorListHelper errorListHelper,
            IRoslynIssueLineHashCalculator roslynIssueLineHashCalculator,
            IServerIssueFinder serverIssueFinder,
            IMuteIssuesService muteIssuesService,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IThreadHandling threadHandling,
            IMessageBox messageBox,
            ILogger logger)
        {
            if (menuCommandService == null)
            {
                throw new ArgumentNullException(nameof(menuCommandService));
            }

            this.errorListHelper = errorListHelper ?? throw new ArgumentNullException(nameof(errorListHelper));
            this.serverIssueFinder = serverIssueFinder ?? throw new ArgumentNullException(nameof(serverIssueFinder));
            this.roslynIssueLineHashCalculator = roslynIssueLineHashCalculator ?? throw new ArgumentNullException(nameof(roslynIssueLineHashCalculator));
            this.muteIssuesService = muteIssuesService ?? throw new ArgumentNullException(nameof(muteIssuesService));
            this.activeSolutionBoundTracker = activeSolutionBoundTracker ?? throw new ArgumentNullException(nameof(activeSolutionBoundTracker));
            this.threadHandling = threadHandling ?? throw new ArgumentNullException(nameof(threadHandling));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            menuItem = new OleMenuCommand(Execute, null, QueryStatus, menuCommandID);
            menuCommandService.AddCommand(menuItem);
        }

        private void QueryStatus(object sender, EventArgs e)
        {
            try
            {
                var isActiveSonarRule = errorListHelper.TryGetRuleIdAndSuppressionStateFromSelectedRow(out var ruleId, out var isSuppressed) && IsSupportedSonarRule(ruleId) && !isSuppressed;
                menuItem.Visible = isActiveSonarRule;
                menuItem.Enabled = isActiveSonarRule && activeSolutionBoundTracker.CurrentConfiguration.Mode.IsInAConnectedMode();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(AnalysisStrings.MuteIssue_ErrorCheckingCommandStatus, ex.Message);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            try
            {
                if (!TryGetNonRoslynIssue(out var issue) && !TryGetRoslynIssue(out issue))
                {
                    return;
                }

                threadHandling
                    .RunOnBackgroundThread(() =>
                    {
                        if (issue is IFilterableRoslynIssue roslynIssue)
                        {
                            roslynIssueLineHashCalculator.UpdateRoslynIssueWithLineHash(roslynIssue);
                        }
                        
                        return MuteIssueAsync(issue);
                    })
                    .Forget();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(AnalysisStrings.MuteIssue_ErrorMutingIssue, ex.Message);
            }
        }

        private bool TryGetNonRoslynIssue(out IFilterableIssue issue)
        {
            return errorListHelper.TryGetIssueFromSelectedRow(out issue);
        }

        private bool TryGetRoslynIssue(out IFilterableIssue issue)
        {
            issue = null;
            
            if (errorListHelper.TryGetRoslynIssueFromSelectedRow(out var roslynIssue))
            {
                issue = roslynIssue;
            }

            return issue != null;
        }

        private async Task<bool> MuteIssueAsync(IFilterableIssue issue)
        {
            var serverIssue = await serverIssueFinder.FindServerIssueAsync(issue, CancellationToken.None);
            if (serverIssue == null)
            {
                messageBox.Show(AnalysisStrings.MuteIssue_IssueNotFoundText, AnalysisStrings.MuteIssue_IssueNotFoundCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (serverIssue.IsResolved)
            {
                muteIssuesService.CacheOutOfSyncResolvedIssue(serverIssue);
                messageBox.Show(AnalysisStrings.MuteIssue_IssueAlreadyMutedText, AnalysisStrings.MuteIssue_IssueAlreadyMutedCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            await muteIssuesService.ResolveIssueWithDialogAsync(serverIssue, CancellationToken.None);
            logger.WriteLine(AnalysisStrings.MuteIssue_HaveMuted, serverIssue.IssueKey);

            return true;
        }

        // Strictly speaking we are allowing rules from known repos to be disabled,
        // not "all rules for language X".  However, since we are in control of the
        // rules/repos that are installed in  VSIX, checking the repo key is good
        // enough.
        private static readonly string[] SupportedRepos =
        {
            SonarRuleRepoKeys.C,
            SonarRuleRepoKeys.Cpp,
            SonarRuleRepoKeys.JavaScript,
            SonarRuleRepoKeys.TypeScript,
            SonarRuleRepoKeys.Css,
            SonarRuleRepoKeys.CSharpRules,
            SonarRuleRepoKeys.VBNetRules
        };

        private static bool IsSupportedSonarRule(SonarCompositeRuleId rule) =>
            SupportedRepos.Contains(rule.RepoKey, SonarRuleRepoKeys.RepoKeyComparer);
    }
}

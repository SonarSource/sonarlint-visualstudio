//-----------------------------------------------------------------------
// <copyright file="RuleSetConflictsController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    internal class RuleSetConflictsController : IRuleSetConflictsController
    {
        private const string Indent = "\t";

        private readonly IHost host;

        public RuleSetConflictsController(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.host = host;
            this.FixConflictsCommand = new RelayCommand<IEnumerable<ProjectRuleSetConflict>>(this.OnFixConflicts, this.OnFixConflictsStatus);
        }

        internal /*for testing purposes*/ RelayCommand<IEnumerable<ProjectRuleSetConflict>> FixConflictsCommand { get; }

        #region IRuleSetConflictsController
        public bool CheckForConflicts()
        {
            Debug.Assert(this.host.UIDispatcher.CheckAccess(), "Expected to be called from the UI thread");

            var conflictsManager = this.host.GetService<IConflictsManager>();
            var conflicts = conflictsManager.GetCurrentConflicts();

            if (conflicts.Count > 0)
            {
                this.WriteConflictsSummaryToOutputWindow(conflicts);

                // Let the user know that they have conflicts
                this.host.ActiveSection?.UserNotifications?.ShowNotificationWarning(
                    Strings.RuleSetConflictsDetected, 
                    NotificationIds.RuleSetConflictsId, 
                    this.CreateFixConflictsCommand(conflicts));
            }

            return conflicts.Any();
        }

        public void Clear()
        {
            Debug.Assert(this.host.UIDispatcher.CheckAccess(), "Expected to be called from the UI thread");

            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.RuleSetConflictsId);
        }
        #endregion

        #region Fix conflicts command
        internal /*for testing purposes*/ ICommand CreateFixConflictsCommand(IReadOnlyList<ProjectRuleSetConflict> conflicts)
        {
            Debug.Assert((conflicts?.Count ?? 0)> 0, "Expecting at least one conflict");
            return new ContextualCommandViewModel(conflicts, this.FixConflictsCommand).Command;
        }

        private bool OnFixConflictsStatus(IEnumerable<ProjectRuleSetConflict> conflicts)
        {
            return conflicts != null
                && conflicts.Any()
                && !this.host.VisualStateManager.IsBusy
                && this.host.VisualStateManager.HasBoundProject;
        }

        private void OnFixConflicts(IEnumerable<ProjectRuleSetConflict> conflicts)
        {
            if (this.OnFixConflictsStatus(conflicts))
            {
                IRuleSetInspector inspector = this.host.GetService<IRuleSetInspector>();
                inspector.AssertLocalServiceIsNotNull();

                ISourceControlledFileSystem sccFileSystem = this.host.GetService<ISourceControlledFileSystem>();
                sccFileSystem.AssertLocalServiceIsNotNull();

                IRuleSetSerializer ruleSetSerializer = this.host.GetService<IRuleSetSerializer>();
                ruleSetSerializer.AssertLocalServiceIsNotNull();

                var fixedConflictsMap = new Dictionary<RuleSetInformation, FixedRuleSetInfo>();
                foreach (RuleSetInformation ruleSetInfo in conflicts.Select(c => c.RuleSetInfo))
                {
                    FixedRuleSetInfo fixInfo = inspector.FixConflictingRules(ruleSetInfo.BaselineFilePath, ruleSetInfo.RuleSetFilePath, ruleSetInfo.RuleSetDirectories);
                    Debug.Assert(fixInfo != null);

                    fixedConflictsMap[ruleSetInfo] = fixInfo;

                    sccFileSystem.QueueFileWrite(fixInfo.FixedRuleSet.FilePath, () =>
                    {
                        ruleSetSerializer.WriteRuleSetFile(fixInfo.FixedRuleSet, fixInfo.FixedRuleSet.FilePath);

                        return true;
                    });
                }

                this.WriteFixSummaryToOutputWindow(fixedConflictsMap);

                if (sccFileSystem.WriteQueuedFiles())
                {
                    this.Clear();
                }
                else
                {
                    Debug.Fail("Failed to write one or more of the queued files");
                }
            }
        }
        #endregion

        #region Helpers
        private void WriteConflictsSummaryToOutputWindow(IReadOnlyList<ProjectRuleSetConflict> conflicts)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine();
            foreach (ProjectRuleSetConflict conflictInfo in conflicts)
            {
                WriteSummaryInformation(conflictInfo, builder);
            }

            VsShellUtils.WriteToGeneralOutputPane(this.host, builder.ToString());
        }

        private static void WriteSummaryInformation(ProjectRuleSetConflict conflictInfo, StringBuilder output)
        {
            output.AppendFormat(Strings.ConflictsSummaryHeader,
                            conflictInfo.RuleSetInfo.RuleSetProjectFullName,
                            CreateCommaSeparatedString(conflictInfo.RuleSetInfo.ConfigurationContexts));
            output.AppendLine();

            output.AppendFormat(Strings.ConflictDetailRuleSetInfo, conflictInfo.RuleSetInfo.RuleSetFilePath);
            output.AppendLine();

            if (conflictInfo.Conflict.MissingRules.Any())
            {
                output.AppendLine(Strings.ConflictDetailMissingRules);
                foreach (string ruleId in conflictInfo.Conflict.MissingRules.Select(r => r.FullId))
                {
                    output.Append(Indent);
                    output.AppendLine(ruleId);
                }
            }

            if (conflictInfo.Conflict.WeakerActionRules.Any())
            {
                output.AppendLine(Strings.ConflictDetailWeakenedRulesTitle);
                foreach (var keyValue in conflictInfo.Conflict.WeakerActionRules)
                {
                    output.Append(Indent);
                    output.AppendFormat(Strings.ConflictDetailWeakenedRulesDetail, keyValue.Key.FullId, keyValue.Key.Action, keyValue.Value);
                    output.AppendLine();
                }
            }
        }

        private void WriteFixSummaryToOutputWindow(Dictionary<RuleSetInformation, FixedRuleSetInfo> fixMap)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine();
            foreach (var keyValue in fixMap)
            {
                RuleSetInformation ruleSetInfo = keyValue.Key;
                FixedRuleSetInfo fixInfo = keyValue.Value;

                builder.AppendFormat(Strings.ConflictFixHeader, ruleSetInfo.RuleSetProjectFullName, CreateCommaSeparatedString(ruleSetInfo.ConfigurationContexts));
                builder.AppendLine();
                WriteSummaryInformation(fixInfo, builder);
            }

            VsShellUtils.WriteToGeneralOutputPane(this.host, builder.ToString());
        }

        private static void WriteSummaryInformation(FixedRuleSetInfo fixInfo, StringBuilder output)
        {
            if (fixInfo.IncludesReset.Any())
            {
                output.Append(Indent);
                output.AppendFormat(Strings.ConflictFixResetInclude, CreateSemicolonSeparatedString(fixInfo.IncludesReset));
                output.AppendLine();
            }

            if (fixInfo.RulesDeleted.Any())
            {
                output.Append(Indent);
                output.AppendFormat(Strings.ConflictFixRulesDeleted, fixInfo.FixedRuleSet.FilePath);
                output.AppendLine();
                foreach (string ruleId in fixInfo.RulesDeleted)
                {
                    output.Append(Indent).Append(Indent);
                    output.AppendLine(ruleId);
                }
            }
        }

        private static string CreateCommaSeparatedString<T>(IEnumerable<T> list)
        {
            return string.Join(", ", list);
        }

        private static string CreateSemicolonSeparatedString<T>(IEnumerable<T> list)
        {
            return string.Join(";", list);
        }
        #endregion
    }
}

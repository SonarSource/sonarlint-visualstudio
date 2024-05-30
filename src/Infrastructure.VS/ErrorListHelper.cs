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

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Suppressions;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    [Export(typeof(IErrorListHelper))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ErrorListHelper : IErrorListHelper
    {
        private readonly IVsUIServiceOperation vSServiceOperation;

        [ImportingConstructor]
        public ErrorListHelper(IVsUIServiceOperation vSServiceOperation)
        {
            this.vSServiceOperation = vSServiceOperation;
        }

        public bool TryGetRuleIdFromSelectedRow(out SonarCompositeRuleId ruleId)
        {
            SonarCompositeRuleId ruleIdOut = null;
            var result = vSServiceOperation.Execute<SVsErrorList, IErrorList, bool>(errorList =>
                TryGetSelectedTableEntry(errorList, out var handle) && TryGetRuleId(handle, out ruleIdOut));

            ruleId = ruleIdOut;

            return result;
        }

        public bool TryGetRuleId(ITableEntryHandle handle, out SonarCompositeRuleId ruleId)
        {
            ruleId = null;
            if (!handle.TryGetSnapshot(out var snapshot, out var index))
            {
                return false;
            }

            var errorCode = FindErrorCodeForEntry(snapshot, index);
            return SonarCompositeRuleId.TryParse(errorCode, out ruleId);
        }

        public bool TryGetRuleIdAndSuppressionStateFromSelectedRow(out SonarCompositeRuleId ruleId, out bool isSuppressed)
        {
            SonarCompositeRuleId ruleIdOut = null;
            var isSuppressedOut = false;
            var result = vSServiceOperation.Execute<SVsErrorList, IErrorList, bool>(errorList =>
            {
                if (!TryGetSelectedTableEntry(errorList, out var handle) || !TryGetRuleId(handle, out ruleIdOut))
                {
                    return false;
                }

                isSuppressedOut = IsSuppressed(handle);
                return true;
            });

            ruleId = ruleIdOut;
            isSuppressed = isSuppressedOut;

            return result;
        }

        public bool TryGetIssueFromSelectedRow(out IFilterableIssue issue)
        {
            IFilterableIssue issueOut = null;
            var result = vSServiceOperation.Execute<SVsErrorList, IErrorList, bool>(
                errorList => TryGetSelectedSnapshotAndIndex(errorList, out var snapshot, out var index)
                             && TryGetValue(snapshot, index, SonarLintTableControlConstants.IssueVizColumnName, out issueOut));

            issue = issueOut;

            return result;
        }

        public bool TryGetRoslynIssueFromSelectedRow(out IFilterableRoslynIssue filterableRoslynIssue)
        {
            IFilterableRoslynIssue outIssue = null;

            var result = vSServiceOperation.Execute<SVsErrorList, IErrorList, bool>(errorList =>
            {
                string errorCode;
                if (TryGetSelectedSnapshotAndIndex(errorList, out var snapshot, out var index)
                    && (errorCode = FindErrorCodeForEntry(snapshot, index)) != null
                    && TryGetValue(snapshot, index, StandardTableKeyNames.DocumentName, out string filePath)
                    && TryGetValue(snapshot, index, StandardTableKeyNames.Line, out int line)
                    && TryGetValue(snapshot, index, StandardTableKeyNames.Column, out int column))
                {
                    outIssue = new FilterableRoslynIssue(errorCode, filePath, line + 1, column + 1 /* error list issues are 0-based and we use 1-based line & column numbers */);
                }

                return outIssue != null;
            });

            filterableRoslynIssue = outIssue;

            return result;
        }

        private static bool IsSuppressed(ITableEntryHandle handle)
        {
            return handle.TryGetSnapshot(out var snapshot, out var index)
                   && TryGetValue(snapshot, index, StandardTableKeyNames.SuppressionState, out SuppressionState suppressionState)
                   && suppressionState == SuppressionState.Suppressed;
        }

        private static string FindErrorCodeForEntry(ITableEntriesSnapshot snapshot, int index)
        {
            if (!TryGetValue(snapshot, index, StandardTableKeyNames.ErrorCode, out string errorCode))
            {
                return null;
            }

            if (TryGetValue(snapshot, index, StandardTableKeyNames.BuildTool, out string buildTool))
            {
                // For CSharp and VisualBasic the buildTool returns the name of the analyzer package.
                // The prefix is required for roslyn languages as the error code is in style "S111" meaning
                // unlike other languages it has no repository prefix.
                return buildTool switch
                {
                    "SonarAnalyzer.CSharp" => $"{SonarRuleRepoKeys.CSharpRules}:{errorCode}",
                    "SonarAnalyzer.VisualBasic" => $"{SonarRuleRepoKeys.VBNetRules}:{errorCode}",
                    "SonarLint" => errorCode,
                    _ => null
                };
            }

            if (TryGetValue(snapshot, index, StandardTableKeyNames.HelpLink, out string helpLink))
            {
                if (helpLink.Contains("rules.sonarsource.com/csharp/"))
                {
                    return $"{SonarRuleRepoKeys.CSharpRules}:{errorCode}";
                }
                
                if (helpLink.Contains("rules.sonarsource.com/vbnet/"))
                {
                    return $"{SonarRuleRepoKeys.VBNetRules}:{errorCode}";
                }
            }

            return null;
        }

        private static bool TryGetSelectedSnapshotAndIndex(IErrorList errorList, out ITableEntriesSnapshot snapshot, out int index)
        {
            snapshot = default;
            index = default;

            return TryGetSelectedTableEntry(errorList, out var handle) && handle.TryGetSnapshot(out snapshot, out index);
        }

        private static bool TryGetSelectedTableEntry(IErrorList errorList, out ITableEntryHandle handle)
        {
            handle = null;

            var selectedItems = errorList?.TableControl?.SelectedEntries;

            if (selectedItems == null)
            {
                return false;
            }

            foreach (var tableEntryHandle in selectedItems)
            {
                if (handle != null)
                {
                    return false; // more than one selected is not supported
                }

                handle = tableEntryHandle;
            }

            return true;
        }

        private static bool TryGetValue<T>(ITableEntriesSnapshot snapshot, int index, string columnName, out T value)
        {
            value = default;

            try
            {
                if (!snapshot.TryGetValue(index, columnName, out var objValue) || objValue == null)
                {
                    return false;
                }

                value = (T)objValue;
                return true;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }
    }
}

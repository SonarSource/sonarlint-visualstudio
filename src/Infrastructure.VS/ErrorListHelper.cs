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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Suppressions;

namespace SonarLint.VisualStudio.Infrastructure.VS;

[Export(typeof(IErrorListHelper))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class ErrorListHelper(IVsUIServiceOperation vSServiceOperation) : IErrorListHelper
{
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

        ruleId = FindErrorCodeForEntry(snapshot, index);
        return ruleId != null;
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
            errorList => TryGetSelectedTableEntry(errorList, out var handle) && TryGetFilterableIssue(handle, out issueOut));

        issue = issueOut;

        return result;
    }

    public bool TryGetFilterableIssue(ITableEntryHandle handle, out IFilterableIssue issue)
    {
        IFilterableIssue issueOut = null;
        var result = vSServiceOperation.Execute<SVsErrorList, IErrorList, bool>(
            _ => handle.TryGetSnapshot(out var snapshot, out var index)
                 && TryGetValue(snapshot, index, SonarLintTableControlConstants.IssueVizColumnName, out issueOut));

        issue = issueOut;

        return result;
    }

    private static bool IsSuppressed(ITableEntryHandle handle) =>
        handle.TryGetSnapshot(out var snapshot, out var index)
        && TryGetValue(snapshot, index, StandardTableKeyNames.SuppressionState, out SuppressionState suppressionState)
        && suppressionState == SuppressionState.Suppressed;

    private static SonarCompositeRuleId FindErrorCodeForEntry(ITableEntriesSnapshot snapshot, int index) =>
        TryGetValue(snapshot, index, SonarLintTableControlConstants.IssueVizColumnName, out IFilterableIssue issue)
            ? issue?.SonarRuleId
            : null;

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

    private static bool TryGetValue<T>(
        ITableEntriesSnapshot snapshot,
        int index,
        string columnName,
        out T value)
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

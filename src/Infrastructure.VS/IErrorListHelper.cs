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

using Microsoft.VisualStudio.Shell.TableControl;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Suppressions;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    public interface IErrorListHelper
    {
        /// <summary>
        /// Attempts to retrieve the Sonar rule key from the selected item in the Error List.
        /// </summary>
        /// <remarks>
        /// The method will only return a rule key if:
        /// * there is a single row selected in the Error List
        /// * the row represents a Sonar analysis issue for any supported language
        ///   (including Roslyn languages i.e. C# and VB.NET)
        /// </remarks>
        bool TryGetRuleIdFromSelectedRow(out SonarCompositeRuleId ruleId);

        /// <summary>
        /// Attempts to retrieve the Sonar rule key from the specific table entry in the Error List.
        /// </summary>
        /// The method will only return a rule key if the row represents a Sonar analysis issue for
        /// any supported language (including Roslyn languages i.e. C# and VB.NET)

        bool TryGetRuleId(ITableEntryHandle handle, out SonarCompositeRuleId ruleId);

        /// <summary>
        /// Extracts, if present, <see cref="IFilterableIssue"/> from the hidden column <see cref="SonarLintTableControlConstants.IssueVizColumnName"/>
        /// </summary>
        /// <returns>True if issue is present in the selected row, False if not present or multiple rows selected</returns>
        bool TryGetIssueFromSelectedRow(out IFilterableIssue issue);

        /// <summary>
        /// Extracts <see cref="IFilterableRoslynIssue"/> from error code, line number and file path. Does not calculate line hash.
        /// </summary>
        /// <returns>True if error code is of sonar issue, line number and file path are available. False otherwise or if multiple rows selected</returns>
        bool TryGetRoslynIssueFromSelectedRow(out IFilterableRoslynIssue filterableRoslynIssue);

    }
}

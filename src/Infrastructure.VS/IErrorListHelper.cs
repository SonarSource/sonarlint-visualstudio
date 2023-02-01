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

using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    public interface IErrorListHelper
    {
        bool TryGetRuleIdFromSelectedRow(IErrorList errorList, out SonarCompositeRuleId ruleId);
    }

    public class ErrorListHelper : IErrorListHelper
    {
        public bool TryGetRuleIdFromSelectedRow(IErrorList errorList, out SonarCompositeRuleId ruleId)
        {
            ruleId = null;
            var selectedItems = errorList?.TableControl?.SelectedEntries;

            if (selectedItems?.Count() == 1)
            {
                var handle = selectedItems.First();
                var errorCode = FindErrorCodeForEntry(handle);
                SonarCompositeRuleId.TryParse(errorCode, out ruleId);
            }

            return ruleId != null;
        }

        private static string FindErrorCodeForEntry(ITableEntryHandle handle)
        {
            if (handle.TryGetSnapshot(out var snapshot, out int index) &&
                snapshot.TryGetValue(index, StandardTableKeyNames.BuildTool, out var buildToolObj) &&
                buildToolObj is string buildTool)
            {
                var prefixErrorCode = "";

                switch (buildTool)
                {
                    case "SonarAnalyzer.CSharp":
                        {
                            prefixErrorCode = "cs:";
                            break;
                        }
                    case "SonarAnalyzer.VisualBasic":
                        {
                            prefixErrorCode = "vb:";
                            break;
                        }
                    case "SonarLint":
                        break;

                    default:
                        return null;
                }

                if (snapshot.TryGetValue(index, StandardTableKeyNames.ErrorCode, out var errorCode))
                {
                    return prefixErrorCode + errorCode as string;
                }
            }

            return null;
        }
    }
}

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
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core;

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
            var result = vSServiceOperation.Execute<SVsErrorList, IErrorList, bool>(
                errorList =>
                {
                    return DoTryGetRuleIdFromSelectedRow(errorList, out ruleIdOut);
                });

            ruleId = ruleIdOut;

            return result;
        }

        private bool DoTryGetRuleIdFromSelectedRow(IErrorList errorList, out SonarCompositeRuleId ruleId)
        {
            ruleId = null;
            var selectedItems = errorList?.TableControl?.SelectedEntries;

            if (selectedItems?.Count() == 1)
            {
                var handle = selectedItems.First();
                TryGetRuleId(handle, out ruleId);
            }

            return ruleId != null;
        }

        public bool TryGetRuleId(ITableEntryHandle tableEntryHandle, out SonarCompositeRuleId ruleId)
        {
            var errorCode = FindErrorCodeForEntry(tableEntryHandle);
            return SonarCompositeRuleId.TryParse(errorCode, out ruleId);
        }

        private static string FindErrorCodeForEntry(ITableEntryHandle handle)
        {
            if (handle.TryGetSnapshot(out var snapshot, out int index) &&
                snapshot.TryGetValue(index, StandardTableKeyNames.BuildTool, out var buildToolObj) &&
                buildToolObj is string buildTool)
            {
                var prefixErrorCode = "";
                
                // For CSharp and VisualBasic the buildTool returns the name of the analyzer package. 
                // The prefix is required for roslyn languages as the error code is in style "S111" meaning
                // unlike other languages it has no repository prefix.
                switch (buildTool)
                {
                    case "SonarAnalyzer.CSharp":
                        {
                            prefixErrorCode = "csharpsquid:";
                            break;
                        }
                    case "SonarAnalyzer.VisualBasic":
                        {
                            prefixErrorCode = "vbnet:";
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

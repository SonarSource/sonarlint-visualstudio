/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Windows;
using Microsoft.VisualStudio.Shell.TableControl;
using SonarLint.VisualStudio.IssueVisualization.Models;
using ImageMoniker = Microsoft.VisualStudio.Imaging.Interop.ImageMoniker;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource
{
    internal class HotspotTableEntry : IWpfTableEntry
    {
        private readonly IAnalysisIssueVisualization hotspot;

        public HotspotTableEntry(IAnalysisIssueVisualization hotspot)
        {
            this.hotspot = hotspot;
        }

        public object Identity => hotspot;

        public bool TryGetValue(string keyName, out object content)
        {
            var originalIssue = hotspot.Issue;

            switch (keyName)
            {
                case StandardTableColumnDefinitions.ErrorCode:
                    content = originalIssue.RuleKey;
                    break;

                case StandardTableColumnDefinitions.DocumentName:
                    content = originalIssue.FilePath;
                    break;

                case StandardTableColumnDefinitions.Text:
                    content = originalIssue.Message;
                    break;

                case StandardTableColumnDefinitions.Line:
                    content = originalIssue.StartLine;
                    break;

                case StandardTableColumnDefinitions.Column:
                    content = originalIssue.StartLineOffset;
                    break;

                default:
                    content = null;
                    return false;
            }

            return true;
        }

        #region ITableEntry unimplemented methods

        public virtual bool TryCreateToolTip(string columnName, out object toolTip)
        {
            toolTip = null;
            return false;
        }

        public virtual bool TrySetValue(string keyName, object content)
        {
            return false;
        }

        public virtual bool CanSetValue(string keyName)
        {
            return false;
        }

        public virtual bool TryCreateStringContent(string columnName, bool truncatedText, bool singleColumnView, out string content)
        {
            content = null;
            return false;
        }

        public virtual bool TryCreateImageContent(string columnName, bool singleColumnView, out ImageMoniker content)
        {
            content = default;
            return false;
        }

        public virtual bool TryCreateColumnContent(string columnName, bool singleColumnView, out FrameworkElement content)
        {
            content = null;
            return false;
        }

        public virtual bool CanCreateDetailsContent()
        {
            return false;
        }

        public virtual bool TryCreateDetailsContent(out FrameworkElement expandedContent)
        {
            expandedContent = null;
            return false;
        }

        public virtual bool TryCreateDetailsStringContent(out string content)
        {
            content = null;
            return false;
        }

        #endregion
    }
}

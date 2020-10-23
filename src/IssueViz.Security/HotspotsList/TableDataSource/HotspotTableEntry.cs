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
        private readonly IAnalysisSeverityToPriorityConverter severityToPriorityConverter;

        public HotspotTableEntry(IAnalysisIssueVisualization hotspot)
            : this(hotspot, new AnalysisSeverityToPriorityConverter())
        {
        }

        internal HotspotTableEntry(IAnalysisIssueVisualization hotspot, IAnalysisSeverityToPriorityConverter severityToPriorityConverter)
        {
            this.hotspot = hotspot;
            this.severityToPriorityConverter = severityToPriorityConverter;
        }

        public object Identity => hotspot;

        public bool TryGetValue(string keyName, out object content)
        {
            var originalIssue = hotspot.Issue;
            var span = hotspot.Span;

            switch (keyName)
            {
                case StandardTableColumnDefinitions.ErrorCode:
                    content = originalIssue.RuleKey;
                    break;

                case StandardTableColumnDefinitions.Priority:
                    content = severityToPriorityConverter.Convert(originalIssue.Severity);
                    break;

                case StandardTableColumnDefinitions.Text:
                    content = originalIssue.Message;
                    break;

                case StandardTableColumnDefinitions.DocumentName:
                    content = originalIssue.FilePath;
                    break;

                case StandardTableColumnDefinitions.Line:
                    content = span?.Start.GetContainingLine().LineNumber ?? originalIssue.StartLine;
                    break;

                case StandardTableColumnDefinitions.Column:
                    if (!span.HasValue)
                    {
                        content = originalIssue.StartLineOffset;
                        break;
                    }
                    var position = span.Value.Start;
                    var line = position.GetContainingLine();
                    content = position.Position - line.Start.Position;
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

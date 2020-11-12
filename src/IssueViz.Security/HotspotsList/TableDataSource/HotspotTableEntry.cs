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
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource.CustomColumns;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource
{
    internal class HotspotTableEntry : WpfTableEntryBase
    {
        private readonly IAnalysisIssueVisualization hotspotViz;
        private readonly IHotspotTableEntryWpfElementFactory hotspotTableEntryWpfElementFactory;

        public HotspotTableEntry(IAnalysisIssueVisualization hotspotViz, IHotspotTableEntryWpfElementFactory hotspotTableEntryWpfElementFactory)
        {
            this.hotspotViz = hotspotViz;
            this.hotspotTableEntryWpfElementFactory = hotspotTableEntryWpfElementFactory;
        }

        public override object Identity => hotspotViz;

        public override bool TryGetValue(string keyName, out object content)
        {
            var hotspot = hotspotViz.Issue as IHotspot;

            switch (keyName)
            {
                case StandardTableColumnDefinitions.ErrorCode:
                    content = hotspot.RuleKey;
                    break;

                case PriorityTableColumnDefinition.ColumnName:
                    content = hotspot.Priority.ToString();
                    break;

                case StandardTableColumnDefinitions.Text:
                    content = hotspot.Message;
                    break;

                case StandardTableColumnDefinitions.DocumentName:
                    content = hotspot.FilePath;
                    break;

                case StandardTableColumnDefinitions.Line:
                    if (!hotspotViz.Span.HasValue || hotspotViz.Span.Value.IsEmpty)
                    {
                        content = hotspot.StartLine;
                        break;
                    }

                    content = hotspotViz.Span.Value.Start.GetContainingLine().LineNumber;
                    break;

                case StandardTableColumnDefinitions.Column:
                    if (!hotspotViz.Span.HasValue || hotspotViz.Span.Value.IsEmpty)
                    {
                        content = hotspot.StartLineOffset;
                        break;
                    }

                    var position = hotspotViz.Span.Value.Start;
                    var line = position.GetContainingLine();
                    content = position.Position - line.Start.Position;
                    break;

                default:
                    content = null;
                    return false;
            }

            return true;
        }

        public override bool TryCreateColumnContent(string columnName, bool singleColumnView, out FrameworkElement content)
        {
            if (!TryGetValue(columnName, out var columnContent))
            {
                return base.TryCreateColumnContent(columnName, singleColumnView, out content);
            }

            content = hotspotTableEntryWpfElementFactory.Create(columnContent.ToString());
            return true;
        }
    }
}

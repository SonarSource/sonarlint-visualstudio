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

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource.CustomColumns;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;
using Color = System.Windows.Media.Color;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource
{
    internal class HotspotTableEntry : WpfTableEntryBase
    {
        private readonly IAnalysisIssueVisualization hotspotViz;
        private readonly IVsUIShell2 vsUiShell;

        public HotspotTableEntry(IAnalysisIssueVisualization hotspotViz, IVsUIShell2 vsUiShell)
        {
            this.hotspotViz = hotspotViz;
            this.vsUiShell = vsUiShell;

            if (!(hotspotViz.Issue is IHotspot))
            {
                throw new InvalidCastException($"{nameof(hotspotViz.Issue)} is not {nameof(IHotspot)}");
            }
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
            if (hotspotViz.IsNavigable() || !TryGetValue(columnName, out var columnContent))
            {
                return base.TryCreateColumnContent(columnName, singleColumnView, out content);
            }

            var textColor = GetInactiveTextColor();
            var control = new TextBlock
            {
                Text = columnContent.ToString(),
                Foreground = new SolidColorBrush(textColor),
                FontStyle = FontStyles.Italic
            };

            content = control;
            return true;
        }

        private Color GetInactiveTextColor()
        {
            vsUiShell.GetVSSysColorEx((int) __VSSYSCOLOREX.VSCOLOR_COMMANDBAR_TEXT_INACTIVE, out var rgbValue);

            var color = ColorTranslator.FromWin32((int) rgbValue);
            var wpfColor = Color.FromRgb(color.R, color.G, color.B);

            return wpfColor;
        }
    }
}

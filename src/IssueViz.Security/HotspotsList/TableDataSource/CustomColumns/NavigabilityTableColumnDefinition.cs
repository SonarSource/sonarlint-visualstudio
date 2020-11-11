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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource.CustomColumns
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    internal class NavigabilityTableColumnDefinition : TableColumnDefinitionBase
    {
        public const string ColumnName = "NavigabilityStatus";
        public const int Width = 20;

        public override string Name { get; } = ColumnName;

        public override string DisplayName { get; } = "";

        public override double DefaultWidth { get; } = Width;

        public override bool IsFilterable { get; } = false;

        public override bool IsSortable { get; } = false;

        public override bool TryCreateToolTip(ITableEntryHandle entry, out object toolTip)
        {
            if (!(entry.Identity is IAnalysisIssueVisualization issueViz))
            {
                return base.TryCreateToolTip(entry, out toolTip);
            }

            if (!issueViz.IsNavigable())
            {
                toolTip = HotspotsListResources.ERR_CannotNavigateTooltip;
                return true;
            }

            toolTip = null;
            return false;
        }

        public override bool TryCreateImageContent(ITableEntryHandle entry, bool singleColumnView, out ImageMoniker content)
        {
            if (!(entry.Identity is IAnalysisIssueVisualization issueViz))
            {
                return base.TryCreateImageContent(entry, singleColumnView, out content);
            }

            if (!issueViz.IsNavigable())
            {
                content = KnownMonikers.DocumentWarning;
                return true;
            }

            content = default;
            return false;
        }
    }
}

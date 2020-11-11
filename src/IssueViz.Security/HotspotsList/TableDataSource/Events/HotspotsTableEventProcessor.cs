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

using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource.Events
{
    internal class HotspotsTableEventProcessor : TableControlEventProcessorBase
    {
        private readonly ILocationNavigator locationNavigator;

        public HotspotsTableEventProcessor(ILocationNavigator locationNavigator)
        {
            this.locationNavigator = locationNavigator;
        }

        public override void PreprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
        {
            base.PreprocessNavigate(entry, e);

            TryNavigateToHotspot(entry);
        }

        private void TryNavigateToHotspot(ITableEntry entry)
        {
            if (!(entry?.Identity is IAnalysisIssueVisualization issueVisualization))
            {
                return;
            }

            locationNavigator.TryNavigate(issueVisualization);
        }
    }
}

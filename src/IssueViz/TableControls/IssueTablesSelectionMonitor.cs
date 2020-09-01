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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.TableControls
{
    /// <summary>
    /// Handles issue selection changes from table controls
    /// </summary>
    internal interface IIssueTablesSelectionMonitor
    {
        void SelectionChanged(IAnalysisIssueVisualization selectedIssue);
    }

    /// <summary>
    /// Singleton that processes "selected issue changed" notifications from multiple different
    /// table sources
    /// </summary>
    [Export(typeof(IIssueTablesSelectionMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class IssueTablesSelectionMonitor : IIssueTablesSelectionMonitor
    {
        private readonly IAnalysisIssueSelectionService selectionService;

        [ImportingConstructor]
        public IssueTablesSelectionMonitor(IAnalysisIssueSelectionService selectionService)
        {
            // Created by VS MEF -> arguments will never be null
            this.selectionService = selectionService;
        }

        void IIssueTablesSelectionMonitor.SelectionChanged(IAnalysisIssueVisualization selected)
        {
            // Exception handling: assuming the caller is handling exceptions
            selectionService.SelectedIssue = selected;
        }
    }
}

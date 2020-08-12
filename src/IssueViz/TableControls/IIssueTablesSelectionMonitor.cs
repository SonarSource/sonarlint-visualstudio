/*
 * SonarQube Client
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
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.TableControls
{
    /// <summary>
    /// Aggregates and handle issue changed notifications from multiple VS WPF table controls
    /// </summary>
    public interface IIssueTablesSelectionMonitor
    {
        void AddEventSource(IIssueTableEventSource source);
    }

    /// <summary>
    /// Raises an event when the selected issue in an table of issues changes
    /// </summary>
    /// <remarks>The selected issue will be null if more than one item is selected in the list
    /// or if the selected item is not a SonarLint issue</remarks>
    public interface IIssueTableEventSource
    {
        event EventHandler<IssueTableSelectionChangedEventArgs> SelectedIssueChanged;
    }

    public class IssueTableSelectionChangedEventArgs : EventArgs
    {
        public IssueTableSelectionChangedEventArgs(IAnalysisIssue selectedIssue)
        {
            SelectedIssue = selectedIssue;
        }

        /// <summary>
        /// The selected issue. Can be null.
        /// </summary>
        public IAnalysisIssue SelectedIssue { get; }
    }
}

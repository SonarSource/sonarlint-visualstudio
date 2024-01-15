/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Selection
{
    /// <summary>
    /// Represents the currently selected <see cref="IAnalysisIssueVisualization"/>.
    /// Raises an event <see cref="SelectedIssueChanged"/> when selection changes.
    /// </summary>
    public interface IIssueSelectionService
    {
        event EventHandler SelectedIssueChanged;

        IAnalysisIssueVisualization SelectedIssue { get; set; }
    }

    [Export(typeof(IIssueSelectionService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class IssueSelectionService : IIssueSelectionService
    {
        private IAnalysisIssueVisualization selectedIssue;

        public event EventHandler SelectedIssueChanged;

        public IAnalysisIssueVisualization SelectedIssue
        {
            get => selectedIssue;
            set
            {
                var selectionChanged = selectedIssue != value;

                if (selectionChanged)
                {
                    selectedIssue = value;
                    SelectedIssueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}

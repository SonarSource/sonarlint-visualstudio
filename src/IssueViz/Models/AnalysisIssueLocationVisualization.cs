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

using Microsoft.VisualStudio.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Models
{
    public interface IAnalysisIssueLocationVisualization
    {
        int StepNumber { get; }

        bool IsNavigable { get; set; }

        IAnalysisIssueLocation Location { get; }

        /// <summary>
        /// Editor span associated with the issue location
        /// </summary>
        /// <remarks>
        /// The span gives the current region in an open editor document for the issue. This can be different
        /// from the Location of the issue, which is fixed at time of analysis.
        /// It will be null if the document has not been opened in the editor.
        /// It will be an empty span if the issue location does not exist in the document due to editing.
        /// </remarks>
        SnapshotSpan? Span { get; set; }
    }

    public class AnalysisIssueLocationVisualization : IAnalysisIssueLocationVisualization, INotifyPropertyChanged
    {
        private bool isNavigable;

        public AnalysisIssueLocationVisualization(int stepNumber, IAnalysisIssueLocation location)
        {
            StepNumber = stepNumber;
            Location = location;
            IsNavigable = true;
        }

        public int StepNumber { get; }

        public IAnalysisIssueLocation Location { get; }

        public bool IsNavigable
        {
            get => isNavigable;
            set
            {
                isNavigable = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public SnapshotSpan? Span { get; set; }
    }
}

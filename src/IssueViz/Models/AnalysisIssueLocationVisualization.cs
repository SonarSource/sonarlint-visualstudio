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
    public interface IAnalysisIssueLocationVisualization : INotifyPropertyChanged
    {
        int StepNumber { get; }

        IAnalysisIssueLocation Location { get; }

        /// <summary>
        /// Up-to-date file path associated with the issue location
        /// </summary>
        /// <remarks>
        /// If the file was renamed after the analysis, this property will contain the new value and will be different from the underlying <see cref="IAnalysisIssueLocation.FilePath"/>.
        /// </remarks>
        string CurrentFilePath { get; set; }

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

    public class AnalysisIssueLocationVisualization : IAnalysisIssueLocationVisualization
    {
        private string filePath;
        private SnapshotSpan? span;

        public AnalysisIssueLocationVisualization(int stepNumber, IAnalysisIssueLocation location)
        {
            StepNumber = stepNumber;
            Location = location;
            CurrentFilePath = location.FilePath;
        }

        public int StepNumber { get; }

        public IAnalysisIssueLocation Location { get; }

        public SnapshotSpan? Span
        {
            get => span;
            set 
            {
                span = value;
                NotifyPropertyChanged();
            }
        }

        public string CurrentFilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class AnalysisIssueLocationVisualizationExtensions
    {
        /// <summary>
        /// Returns true/false if we can navigate to <see cref="Span"/>.
        /// Returns true if <see cref="Span"/> is null.
        /// </summary>
        public static bool IsNavigable(this IAnalysisIssueLocationVisualization locationVisualization)
        {
            return locationVisualization.Span.IsNavigable();
        }

        public static bool IsNavigable(this SnapshotSpan? span)
        {
            return !span.HasValue || !span.Value.IsEmpty;
        }
    }
}

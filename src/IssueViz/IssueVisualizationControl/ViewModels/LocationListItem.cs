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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels
{
    internal sealed class LocationListItem : ILocationListItem
    {
        public IAnalysisIssueLocationVisualization Location { get; }
        public string DisplayMessage { get; }
        public int LineNumber { get; private set; }

        public LocationListItem(IAnalysisIssueLocationVisualization location)
        {
            DisplayMessage = location is IAnalysisIssueVisualization
                ? "root"
                : location.Location.Message;

            Location = location;
            location.PropertyChanged += Location_PropertyChanged;

            UpdateState();
        }

        private void Location_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IAnalysisIssueLocationVisualization.Span))
            {
                UpdateState();
            }
        }

        private void UpdateState()
        {
            if (!Location.Span.HasValue)
            {
                LineNumber = Location.Location.StartLine;
            } 
            else if (!Location.Span.IsNavigable())
            {
                LineNumber = 0;
            }
            else
            {
                var position = Location.Span.Value.Start;
                var line = position.GetContainingLine();
                LineNumber = line.LineNumber + 1;
            }

            NotifyPropertyChanged(nameof(LineNumber));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Location.PropertyChanged -= Location_PropertyChanged;
        }
    }
}

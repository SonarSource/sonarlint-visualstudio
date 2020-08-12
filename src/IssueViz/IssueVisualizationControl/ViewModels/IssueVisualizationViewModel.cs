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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.SelectionEvents;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels
{
    internal sealed class IssueVisualizationViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IAnalysisIssueSelectionEvents selectionEvents;

        private IAnalysisIssueVisualization currentIssue;
        private IAnalysisIssueFlowVisualization currentFlow;
        private LocationListItem currentLocation;

        private bool isBindingUpdatedInControl;

        public IssueVisualizationViewModel(IAnalysisIssueSelectionEvents selectionEvents)
        {
            this.selectionEvents = selectionEvents;

            selectionEvents.SelectedIssueChanged += SelectionEvents_SelectedIssueChanged;
            selectionEvents.SelectedFlowChanged += SelectionEventsOnSelectedFlowChanged;
            selectionEvents.SelectedLocationChanged += SelectionEvents_SelectedLocationChanged;
        }

        public string Description => currentIssue?.Issue?.Message;

        public IAnalysisIssueVisualization CurrentIssue
        {
            get => currentIssue;
            set
            {
                currentIssue = value;
                OnPropertyChanged(null);
            }
        }

        public IAnalysisIssueFlowVisualization CurrentFlow
        {
            get => currentFlow;
            set
            {
                currentFlow = value;

                if (isBindingUpdatedInControl)
                {
                    selectionEvents.SelectedFlow = currentFlow;
                }
                else
                {
                    OnPropertyChanged(nameof(CurrentFlow));

                    LocationListItems = BuildLocationListItems(currentFlow);
                    OnPropertyChanged(nameof(LocationListItems));
                }
            }
        }

        public IReadOnlyList<ILocationListItem> LocationListItems { get; private set; }

        public LocationListItem CurrentLocationListItem
        {
            get => currentLocation;
            set
            {
                currentLocation = value;

                if (isBindingUpdatedInControl)
                {
                    selectionEvents.SelectedLocation = currentLocation?.Location;
                }
                else
                {
                    OnPropertyChanged(nameof(CurrentLocationListItem));
                }
            }
        }

        private void SelectionEvents_SelectedIssueChanged(object sender, IssueChangedEventArgs e)
        {
            CurrentIssue = e.Issue;
        }

        private void SelectionEventsOnSelectedFlowChanged(object sender, FlowChangedEventArgs e)
        {
            isBindingUpdatedInControl = false;

            CurrentFlow = e.Flow;
        }

        private void SelectionEvents_SelectedLocationChanged(object sender, LocationChangedEventArgs e)
        {
            isBindingUpdatedInControl = false;

            if (e.Location == null)
            {
                CurrentLocationListItem = null;
            }
            else
            {
                var selectedLocationListItem = LocationListItems
                    .OfType<LocationListItem>()
                    .FirstOrDefault(x => x.Location == e.Location);

                CurrentLocationListItem = selectedLocationListItem;
            }
        }

        private static IReadOnlyList<ILocationListItem> BuildLocationListItems(IAnalysisIssueFlowVisualization flow)
        {
            var listItems = new List<ILocationListItem>();
            var flowLocations = flow?.Locations.ToList();

            if (flowLocations != null && flowLocations.Any())
            {
                for (var i = 0; i < flowLocations.Count; i++)
                {
                    var filePath = flowLocations[i].Location.FilePath;
                    
                    listItems.Add(new FileNameLocationListItem(Path.GetFileName(filePath)));

                    var sequentialLocations = flowLocations.Skip(i).TakeWhile(x => x.Location.FilePath == filePath).ToList();
                    listItems.AddRange(sequentialLocations.Select(x => (ILocationListItem)new LocationListItem(x)));

                    i += sequentialLocations.Count - 1;
                }
            }

            return listItems;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            selectionEvents.SelectedIssueChanged -= SelectionEvents_SelectedIssueChanged;
            selectionEvents.SelectedFlowChanged -= SelectionEventsOnSelectedFlowChanged;
            selectionEvents.SelectedLocationChanged -= SelectionEvents_SelectedLocationChanged;
            selectionEvents?.Dispose();
        }
    }
}

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
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels
{
    internal sealed class IssueVisualizationViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IAnalysisIssueSelectionService selectionEvents;
        private readonly IVsImageService2 vsImageService;
        private readonly IRuleHelpLinkProvider ruleHelpLinkProvider;

        private IAnalysisIssueVisualization currentIssue;
        private IAnalysisIssueFlowVisualization currentFlow;
        private LocationListItem currentLocation;

        private bool isBindingUpdatedOutsideOfControl;

        public IssueVisualizationViewModel(IAnalysisIssueSelectionService selectionEvents, IVsImageService2 vsImageService, IRuleHelpLinkProvider ruleHelpLinkProvider)
        {
            this.selectionEvents = selectionEvents;
            this.vsImageService = vsImageService;
            this.ruleHelpLinkProvider = ruleHelpLinkProvider;

            selectionEvents.SelectedIssueChanged += SelectionEvents_SelectedIssueChanged;
            selectionEvents.SelectedFlowChanged += SelectionEventsOnSelectedFlowChanged;
            selectionEvents.SelectedLocationChanged += SelectionEvents_SelectedLocationChanged;
        }

        public string Description => currentIssue?.Issue?.Message;

        public string RuleKey => currentIssue?.Issue?.RuleKey;

        public string RuleHelpLink => string.IsNullOrEmpty(RuleKey) ? string.Empty : ruleHelpLinkProvider.GetHelpLink(RuleKey);

        public AnalysisIssueSeverity Severity => currentIssue?.Issue?.Severity ?? AnalysisIssueSeverity.Info;

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

                if (isBindingUpdatedOutsideOfControl)
                {
                    OnPropertyChanged(nameof(CurrentFlow));

                    LocationListItems = BuildLocationListItems(currentFlow);
                    OnPropertyChanged(nameof(LocationListItems));
                }
                else
                {
                    selectionEvents.SelectedFlow = currentFlow;
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

                if (isBindingUpdatedOutsideOfControl)
                {
                    OnPropertyChanged(nameof(CurrentLocationListItem));
                }
                else
                {
                    selectionEvents.SelectedLocation = currentLocation?.Location;
                }
            }
        }

        private void SelectionEvents_SelectedIssueChanged(object sender, IssueChangedEventArgs e)
        {
            CurrentIssue = e.Issue;
        }

        private void SelectionEventsOnSelectedFlowChanged(object sender, FlowChangedEventArgs e)
        {
            isBindingUpdatedOutsideOfControl = true;

            CurrentFlow = e.Flow;

            isBindingUpdatedOutsideOfControl = false;
        }

        private void SelectionEvents_SelectedLocationChanged(object sender, LocationChangedEventArgs e)
        {
            isBindingUpdatedOutsideOfControl = true;

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

            isBindingUpdatedOutsideOfControl = false;
        }

        private IReadOnlyList<ILocationListItem> BuildLocationListItems(IAnalysisIssueFlowVisualization flow)
        {
            var listItems = new List<ILocationListItem>();
            var flowLocations = flow?.Locations.ToList();

            if (flowLocations != null && flowLocations.Any())
            {
                for (var i = 0; i < flowLocations.Count; i++)
                {
                    var filePath = flowLocations[i].Location.FilePath;
                    var fileIcon = vsImageService.GetImageMonikerForFile(filePath);
                    
                    listItems.Add(new FileNameLocationListItem(Path.GetFileName(filePath), fileIcon));

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

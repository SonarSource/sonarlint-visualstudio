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
using System.Linq;
using System.Runtime.CompilerServices;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels
{
    internal sealed class IssueVisualizationViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IAnalysisIssueSelectionService selectionService;
        private readonly IRuleHelpLinkProvider ruleHelpLinkProvider;
        private readonly ILocationNavigator locationNavigator;
        private readonly IFileNameLocationListItemCreator fileNameLocationListItemCreator;

        private IAnalysisIssueVisualization currentIssue;
        private IAnalysisIssueFlowVisualization currentFlow;
        private LocationListItem currentLocationListItem;

        /// <summary>
        /// There is a two-way binding between the panel and the selectionService - this is a way to avoid the infinite recursion.
        /// Value changed in UI --> the view model updates the property in selectionService --> selectionService raises an event of SelectionChanged --> view model listens to it and calls NotifyPropertyChanged
        /// </summary>
        private bool isBindingUpdatedByUI = true;

        private bool pausePropertyChangeNotifications;

        public IssueVisualizationViewModel(IAnalysisIssueSelectionService selectionService, 
            IRuleHelpLinkProvider ruleHelpLinkProvider,
            ILocationNavigator locationNavigator,
            IFileNameLocationListItemCreator fileNameLocationListItemCreator)
        {
            this.selectionService = selectionService;
            this.ruleHelpLinkProvider = ruleHelpLinkProvider;
            this.locationNavigator = locationNavigator;
            this.fileNameLocationListItemCreator = fileNameLocationListItemCreator;

            selectionService.SelectionChanged += SelectionEvents_SelectionChanged;

            UpdateState(SelectionChangeLevel.Issue, 
                selectionService.SelectedIssue, 
                selectionService.SelectedFlow,
                selectionService.SelectedLocation);
        }

        public string Description => CurrentIssue?.Issue?.Message;

        public string RuleKey => CurrentIssue?.Issue?.RuleKey;

        public string RuleHelpLink => string.IsNullOrEmpty(RuleKey) ? null : ruleHelpLinkProvider.GetHelpLink(RuleKey);

        public AnalysisIssueSeverity Severity => CurrentIssue?.Issue?.Severity ?? AnalysisIssueSeverity.Info;

        public IAnalysisIssueVisualization CurrentIssue
        {
            get => currentIssue;
            private set
            {
                if (currentIssue != value)
                {
                    currentIssue = value;
                    // Trigger PropertyChanged for all properties
                    NotifyPropertyChanged(string.Empty);
                }
            }
        }

        public IAnalysisIssueFlowVisualization CurrentFlow
        {
            get => currentFlow;
            set
            {
                if (isBindingUpdatedByUI)
                {
                    selectionService.SelectedFlow = value;
                }
                else if (currentFlow != value)
                {
                    currentFlow = value;
                    NotifyPropertyChanged();

                    UpdateLocationsList();
                }
            }
        }

        public IReadOnlyList<ILocationListItem> LocationListItems { get; private set; }

        public LocationListItem CurrentLocationListItem
        {
            get => currentLocationListItem;
            set
            {
                if (isBindingUpdatedByUI)
                {
                    selectionService.SelectedLocation = value?.Location;
                }
                else if (currentLocationListItem != value)
                {
                    currentLocationListItem = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private void SelectionEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateState(e.SelectionChangeLevel, e.SelectedIssue, e.SelectedFlow, e.SelectedLocation);
        }

        private void UpdateState(SelectionChangeLevel selectionChangeLevel, 
            IAnalysisIssueVisualization selectedIssue, 
            IAnalysisIssueFlowVisualization selectedFlow,
            IAnalysisIssueLocationVisualization selectedLocation)
        {
            isBindingUpdatedByUI = false;

            if (selectionChangeLevel == SelectionChangeLevel.Issue)
            {
                pausePropertyChangeNotifications = true;
            }

            CurrentFlow = selectedFlow;

            if (selectionChangeLevel == SelectionChangeLevel.Issue)
            {
                pausePropertyChangeNotifications = false;
                CurrentIssue = selectedIssue;
            }

            NavigateToLocation(selectedLocation);

            // Setting the selected location should be done last, after the flow list has been updated, so SelectedItem will be set in the xaml
            CurrentLocationListItem = GetLocationListItem(selectedLocation);

            isBindingUpdatedByUI = true;
        }

        private void NavigateToLocation(IAnalysisIssueLocationVisualization locationVisualization)
        {
            if (locationVisualization == null)
            {
                return;
            }

            locationVisualization.IsNavigable = locationNavigator.TryNavigate(locationVisualization.Location);
        }

        private void UpdateLocationsList()
        {
            if (LocationListItems != null)
            {
                foreach (var disposable in LocationListItems.OfType<IDisposable>())
                {
                    disposable.Dispose();
                }
            }

            LocationListItems = BuildLocationListItems(currentFlow);

            NotifyPropertyChanged(nameof(LocationListItems));
        }

        /// <summary>
        /// This method groups all sequential locations under the same file path and returns a single list with File nodes and Location nodes.
        /// This way the UI can use different data templates to make a flat list look like a hierarchical tree.
        /// </summary>
        private IReadOnlyList<ILocationListItem> BuildLocationListItems(IAnalysisIssueFlowVisualization flow)
        {
            var listItems = new List<ILocationListItem>();
            var flowLocations = flow?.Locations.ToList();

            if (flowLocations != null && flowLocations.Any())
            {
                for (var i = 0; i < flowLocations.Count; i++)
                {
                    var location = flowLocations[i];

                    listItems.Add(fileNameLocationListItemCreator.Create(location));

                    var sequentialLocations = flowLocations
                        .Skip(i)
                        .TakeWhile(x => x.FilePath == location.FilePath)
                        .ToList();

                    listItems.AddRange(sequentialLocations.Select(x => (ILocationListItem) new LocationListItem(x)));

                    i += sequentialLocations.Count - 1;
                }
            }

            return listItems;
        }

        private LocationListItem GetLocationListItem(IAnalysisIssueLocationVisualization locationViz)
        {
            if (locationViz == null)
            {
                return null;
            }

            var selectedLocationListItem = LocationListItems?
                .OfType<LocationListItem>()
                .FirstOrDefault(x => x.Location == locationViz);

            return selectedLocationListItem;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (!pausePropertyChangeNotifications)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Dispose()
        {
            selectionService.SelectionChanged -= SelectionEvents_SelectionChanged;
        }
    }
}

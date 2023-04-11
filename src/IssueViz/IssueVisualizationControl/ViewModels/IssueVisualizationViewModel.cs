/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Runtime.CompilerServices;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels
{
    internal interface IIssueVisualizationViewModel : INotifyPropertyChanged, IDisposable
    {
        bool HasNonNavigableLocations { get; }
        int? LineNumber { get; }
        string FileName { get; }
        string Description { get; }
        string RuleKey { get; }
        string RuleDescriptionContextKey { get; }

        AnalysisIssueSeverity Severity { get; }
        IAnalysisIssueVisualization CurrentIssue { get; }
        IAnalysisIssueFlowVisualization CurrentFlow { get; set; }
        IReadOnlyList<ILocationListItem> LocationListItems { get; }
        LocationListItem CurrentLocationListItem { get; set; }
        INavigateToCodeLocationCommand NavigateToCodeLocationCommand { get; }
        INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; }
        INavigateToDocumentationCommand NavigateToDocumentationCommand { get; }
    }

    internal sealed class IssueVisualizationViewModel : IIssueVisualizationViewModel
    {
        private readonly IAnalysisIssueSelectionService selectionService;
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
            ILocationNavigator locationNavigator,
            IFileNameLocationListItemCreator fileNameLocationListItemCreator,
            INavigateToCodeLocationCommand navigateToCodeLocationCommand,
            INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
            INavigateToDocumentationCommand navigateToDocumentationCommand)
        {
            this.selectionService = selectionService;
            this.locationNavigator = locationNavigator;
            this.fileNameLocationListItemCreator = fileNameLocationListItemCreator;

            NavigateToCodeLocationCommand = navigateToCodeLocationCommand;
            NavigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
            NavigateToDocumentationCommand = navigateToDocumentationCommand;

            selectionService.SelectionChanged += SelectionEvents_OnSelectionChanged;

            UpdateState(SelectionChangeLevel.Issue,
                selectionService.SelectedIssue,
                selectionService.SelectedFlow,
                selectionService.SelectedLocation);
        }

        public INavigateToCodeLocationCommand NavigateToCodeLocationCommand { get; }

        public INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; }

        public INavigateToDocumentationCommand NavigateToDocumentationCommand { get; }

        public bool HasNonNavigableLocations { get; private set; }

        public int? LineNumber
        {
            get
            {
                var issueSpan = CurrentIssue?.Span;

                if (issueSpan == null || issueSpan.Value.IsEmpty)
                {
                    return null;
                }

                var position = issueSpan.Value.Start;
                var line = position.GetContainingLine();

                return line.LineNumber + 1;
            }
        }

        public string FileName => Path.GetFileName(CurrentIssue?.CurrentFilePath);

        public string Description => CurrentIssue?.Issue?.PrimaryLocation.Message;

        public string RuleKey => CurrentIssue?.Issue?.RuleKey;

        public string RuleDescriptionContextKey => CurrentIssue?.Issue?.RuleDescriptionContextKey;

        public AnalysisIssueSeverity Severity =>
            CurrentIssue?.Issue is IAnalysisIssue analysisIssue
                ? analysisIssue.Severity
                : AnalysisIssueSeverity.Info;

        public IAnalysisIssueVisualization CurrentIssue
        {
            get => currentIssue;
            private set
            {
                if (currentIssue != value)
                {
                    if (currentIssue != null)
                    {
                        UnsubscribeFromIssuePropertyChanges();
                    }

                    currentIssue = value;
                    CalculateNonNavigableLocations();

                    if (currentIssue != null)
                    {
                        SubscribeToIssuePropertyChanges();
                    }

                    // Trigger PropertyChanged for all properties
                    NotifyPropertyChanged(string.Empty);
                }
            }
        }

        private void SubscribeToIssuePropertyChanges()
        {
            currentIssue.PropertyChanged += CurrentIssue_OnPropertyChanged;

            foreach (var location in currentIssue.GetSecondaryLocations())
            {
                location.PropertyChanged += Location_PropertyChanged;
            }
        }

        private void UnsubscribeFromIssuePropertyChanges()
        {
            currentIssue.PropertyChanged -= CurrentIssue_OnPropertyChanged;

            foreach (var location in currentIssue.GetSecondaryLocations())
            {
                location.PropertyChanged -= Location_PropertyChanged;
            }
        }

        private void CurrentIssue_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IAnalysisIssueVisualization.Span):
                    NotifyPropertyChanged(nameof(LineNumber));
                    CalculateNonNavigableLocations();
                    break;

                case nameof(IAnalysisIssueVisualization.CurrentFilePath):
                    NotifyPropertyChanged(nameof(FileName));
                    break;
            }
        }

        private void Location_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IAnalysisIssueLocationVisualization.Span))
            {
                CalculateNonNavigableLocations();
            }
        }

        private void CalculateNonNavigableLocations()
        {
            HasNonNavigableLocations = CurrentIssue != null &&
                                       CurrentIssue.GetAllLocations().Any(x => !x.IsNavigable());

            NotifyPropertyChanged(nameof(HasNonNavigableLocations));
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
                    NavigateToLocation(value?.Location);

                    selectionService.SelectedLocation = value?.Location;
                }
                else if (currentLocationListItem != value)
                {
                    currentLocationListItem = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private void SelectionEvents_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
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

            locationNavigator.TryNavigate(locationVisualization);
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
                        .TakeWhile(x => x.CurrentFilePath == location.CurrentFilePath)
                        .ToList();

                    listItems.AddRange(sequentialLocations.Select(x => (ILocationListItem)new LocationListItem(x)));

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
            if (currentIssue != null)
            {
                UnsubscribeFromIssuePropertyChanges();
            }

            selectionService.SelectionChanged -= SelectionEvents_OnSelectionChanged;
        }
    }
}

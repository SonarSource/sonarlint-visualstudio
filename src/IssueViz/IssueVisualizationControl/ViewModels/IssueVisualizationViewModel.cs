﻿/*
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
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels
{
    internal sealed class IssueVisualizationViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IAnalysisIssueSelectionService selectionEvents;
        private readonly IVsImageService2 vsImageService;
        private readonly IRuleHelpLinkProvider ruleHelpLinkProvider;
        private readonly ILogger logger;

        private IAnalysisIssueVisualization currentIssue;
        private IAnalysisIssueFlowVisualization currentFlow;
        private LocationListItem currentLocation;

        /// <summary>
        /// There is a two-way binding between the panel and the selectionService - this is a way to avoid the infinite recursion.
        /// Value changed in UI --> the view model updates the property in selectionService --> selectionService raises an event of SelectionChanged --> view model listens to it and calls NotifyPropertyChanged
        /// </summary>
        private bool isBindingUpdatedByUI = true;

        private bool pausePropertyChangeNotifications;

        public IssueVisualizationViewModel(IAnalysisIssueSelectionService selectionEvents, IVsImageService2 vsImageService, IRuleHelpLinkProvider ruleHelpLinkProvider, ILogger logger)
        {
            this.selectionEvents = selectionEvents;
            this.vsImageService = vsImageService;
            this.ruleHelpLinkProvider = ruleHelpLinkProvider;
            this.logger = logger;

            selectionEvents.SelectionChanged += SelectionEvents_SelectionChanged;
        }

        public string Description => currentIssue?.Issue?.Message;

        public string RuleKey => currentIssue?.Issue?.RuleKey;

        public string RuleHelpLink => string.IsNullOrEmpty(RuleKey) ? null : ruleHelpLinkProvider.GetHelpLink(RuleKey);

        public AnalysisIssueSeverity Severity => currentIssue?.Issue?.Severity ?? AnalysisIssueSeverity.Info;

        public IAnalysisIssueVisualization CurrentIssue
        {
            get => currentIssue;
            private set
            {
                if (currentIssue != value)
                {
                    currentIssue = value;
                    // Trigger PropertyChanged for all properties
                    OnPropertyChanged(string.Empty);
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
                    selectionEvents.SelectedFlow = value;
                }
                else if (currentFlow != value)
                {
                    currentFlow = value;
                    OnPropertyChanged();

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
                if (isBindingUpdatedByUI)
                {
                    selectionEvents.SelectedLocation = value?.Location;
                }
                else if (currentLocation != value)
                {
                    currentLocation = value;
                    OnPropertyChanged();
                }
            }
        }

        private void SelectionEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            isBindingUpdatedByUI = false;

            if (e.SelectionChangeLevel == SelectionChangeLevel.Issue)
            {
                pausePropertyChangeNotifications = true;
            }

            CurrentFlow = e.SelectedFlow;

            pausePropertyChangeNotifications = false;

            if (e.SelectedLocation == null)
            {
                CurrentLocationListItem = null;
            }
            else
            {
                var selectedLocationListItem = LocationListItems?
                    .OfType<LocationListItem>()
                    .FirstOrDefault(x => x.Location == e.SelectedLocation);

                CurrentLocationListItem = selectedLocationListItem;
            }

            CurrentIssue = e.SelectedIssue;

            isBindingUpdatedByUI = true;
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
                    var filePath = flowLocations[i].Location.FilePath;
                    var fileIcon = GetImageMonikerForFile(filePath);
                    listItems.Add(new FileNameLocationListItem(filePath, Path.GetFileName(filePath), fileIcon));

                    var sequentialLocations = flowLocations.Skip(i).TakeWhile(x => x.Location.FilePath == filePath).ToList();
                    listItems.AddRange(sequentialLocations.Select(x => (ILocationListItem)new LocationListItem(x)));

                    i += sequentialLocations.Count - 1;
                }
            }

            return listItems;
        }

        private object GetImageMonikerForFile(string filePath)
        {
            try
            {
                return vsImageService.GetImageMonikerForFile(filePath);
            }
            catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
            {
                logger.WriteLine(Resources.ERR_FailedToGetFileImageMoniker, filePath, e);

                return KnownMonikers.Blank;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (!pausePropertyChangeNotifications)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Dispose()
        {
            selectionEvents.SelectionChanged -= SelectionEvents_SelectionChanged;
        }
    }
}

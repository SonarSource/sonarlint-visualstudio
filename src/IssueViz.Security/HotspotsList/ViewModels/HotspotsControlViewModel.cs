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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.SelectionService;
using SonarLint.VisualStudio.IssueVisualization.Security.Store;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.ViewModels
{
    internal interface IHotspotsControlViewModel : IDisposable
    {
        ObservableCollection<IHotspotViewModel> Hotspots { get; }

        IHotspotViewModel SelectedHotspot { get; }

        ICommand NavigateCommand { get; }

        ICommand DeleteCommand { get; }
    }

    internal sealed class HotspotsControlViewModel : IHotspotsControlViewModel, INotifyPropertyChanged
    {
        private readonly object Lock = new object();
        private readonly INotifyCollectionChanged readonlyObservableHotspotsCollection;
        private readonly IHotspotsSelectionService selectionService;

        public ObservableCollection<IHotspotViewModel> Hotspots { get; } = new ObservableCollection<IHotspotViewModel>();

        public IHotspotViewModel SelectedHotspot { get; set; }

        public ICommand NavigateCommand { get; private set; }

        public ICommand DeleteCommand { get; private set; }

        public HotspotsControlViewModel(IHotspotsStore hotspotsStore, 
            ILocationNavigator locationNavigator, 
            IHotspotsSelectionService selectionService)
        {
            AllowMultiThreadedAccessToHotspotsList();

            this.selectionService = selectionService;
            selectionService.SelectionChanged += SelectionService_SelectionChanged;

            var allHotspots = hotspotsStore.GetAll();
            readonlyObservableHotspotsCollection = allHotspots;
            readonlyObservableHotspotsCollection.CollectionChanged += HotspotsStore_CollectionChanged;

            UpdateHotspotsList(allHotspots, Array.Empty<IAnalysisIssueVisualization>());

            SetCommands(hotspotsStore, locationNavigator);
        }

        /// <summary>
        /// Allow the observable collection <see cref="Hotspots"/> to be modified from non-UI thread. 
        /// </summary>
        private void AllowMultiThreadedAccessToHotspotsList()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            BindingOperations.EnableCollectionSynchronization(Hotspots, Lock);
        }

        private void SetCommands(IHotspotsStore hotspotsStore, ILocationNavigator locationNavigator)
        {
            NavigateCommand = new DelegateCommand(parameter =>
            {
                var selectedHotspot = (IHotspotViewModel) parameter;
                locationNavigator.TryNavigate(selectedHotspot.Hotspot);
            }, parameter => parameter is IHotspotViewModel);

            DeleteCommand = new DelegateCommand(parameter =>
        {
                var selectedHotspot = (IHotspotViewModel) parameter;
                hotspotsStore.Delete(selectedHotspot.Hotspot);
            }, parameter => parameter is IHotspotViewModel);
        }

        private void UpdateHotspotsList(IEnumerable<IAnalysisIssueVisualization> addedHotspots, IEnumerable<IAnalysisIssueVisualization> deletedHotspots)
        {
            foreach (var addedHotspot in addedHotspots)
            {
                Hotspots.Add(new HotspotViewModel(addedHotspot));
            }

            var viewModelsToDelete = Hotspots.Where(x => deletedHotspots.Contains(x.Hotspot)).ToList();

            foreach (var deletedHotspot in viewModelsToDelete)
            {
                Hotspots.Remove(deletedHotspot);
            }
        }

        private void HotspotsStore_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var addedHotspots = e.NewItems ?? Array.Empty<IAnalysisIssueVisualization>();
            var deletedHotspots = e.OldItems ?? Array.Empty<IAnalysisIssueVisualization>();

            UpdateHotspotsList(
                addedHotspots.Cast<IAnalysisIssueVisualization>(),
                deletedHotspots.Cast<IAnalysisIssueVisualization>());
        }

        private void SelectionService_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedHotspot = Hotspots.FirstOrDefault(x => x.Hotspot == e.SelectedHotspot);
            NotifyPropertyChanged(nameof(SelectedHotspot));
        }

        public void Dispose()
        {
            readonlyObservableHotspotsCollection.CollectionChanged -= HotspotsStore_CollectionChanged;
            selectionService.SelectionChanged -= SelectionService_SelectionChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

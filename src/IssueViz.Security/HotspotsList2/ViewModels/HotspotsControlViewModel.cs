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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.SelectionService;
using SonarLint.VisualStudio.IssueVisualization.Security.Store;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList2.ViewModels
{
    internal interface IHotspotsControlViewModel : IDisposable
    {
        ObservableCollection<IHotspotViewModel> Hotspots { get; }

        IHotspotViewModel SelectedHotspot { get; }

        ICommand NavigateCommand { get; }
    }

    internal sealed class HotspotsControlViewModel : IHotspotsControlViewModel, INotifyPropertyChanged
    {
        private readonly object Lock = new object();
        private readonly INotifyCollectionChanged readonlyObservableHotspotsCollection;
        private readonly IHotspotsSelectionService selectionService;

        public ObservableCollection<IHotspotViewModel> Hotspots { get; } = new ObservableCollection<IHotspotViewModel>();

        public IHotspotViewModel SelectedHotspot { get; set; }

        public ICommand NavigateCommand { get; }

        public HotspotsControlViewModel(IHotspotsStore hotspotsStore, ICommand navigateCommand, IHotspotsSelectionService selectionService)
        {
            BindingOperations.EnableCollectionSynchronization(Hotspots, Lock);

            this.selectionService = selectionService;
            selectionService.SelectionChanged += SelectionService_SelectionChanged;

            var allHotspots = hotspotsStore.GetAll();
            readonlyObservableHotspotsCollection = allHotspots;
            readonlyObservableHotspotsCollection.CollectionChanged += HotspotsStore_CollectionChanged;

            NavigateCommand = navigateCommand;
            UpdateHotspotsList(allHotspots);
        }

        private void UpdateHotspotsList(IEnumerable<IAnalysisIssueVisualization> hotspots)
        {
            foreach (var addedHotspot in hotspots)
            {
                Hotspots.Add(new HotspotViewModel(addedHotspot));
            }
        }

        private void HotspotsStore_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // todo: handle deletion
            UpdateHotspotsList(e.NewItems.Cast<IAnalysisIssueVisualization>());
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

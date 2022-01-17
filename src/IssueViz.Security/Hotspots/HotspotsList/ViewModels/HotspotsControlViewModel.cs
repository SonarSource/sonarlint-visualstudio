/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels
{
    internal interface IHotspotsControlViewModel : IDisposable
    {
        ObservableCollection<IHotspotViewModel> Hotspots { get; }

        IHotspotViewModel SelectedHotspot { get; }

        ICommand NavigateCommand { get; }

        ICommand RemoveCommand { get; }
    }

    internal sealed class HotspotsControlViewModel : IHotspotsControlViewModel, INotifyPropertyChanged
    {
        private readonly object Lock = new object();
        private readonly IIssueSelectionService selectionService;
        private readonly IHotspotsStore store;
        private IHotspotViewModel selectedHotspot;

        public ObservableCollection<IHotspotViewModel> Hotspots { get; } = new ObservableCollection<IHotspotViewModel>();

        public ICommand NavigateCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }

        public HotspotsControlViewModel(IHotspotsStore hotspotsStore,
            ILocationNavigator locationNavigator,
            IIssueSelectionService selectionService)
        {
            AllowMultiThreadedAccessToHotspotsList();

            this.selectionService = selectionService;
            selectionService.SelectedIssueChanged += SelectionService_SelectionChanged;

            this.store = hotspotsStore;
            store.IssuesChanged += Store_IssuesChanged;

            UpdateHotspotsList();

            SetCommands(hotspotsStore, locationNavigator);
        }

        public IHotspotViewModel SelectedHotspot
        {
            get => selectedHotspot;
            set
            {
                if (selectedHotspot != value)
                {
                    selectedHotspot = value;
                    selectionService.SelectedIssue = selectedHotspot?.Hotspot;
                }
            }
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
                var hotspot = (IHotspotViewModel)parameter;
                locationNavigator.TryNavigate(hotspot.Hotspot);
            }, parameter => parameter is IHotspotViewModel);

            RemoveCommand = new DelegateCommand(parameter =>
            {
                var hotspot = (IHotspotViewModel)parameter;
                hotspotsStore.Remove(hotspot.Hotspot);
            }, parameter => parameter is IHotspotViewModel);
        }

        private void UpdateHotspotsList()
        {
            Hotspots.Clear();

            foreach (var issueViz in store.GetAll())
            {
                Hotspots.Add(new HotspotViewModel(issueViz));
            }
        }

        private void Store_IssuesChanged(object sender, IssuesChangedEventArgs e)
        {
            UpdateHotspotsList();
        }

        private void SelectionService_SelectionChanged(object sender, EventArgs e)
        {
            selectedHotspot = Hotspots.FirstOrDefault(x => x.Hotspot == selectionService.SelectedIssue);
            NotifyPropertyChanged(nameof(SelectedHotspot));
        }

        public void Dispose()
        {
            store.IssuesChanged -= Store_IssuesChanged;
            selectionService.SelectedIssueChanged -= SelectionService_SelectionChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

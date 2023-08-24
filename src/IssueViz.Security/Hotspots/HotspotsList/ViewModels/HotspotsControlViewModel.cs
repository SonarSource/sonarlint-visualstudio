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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels
{
    internal interface IHotspotsControlViewModel : IDisposable
    {
        ObservableCollection<IHotspotViewModel> Hotspots { get; }

        IHotspotViewModel SelectedHotspot { get; }

        ICommand NavigateCommand { get; }
        
        INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; }
    }

    internal sealed class HotspotsControlViewModel : IHotspotsControlViewModel, INotifyPropertyChanged
    {
        private readonly object Lock = new object();
        private readonly IIssueSelectionService selectionService;
        private readonly IThreadHandling threadHandling;
        private readonly ILocalHotspotsStore store;
        private IHotspotViewModel selectedHotspot;

        public ObservableCollection<IHotspotViewModel> Hotspots { get; } = new ObservableCollection<IHotspotViewModel>();

        public ICommand NavigateCommand { get; private set; }
        public INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; }

        public HotspotsControlViewModel(ILocalHotspotsStore hotspotsStore,
            INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
            ILocationNavigator locationNavigator,
            IIssueSelectionService selectionService,
            IThreadHandling threadHandling)
        {
            this.threadHandling = threadHandling;
            AllowMultiThreadedAccessToHotspotsList();

            this.selectionService = selectionService;
            selectionService.SelectedIssueChanged += SelectionService_SelectionChanged;

            this.store = hotspotsStore;
            store.IssuesChanged += Store_IssuesChanged;

            NavigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
            SetCommands(locationNavigator);
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

        public async System.Threading.Tasks.Task UpdateHotspotsListAsync()
        {
            await threadHandling.RunOnBackgroundThread( () =>
            {
                Hotspots.Clear();
                foreach (var localHotspot in store.GetAllLocalHotspots())
                {
                    Hotspots.Add(new HotspotViewModel(localHotspot.Visualization, localHotspot.Priority));
                }

                return System.Threading.Tasks.Task.FromResult(true);
            });
        }

        /// <summary>
        /// Allow the observable collection <see cref="Hotspots"/> to be modified from non-UI thread. 
        /// </summary>
        private void AllowMultiThreadedAccessToHotspotsList()
        {
            threadHandling.ThrowIfNotOnUIThread();
            BindingOperations.EnableCollectionSynchronization(Hotspots, Lock);
        }

        private void SetCommands(ILocationNavigator locationNavigator)
        {
            NavigateCommand = new DelegateCommand(parameter =>
            {
                var hotspot = (IHotspotViewModel)parameter;
                locationNavigator.TryNavigate(hotspot.Hotspot);
            }, parameter => parameter is IHotspotViewModel);
        }

        private void Store_IssuesChanged(object sender, IssuesChangedEventArgs e)
        {
            UpdateHotspotsListAsync().Forget();
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

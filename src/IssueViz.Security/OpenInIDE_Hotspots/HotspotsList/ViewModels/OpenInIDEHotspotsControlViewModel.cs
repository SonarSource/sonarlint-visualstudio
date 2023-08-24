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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE_Hotspots.HotspotsList.ViewModels
{
    internal interface IOpenInIDEHotspotsControlViewModel : IDisposable
    {
        ObservableCollection<IOpenInIDEHotspotViewModel> Hotspots { get; }

        IOpenInIDEHotspotViewModel SelectedHotspot { get; }

        ICommand NavigateCommand { get; }

        ICommand RemoveCommand { get; }

        INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; }
    }

    internal sealed class OpenInIDEHotspotsControlViewModel : IOpenInIDEHotspotsControlViewModel, INotifyPropertyChanged
    {
        private readonly object Lock = new object();
        private readonly IIssueSelectionService selectionService;
        private readonly IOpenInIDEHotspotsStore store;
        private readonly IThreadHandling threadHandling;
        private IOpenInIDEHotspotViewModel selectedHotspot;


        public ObservableCollection<IOpenInIDEHotspotViewModel> Hotspots { get; } = new ObservableCollection<IOpenInIDEHotspotViewModel>();

        public ICommand NavigateCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }

        public INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; private set; }

        public OpenInIDEHotspotsControlViewModel(IOpenInIDEHotspotsStore hotspotsStore,
            ILocationNavigator locationNavigator,
            IIssueSelectionService selectionService,
            INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand)
            : this(hotspotsStore,
                  locationNavigator,
                  selectionService,
                  navigateToRuleDescriptionCommand,
                  ThreadHandling.Instance)
        { }

        internal /* for testing */ OpenInIDEHotspotsControlViewModel(IOpenInIDEHotspotsStore hotspotsStore,
            ILocationNavigator locationNavigator,
            IIssueSelectionService selectionService,
            INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
            IThreadHandling threadHandling)
        {
            this.threadHandling = threadHandling;
            AllowMultiThreadedAccessToHotspotsList();

            this.selectionService = selectionService;
            selectionService.SelectedIssueChanged += SelectionService_SelectionChanged;

            this.store = hotspotsStore;
            store.IssuesChanged += Store_IssuesChanged;

            UpdateHotspotsList();

            SetCommands(hotspotsStore, locationNavigator);
            this.NavigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
        }

        public IOpenInIDEHotspotViewModel SelectedHotspot
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
            threadHandling.ThrowIfNotOnUIThread();
            BindingOperations.EnableCollectionSynchronization(Hotspots, Lock);
        }

        private void SetCommands(IOpenInIDEHotspotsStore hotspotsStore, ILocationNavigator locationNavigator)
        {
            NavigateCommand = new DelegateCommand(parameter =>
            {
                var hotspot = (IOpenInIDEHotspotViewModel)parameter;
                locationNavigator.TryNavigate(hotspot.Hotspot);
            }, parameter => parameter is IOpenInIDEHotspotViewModel);

            RemoveCommand = new DelegateCommand(parameter =>
            {
                var hotspot = (IOpenInIDEHotspotViewModel)parameter;
                hotspotsStore.Remove(hotspot.Hotspot);
            }, parameter => parameter is IOpenInIDEHotspotViewModel);
        }

        private void UpdateHotspotsList()
        {
            Hotspots.Clear();

            foreach (var issueViz in store.GetAll())
            {
                Hotspots.Add(new OpenInIDEHotspotViewModel(issueViz));
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

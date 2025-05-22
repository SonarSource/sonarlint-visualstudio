/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels
{
    internal sealed class HotspotsControlViewModel : ViewModelBase
    {
        private static readonly ObservableCollection<LocationFilterViewModel> _locationFilterViewModels =
        [
            new(LocationFilter.CurrentDocument, Resources.HotspotsControl_CurrentDocumentFilter),
            new(LocationFilter.OpenDocuments, Resources.HotspotsControl_OpenDocumentsFilter),
        ];
        private readonly object Lock = new object();
        private readonly IIssueSelectionService selectionService;
        private readonly IThreadHandling threadHandling;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IReviewHotspotsService reviewHotspotsService;
        private readonly IMessageBox messageBox;
        private readonly IActiveDocumentTracker activeDocumentTracker;
        private readonly ILocalHotspotsStore store;
        private IHotspotViewModel selectedHotspot;
        private readonly ObservableCollection<IHotspotViewModel> hotspots = new();
        private readonly ObservableCollection<IHotspotViewModel> filteredHotspots = new();
        private ICommand navigateCommand;
        private readonly INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand;
        private bool isCloud;
        private LocationFilterViewModel selectedLocationFilter = _locationFilterViewModels.Single(x => x.LocationFilter == LocationFilter.CurrentDocument);
        private string activeDocumentFilePath;

        public ObservableCollection<IHotspotViewModel> Hotspots => filteredHotspots;

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

        public ObservableCollection<LocationFilterViewModel> LocationFilters => _locationFilterViewModels;

        public ObservableCollection<PriorityFilterViewModel> PriorityFilters { get; } =
            new(Enum.GetValues(typeof(HotspotPriority)).Cast<HotspotPriority>().Select(x => new PriorityFilterViewModel(x)));

        public LocationFilterViewModel SelectedLocationFilter
        {
            get => selectedLocationFilter;
            set
            {
                selectedLocationFilter = value;
                RaisePropertyChanged();
                RefreshFiltering();
            }
        }

        public ICommand NavigateCommand
        {
            get => navigateCommand;
            private set => navigateCommand = value;
        }

        public INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand => navigateToRuleDescriptionCommand;

        public bool IsCloud
        {
            get => isCloud;
            private set
            {
                isCloud = value;
                RaisePropertyChanged();
            }
        }

        public HotspotsControlViewModel(
            ILocalHotspotsStore hotspotsStore,
            INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
            ILocationNavigator locationNavigator,
            IIssueSelectionService selectionService,
            IThreadHandling threadHandling,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IReviewHotspotsService reviewHotspotsService,
            IMessageBox messageBox,
            IActiveDocumentLocator activeDocumentLocator,
            IActiveDocumentTracker activeDocumentTracker)
        {
            this.threadHandling = threadHandling;
            AllowMultiThreadedAccessToHotspotsList();

            this.selectionService = selectionService;
            selectionService.SelectedIssueChanged += SelectionService_SelectionChanged;

            store = hotspotsStore;
            store.IssuesChanged += Store_IssuesChanged;

            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;

            this.reviewHotspotsService = reviewHotspotsService;
            this.messageBox = messageBox;

            activeDocumentFilePath = activeDocumentLocator.FindActiveDocument()?.FilePath;
            this.activeDocumentTracker = activeDocumentTracker;
            activeDocumentTracker.ActiveDocumentChanged += OnActiveDocumentChanged;

            this.navigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
            SetCommands(locationNavigator);

            SelectedLocationFilter = LocationFilters.Single(x => x.LocationFilter == LocationFilter.CurrentDocument);
        }

        public async Task UpdateHotspotsListAsync() =>
            await threadHandling.RunOnBackgroundThread(() =>
            {
                hotspots.Clear();
                foreach (var localHotspot in store.GetAllLocalHotspots())
                {
                    hotspots.Add(new HotspotViewModel(localHotspot.Visualization, localHotspot.Priority, localHotspot.HotspotStatus));
                }
                RefreshFiltering();

                return Task.FromResult(true);
            });

        public async Task<IEnumerable<HotspotStatus>> GetAllowedStatusesAsync()
        {
            var response = await reviewHotspotsService.CheckReviewHotspotPermittedAsync(SelectedHotspot.Hotspot.Issue.IssueServerKey);
            switch (response)
            {
                case ReviewHotspotPermittedArgs reviewHotspotPermittedArgs:
                    return reviewHotspotPermittedArgs.AllowedStatuses;
                case ReviewHotspotNotPermittedArgs reviewHotspotNotPermittedArgs:
                    messageBox.Show(string.Format(Resources.ReviewHotspotWindow_CheckReviewPermittedFailureMessage, reviewHotspotNotPermittedArgs.Reason), Resources.ReviewHotspotWindow_FailureTitle,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
            return null;
        }

        public async Task ChangeHotspotStatusAsync(HotspotStatus newStatus)
        {
            var wasChanged = await reviewHotspotsService.ReviewHotspotAsync(SelectedHotspot.Hotspot.Issue.IssueServerKey, newStatus);
            if (!wasChanged)
            {
                messageBox.Show(Resources.ReviewHotspotWindow_ReviewFailureMessage, Resources.ReviewHotspotWindow_FailureTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (newStatus is HotspotStatus.Fixed or HotspotStatus.Safe)
            {
                hotspots.Remove(SelectedHotspot);
                RefreshFiltering();
            }
        }

        public void UpdatePriorityFilter(PriorityFilterViewModel viewModel, bool isSelected)
        {
            viewModel.IsSelected = isSelected;
            RefreshFiltering();
        }

        /// <summary>
        /// Allow the observable collection <see cref="Hotspots"/> to be modified from non-UI thread.
        /// </summary>
        private void AllowMultiThreadedAccessToHotspotsList() => threadHandling.RunOnUIThread(() => BindingOperations.EnableCollectionSynchronization(Hotspots, Lock));

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
            selectedHotspot = hotspots.FirstOrDefault(x => x.Hotspot == selectionService.SelectedIssue);
            RaisePropertyChanged(nameof(SelectedHotspot));
        }

        public void Dispose()
        {
            store.IssuesChanged -= Store_IssuesChanged;
            selectionService.SelectedIssueChanged -= SelectionService_SelectionChanged;
            activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
            activeDocumentTracker.ActiveDocumentChanged -= OnActiveDocumentChanged;
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs args) => IsCloud = args.Configuration?.Project?.ServerConnection is ServerConnection.SonarCloud;

        private void OnActiveDocumentChanged(object sender, ActiveDocumentChangedEventArgs e)
        {
            activeDocumentFilePath = e.ActiveTextDocument?.FilePath;
            RefreshFiltering();
        }

        private void RefreshFiltering()
        {
            UpdateFilteredHotspots();
            RaisePropertyChanged(nameof(Hotspots));
        }

        private void UpdateFilteredHotspots()
        {
            filteredHotspots.Clear();
            var hotspotsToShow = GetHotspotsFilteredByLocationFilter(hotspots.ToList());
            hotspotsToShow = GetHotspotsFilteredByPriorityFilter(hotspotsToShow.ToList());
            hotspotsToShow.ToList().ForEach(filteredHotspots.Add);
        }

        private IEnumerable<IHotspotViewModel> GetHotspotsFilteredByLocationFilter(IReadOnlyList<IHotspotViewModel> source)
        {
            if (SelectedLocationFilter?.LocationFilter == LocationFilter.CurrentDocument)
            {
                return source.Where(x => x.Hotspot.FilePath == activeDocumentFilePath);
            }
            return source;
        }

        private IEnumerable<IHotspotViewModel> GetHotspotsFilteredByPriorityFilter(IReadOnlyList<IHotspotViewModel> source)
        {
            var prioritiesToShow = PriorityFilters.Where(x => x.IsSelected).Select(x => x.HotspotPriority);
            return source.Where(x => prioritiesToShow.Contains(x.HotspotPriority));
        }
    }
}

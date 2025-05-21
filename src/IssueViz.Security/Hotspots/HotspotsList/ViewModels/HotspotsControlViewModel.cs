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
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
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

        bool IsCloud { get; }

        Task<IEnumerable<HotspotStatus>> GetAllowedStatusesAsync();

        Task ChangeHotspotStatusAsync(HotspotStatus newStatus);
    }

    internal sealed class HotspotsControlViewModel : ViewModelBase, IHotspotsControlViewModel
    {
        private readonly object Lock = new object();
        private readonly IIssueSelectionService selectionService;
        private readonly IThreadHandling threadHandling;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IReviewHotspotsService reviewHotspotsService;
        private readonly IMessageBox messageBox;
        private readonly ILocalHotspotsStore store;
        private IHotspotViewModel selectedHotspot;
        private readonly ObservableCollection<IHotspotViewModel> hotspots = new ObservableCollection<IHotspotViewModel>();
        private ICommand navigateCommand;
        private readonly INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand;
        private bool isCloud;

        public ObservableCollection<IHotspotViewModel> Hotspots => hotspots;

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
            IMessageBox messageBox)
        {
            this.threadHandling = threadHandling;
            AllowMultiThreadedAccessToHotspotsList();

            this.selectionService = selectionService;
            selectionService.SelectedIssueChanged += SelectionService_SelectionChanged;

            store = hotspotsStore;
            store.IssuesChanged += Store_IssuesChanged;

            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.reviewHotspotsService = reviewHotspotsService;
            this.messageBox = messageBox;
            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;

            this.navigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
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

        public async Task UpdateHotspotsListAsync()
        {
            await threadHandling.RunOnBackgroundThread(() =>
            {
                Hotspots.Clear();
                foreach (var localHotspot in store.GetAllLocalHotspots())
                {
                    Hotspots.Add(new HotspotViewModel(localHotspot.Visualization, localHotspot.Priority, localHotspot.HotspotStatus));
                }

                return Task.FromResult(true);
            });
        }

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
                Hotspots.Remove(SelectedHotspot);
            }
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
            selectedHotspot = Hotspots.FirstOrDefault(x => x.Hotspot == selectionService.SelectedIssue);
            RaisePropertyChanged(nameof(SelectedHotspot));
        }

        public void Dispose()
        {
            store.IssuesChanged -= Store_IssuesChanged;
            selectionService.SelectedIssueChanged -= SelectionService_SelectionChanged;
            activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs args) => IsCloud = args.Configuration?.Project?.ServerConnection is ServerConnection.SonarCloud;
    }
}

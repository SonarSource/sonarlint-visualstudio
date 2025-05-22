using System.Collections.ObjectModel;
using System.Windows.Input;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;

internal interface IHotspotsControlViewModel : IDisposable
{
    ObservableCollection<IHotspotViewModel> Hotspots { get; }
    IHotspotViewModel SelectedHotspot { get; }

    LocationFilterViewModel SelectedLocationFilter { get; set; }
    ObservableCollection<LocationFilterViewModel> LocationFilters { get; }

    ICommand NavigateCommand { get; }

    INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; }

    bool IsCloud { get; }

    Task<IEnumerable<HotspotStatus>> GetAllowedStatusesAsync();

    Task ChangeHotspotStatusAsync(HotspotStatus newStatus);
}

public enum LocationFilter
{
    CurrentDocument,
    OpenDocuments,
}

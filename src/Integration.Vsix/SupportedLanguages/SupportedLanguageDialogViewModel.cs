using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;

// TODO https://sonarsource.atlassian.net/browse/SLVS-2869 This class is currently using mock data. This exclusion will need to be removed when the real data is loaded in.
[ExcludeFromCodeCoverage]
internal class SupportedLanguageDialogViewModel : ViewModelBase
{
    private static readonly HashSet<PluginStateDto> DisplayedStates =
    [
        PluginStateDto.ACTIVE,
        PluginStateDto.SYNCED,
        PluginStateDto.DOWNLOADING,
        PluginStateDto.FAILED
    ];

    public ObservableCollection<PluginStatusDto> AllPlugins { get; }

    public ObservableCollection<PluginStatusDto> DisplayedPlugins { get; }

    public string PremiumLanguagesTooltip =>
        string.Join(", ", AllPlugins
            .Where(p => p.state == PluginStateDto.PREMIUM)
            .Select(p => p.pluginName)
            .Distinct());

    public SupportedLanguageDialogViewModel()
    {
        AllPlugins = new ObservableCollection<PluginStatusDto>()
        {
            new PluginStatusDto("Python", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "4.23.0", "1.2.3"),
            new PluginStatusDto("CSS", PluginStateDto.FAILED, ArtifactSourceDto.SONARQUBE_CLOUD, null, null),
            new PluginStatusDto("XML", PluginStateDto.PREMIUM, ArtifactSourceDto.SONARQUBE_SERVER, "1.23.0", null),
        };

        DisplayedPlugins = new ObservableCollection<PluginStatusDto>(
            AllPlugins.Where(p => DisplayedStates.Contains(p.state)));
    }
}

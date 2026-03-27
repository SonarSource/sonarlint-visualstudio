/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.SupportedLanguages;

[TestClass]
public class SupportedLanguagesDialogViewModelTests
{
    private static readonly PluginStatusDisplay[] DefaultPluginStatuses =
    [
        new("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "SonarQube for Visual Studio 1.0"),
        new("Python", PluginStateDto.SYNCED, ArtifactSourceDto.SONARQUBE_SERVER, "SonarQube Server 10.8.1"),
        new("JS", PluginStateDto.DOWNLOADING, ArtifactSourceDto.EMBEDDED, string.Empty),
        new("Go", PluginStateDto.FAILED, null, string.Empty),
        new("Java", PluginStateDto.PREMIUM, null, string.Empty),
        new("COBOL", PluginStateDto.PREMIUM, null, string.Empty),
        new("Kotlin", PluginStateDto.UNSUPPORTED, null, string.Empty)
    ];

    private IPluginStatusesStore pluginStatusesStore;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ISLCoreHandler slCoreHandler;
    private IServerConnectionsRepository serverConnectionsRepository;
    private IConnectedModeUIManager connectedModeUiManager;
    private IThreadHandling threadHandling;
    private ITelemetryManager telemetryManager;
    private SupportedLanguagesDialogViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        pluginStatusesStore = Substitute.For<IPluginStatusesStore>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        slCoreHandler = Substitute.For<ISLCoreHandler>();
        serverConnectionsRepository = Substitute.For<IServerConnectionsRepository>();
        connectedModeUiManager = Substitute.For<IConnectedModeUIManager>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        telemetryManager = Substitute.For<ITelemetryManager>();

        pluginStatusesStore.GetAll().Returns(DefaultPluginStatuses);
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope("MySolution"));
        serverConnectionsRepository.TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>()).Returns(false);
        connectedModeUiManager.ShowManageBindingDialogAsync().Returns(Task.FromResult(true));

        testSubject = new SupportedLanguagesDialogViewModel(pluginStatusesStore, activeConfigScopeTracker, slCoreHandler, serverConnectionsRepository, connectedModeUiManager, threadHandling, telemetryManager);
    }

    [TestMethod]
    public void Ctor_InitializesCollectionsFromStore()
    {
        testSubject.AllPlugins.Should().BeEquivalentTo(DefaultPluginStatuses);
        testSubject.DisplayedPlugins.Select(p => p.PluginName).Should().BeEquivalentTo("C#", "Python", "JS", "Go");
        testSubject.PremiumPluginsTooltip.Should().Be(string.Format(Strings.PluginStatuses_PremiumPluginsTooltip, "Java, COBOL"));
        testSubject.FailedPluginsTooltip.Should().Be("Go");
    }

    [TestMethod]
    public void Ctor_StoreReturnsEmpty_CollectionsAreEmpty()
    {
        pluginStatusesStore.GetAll().Returns(Array.Empty<PluginStatusDisplay>());

        var emptyTestSubject = new SupportedLanguagesDialogViewModel(pluginStatusesStore, activeConfigScopeTracker, slCoreHandler, serverConnectionsRepository, connectedModeUiManager, threadHandling, telemetryManager);

        emptyTestSubject.AllPlugins.Should().BeEmpty();
        emptyTestSubject.DisplayedPlugins.Should().BeEmpty();
        emptyTestSubject.PremiumPluginsTooltip.Should().BeEmpty();
        emptyTestSubject.FailedPluginsTooltip.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_SubscribesToEvents()
    {
        pluginStatusesStore.Received(1).PluginStatusesChanged += Arg.Any<EventHandler>();
        serverConnectionsRepository.Received(1).ConnectionChanged += Arg.Any<EventHandler>();
        activeConfigScopeTracker.ReceivedWithAnyArgs(1).CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
    }

    [TestMethod]
    public void Tooltips_WhenNoSpecialPlugins_AreEmpty()
    {
        SimulatePluginStatusesChanged(new PluginStatusDisplay("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, string.Empty));

        testSubject.PremiumPluginsTooltip.Should().BeEmpty();
        testSubject.FailedPluginsTooltip.Should().BeEmpty();
    }

    [TestMethod]
    public void BannerProperties_WhenNoConnection_ShowsPromotionWithSetUpButton()
    {
        SimulatePluginStatusesChanged(new PluginStatusDisplay("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, string.Empty));

        testSubject.IsBannerPromotion.Should().BeTrue();
        testSubject.IsBannerError.Should().BeFalse();
        testSubject.PromotionBannerButtonText.Should().Be(Strings.PluginStatuses_BannerSetUpConnectionButton);
    }

    [TestMethod]
    public void BannerProperties_WhenNotBoundAndHasConnection_ShowsPromotionWithBindButton()
    {
        SimulatePluginStatusesChanged(new PluginStatusDisplay("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, string.Empty));
        SimulateConnectionChanged(new ServerConnection.SonarQube(new Uri("http://localhost")));

        testSubject.IsBannerPromotion.Should().BeTrue();
        testSubject.IsBannerError.Should().BeFalse();
        testSubject.PromotionBannerButtonText.Should().Be(Strings.PluginStatuses_BannerBindProjectButton);
    }

    [TestMethod]
    public void BannerProperties_WhenBound_NoBanner()
    {
        SimulatePluginStatusesChanged(new PluginStatusDisplay("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, string.Empty));
        SimulateConfigurationScopeChanged(new ConfigurationScope("MySolution", SonarProjectId: "project-key"));

        testSubject.IsBannerPromotion.Should().BeFalse();
        testSubject.IsBannerError.Should().BeFalse();
    }

    [TestMethod]
    public void BannerProperties_WhenNoSolutionOpen_NoBanner()
    {
        SimulatePluginStatusesChanged(new PluginStatusDisplay("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, string.Empty));
        SimulateConfigurationScopeChanged(null);

        testSubject.IsBannerPromotion.Should().BeFalse();
        testSubject.IsBannerError.Should().BeFalse();
    }

    [TestMethod]
    public void BannerProperties_WhenPluginFailed_ShowsErrorNotPromotion()
    {
        testSubject.IsBannerPromotion.Should().BeFalse();
        testSubject.IsBannerError.Should().BeTrue();
    }

    [TestMethod]
    public void OnPluginStatusesChanged_UpdatesCollections()
    {
        var newPlugins = new[]
        {
            new PluginStatusDisplay("Ruby", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, string.Empty),
            new PluginStatusDisplay("COBOL", PluginStateDto.PREMIUM, null, string.Empty)
        };

        SimulatePluginStatusesChanged(newPlugins);

        testSubject.AllPlugins.Should().BeEquivalentTo(newPlugins);
        testSubject.DisplayedPlugins.Single().PluginName.Should().Be("Ruby");
        testSubject.PremiumPluginsTooltip.Should().Be(string.Format(Strings.PluginStatuses_PremiumPluginsTooltip, "COBOL"));
        testSubject.FailedPluginsTooltip.Should().BeEmpty();
    }

    [TestMethod]
    public void OnPluginStatusesChanged_DelegatesThreadingAndRaisesPropertyChanged()
    {
        threadHandling.ClearReceivedCalls();
        var raisedProperties = new List<string>();
        testSubject.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

        pluginStatusesStore.PluginStatusesChanged += Raise.EventWith(EventArgs.Empty);

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        threadHandling.Received(1).RunOnUIThreadAsync(Arg.Any<Action>());
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.PremiumPluginsTooltip));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.FailedPluginsTooltip));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsBannerPromotion));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsBannerError));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.PromotionBannerButtonText));
    }

    [TestMethod]
    public void OnConnectionChanged_DelegatesThreadingAndRaisesPropertyChanged()
    {
        threadHandling.ClearReceivedCalls();
        var raisedProperties = new List<string>();
        testSubject.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

        serverConnectionsRepository.ConnectionChanged += Raise.EventWith(EventArgs.Empty);

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        threadHandling.Received(1).RunOnUIThreadAsync(Arg.Any<Action>());
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsBannerPromotion));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsBannerError));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.PromotionBannerButtonText));
    }

    [TestMethod]
    public void OnConfigurationScopeChanged_DelegatesThreadingAndRaisesPropertyChanged()
    {
        threadHandling.ClearReceivedCalls();
        var raisedProperties = new List<string>();
        testSubject.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(new ConfigurationScopeChangedEventArgs(true));

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        threadHandling.Received(1).RunOnUIThreadAsync(Arg.Any<Action>());
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsBannerPromotion));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsBannerError));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.PromotionBannerButtonText));
    }

    [TestMethod]
    public void SetUpConnection_OpensManageBindingDialog()
    {
        testSubject.SetUpConnection();

        telemetryManager.Received(1).SupportedLanguagesPanelCtaClicked();
        connectedModeUiManager.Received(1).ShowManageBindingDialogAsync();
    }

    [TestMethod]
    public void RestartBackend_ForcesRestartSloop()
    {
        testSubject.RestartBackend();

        slCoreHandler.Received(1).ForceRestartSloop();
        pluginStatusesStore.Received(1).Clear();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromAllEvents()
    {
        testSubject.Dispose();

        pluginStatusesStore.Received(1).PluginStatusesChanged -= Arg.Any<EventHandler>();
        serverConnectionsRepository.Received(1).ConnectionChanged -= Arg.Any<EventHandler>();
        activeConfigScopeTracker.ReceivedWithAnyArgs(1).CurrentConfigurationScopeChanged -= Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
    }

    private void SimulatePluginStatusesChanged(params PluginStatusDisplay[] plugins)
    {
        pluginStatusesStore.GetAll().Returns(plugins);
        pluginStatusesStore.PluginStatusesChanged += Raise.EventWith(EventArgs.Empty);
    }

    private void SimulateConnectionChanged(ServerConnection connection)
    {
        serverConnectionsRepository.TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>()).Returns(callInfo =>
        {
            callInfo[0] = new List<ServerConnection> { connection };
            return true;
        });
        serverConnectionsRepository.ConnectionChanged += Raise.EventWith(EventArgs.Empty);
    }

    private void SimulateConfigurationScopeChanged(ConfigurationScope configScope)
    {
        activeConfigScopeTracker.Current.Returns(configScope);
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(new ConfigurationScopeChangedEventArgs(true));
    }
}

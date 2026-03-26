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
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests.SupportedLanguages;

[TestClass]
public class SupportedLanguagesDialogViewModelTests
{
    private static readonly PluginStatusDto[] DefaultPluginStatuses =
    [
        new(Language.CS, "C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null),
        new(Language.PYTHON, "Python", PluginStateDto.SYNCED, ArtifactSourceDto.SONARQUBE_SERVER, "2.0", "1.5", "10.8.1"),
        new(Language.JS, "JS", PluginStateDto.DOWNLOADING, ArtifactSourceDto.EMBEDDED, null, null, null),
        new(Language.GO, "Go", PluginStateDto.FAILED, null, null, null, null),
        new(Language.JAVA, "Java", PluginStateDto.PREMIUM, null, null, null, null),
        new(Language.COBOL, "COBOL", PluginStateDto.PREMIUM, null, null, null, null),
        new(Language.KOTLIN, "Kotlin", PluginStateDto.UNSUPPORTED, null, null, null, null)
    ];

    private IPluginStatusesStore pluginStatusesStore;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ISLCoreHandler slCoreHandler;
    private IServerConnectionsRepository serverConnectionsRepository;
    private IConnectedModeUIManager connectedModeUiManager;
    private IThreadHandling threadHandling;
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

        pluginStatusesStore.GetAll().Returns(DefaultPluginStatuses);
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope("MySolution"));
        serverConnectionsRepository.TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>()).Returns(false);
        connectedModeUiManager.ShowManageBindingDialogAsync().Returns(Task.FromResult(true));

        testSubject = new SupportedLanguagesDialogViewModel(pluginStatusesStore, activeConfigScopeTracker, slCoreHandler, serverConnectionsRepository, connectedModeUiManager, threadHandling);
    }

    [TestMethod]
    public void Ctor_InitializesCollectionsFromStore()
    {
        testSubject.AllPlugins.Should().BeEquivalentTo(DefaultPluginStatuses);
        testSubject.DisplayedPlugins.Select(p => p.pluginName).Should().BeEquivalentTo("C#", "Python", "JS", "Go");
        testSubject.PremiumPluginsTooltip.Should().Be(string.Format(Strings.PluginStatuses_PremiumPluginsTooltip, "Java, COBOL"));
        testSubject.FailedPluginsTooltip.Should().Be("Go");
    }

    [TestMethod]
    public void Ctor_StoreReturnsEmpty_CollectionsAreEmpty()
    {
        pluginStatusesStore.GetAll().Returns(Array.Empty<PluginStatusDto>());

        var emptyTestSubject = new SupportedLanguagesDialogViewModel(pluginStatusesStore, activeConfigScopeTracker, slCoreHandler, serverConnectionsRepository, connectedModeUiManager, threadHandling);

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
        SimulatePluginStatusesChanged(new PluginStatusDto(Language.CS, "C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null));

        testSubject.PremiumPluginsTooltip.Should().BeEmpty();
        testSubject.FailedPluginsTooltip.Should().BeEmpty();
    }

    [TestMethod]
    public void BannerProperties_WhenNoConnection_ShowsPromotionWithSetUpButton()
    {
        SimulatePluginStatusesChanged(new PluginStatusDto(Language.CS, "C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null));

        testSubject.IsBannerPromotion.Should().BeTrue();
        testSubject.IsBannerError.Should().BeFalse();
        testSubject.PromotionBannerButtonText.Should().Be(Strings.PluginStatuses_BannerSetUpConnectionButton);
    }

    [TestMethod]
    public void BannerProperties_WhenNotBoundAndHasConnection_ShowsPromotionWithBindButton()
    {
        SimulatePluginStatusesChanged(new PluginStatusDto(Language.CS, "C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null));
        SimulateConnectionChanged(new ServerConnection.SonarQube(new Uri("http://localhost")));

        testSubject.IsBannerPromotion.Should().BeTrue();
        testSubject.IsBannerError.Should().BeFalse();
        testSubject.PromotionBannerButtonText.Should().Be(Strings.PluginStatuses_BannerBindProjectButton);
    }

    [TestMethod]
    public void BannerProperties_WhenBound_NoBanner()
    {
        SimulatePluginStatusesChanged(new PluginStatusDto(Language.CS, "C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null));
        SimulateConfigurationScopeChanged(new ConfigurationScope("MySolution", SonarProjectId: "project-key"));

        testSubject.IsBannerPromotion.Should().BeFalse();
        testSubject.IsBannerError.Should().BeFalse();
    }

    [TestMethod]
    public void BannerProperties_WhenNoSolutionOpen_NoBanner()
    {
        SimulatePluginStatusesChanged(new PluginStatusDto(Language.CS, "C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null));
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
            new PluginStatusDto(Language.RUBY, "Ruby", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null),
            new PluginStatusDto(Language.COBOL, "COBOL", PluginStateDto.PREMIUM, null, null, null, null)
        };

        SimulatePluginStatusesChanged(newPlugins);

        testSubject.AllPlugins.Should().BeEquivalentTo(newPlugins);
        testSubject.DisplayedPlugins.Single().pluginName.Should().Be("Ruby");
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

    private void SimulatePluginStatusesChanged(params PluginStatusDto[] plugins)
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

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
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.SupportedLanguages;

[TestClass]
public class SupportedLanguagesDialogViewModelTests
{
    private static readonly PluginStatusDto[] DefaultPluginStatuses =
    [
        new("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null),
        new("Python", PluginStateDto.SYNCED, ArtifactSourceDto.SONARQUBE_SERVER, "2.0", "1.5"),
        new("JS", PluginStateDto.DOWNLOADING, ArtifactSourceDto.EMBEDDED, null, null),
        new("Go", PluginStateDto.FAILED, null, null, null),
        new("Java", PluginStateDto.PREMIUM, null, null, null),
        new("COBOL", PluginStateDto.PREMIUM, null, null, null),
        new("Kotlin", PluginStateDto.UNSUPPORTED, null, null, null)
    ];

    private IPluginStatusesStore pluginStatusesStore;
    private IThreadHandling threadHandling;
    private IServerConnectionsRepository serverConnectionsRepository;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IConnectedModeUIManager connectedModeUIManager;
    private ISLCoreHandler slCoreHandler;

    [TestInitialize]
    public void TestInitialize()
    {
        pluginStatusesStore = Substitute.For<IPluginStatusesStore>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        serverConnectionsRepository = Substitute.For<IServerConnectionsRepository>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        connectedModeUIManager = Substitute.For<IConnectedModeUIManager>();
        slCoreHandler = Substitute.For<ISLCoreHandler>();
        pluginStatusesStore.GetAll().Returns(DefaultPluginStatuses);
        activeSolutionBoundTracker.CurrentConfiguration.Returns(BindingConfiguration.Standalone);
        serverConnectionsRepository.TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>()).Returns(false);
        connectedModeUIManager.ShowManageBindingDialogAsync().Returns(Task.FromResult(true));
    }

    private SupportedLanguagesDialogViewModel CreateTestSubject() =>
        new(pluginStatusesStore, threadHandling, serverConnectionsRepository, activeSolutionBoundTracker, connectedModeUIManager, slCoreHandler);

    [TestMethod]
    public void Ctor_InitializesAllPluginsFromStore()
    {
        var testSubject = CreateTestSubject();

        testSubject.AllPlugins.Should().BeEquivalentTo(DefaultPluginStatuses);
    }

    [TestMethod]
    public void Ctor_DisplayedPlugins_FiltersToDisplayedStatesOnly()
    {
        var testSubject = CreateTestSubject();

        testSubject.DisplayedPlugins.Select(p => p.pluginName).Should().BeEquivalentTo("C#", "Python", "JS", "Go");
    }

    [TestMethod]
    public void Ctor_StoreReturnsEmpty_CollectionsAreEmpty()
    {
        pluginStatusesStore.GetAll().Returns(Array.Empty<PluginStatusDto>());
        var testSubject = CreateTestSubject();

        testSubject.AllPlugins.Should().BeEmpty();
        testSubject.DisplayedPlugins.Should().BeEmpty();
        testSubject.PremiumLanguagesTooltip.Should().BeEmpty();
        testSubject.FailedPluginsText.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_SubscribesToEvents()
    {
        var testSubject = CreateTestSubject();

        pluginStatusesStore.Received(1).PluginStatusesChanged += Arg.Any<EventHandler>();
        serverConnectionsRepository.Received(1).ConnectionChanged += Arg.Any<EventHandler>();
        activeSolutionBoundTracker.ReceivedWithAnyArgs(1).SolutionBindingChanged += Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
    }

    [TestMethod]
    public void PremiumLanguagesTooltip_ReturnsPremiumPluginNames()
    {
        var testSubject = CreateTestSubject();

        testSubject.PremiumLanguagesTooltip.Should().Be(string.Format(Strings.PluginStatuses_PremiumLanguagesTooltip, "Java, COBOL"));
    }

    [TestMethod]
    public void PremiumLanguagesTooltip_NoPremiumPlugins_ReturnsEmpty()
    {
        pluginStatusesStore.GetAll().Returns(new[]
        {
            new PluginStatusDto("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null)
        });
        var testSubject = CreateTestSubject();

        testSubject.PremiumLanguagesTooltip.Should().BeEmpty();
    }

    [TestMethod]
    public void PremiumLanguagesTooltip_DuplicatePremiumNames_ReturnsDistinct()
    {
        pluginStatusesStore.GetAll().Returns(new[]
        {
            new PluginStatusDto("Java", PluginStateDto.PREMIUM, null, null, null),
            new PluginStatusDto("Java", PluginStateDto.PREMIUM, null, null, null)
        });
        var testSubject = CreateTestSubject();

        testSubject.PremiumLanguagesTooltip.Should().Be(string.Format(Strings.PluginStatuses_PremiumLanguagesTooltip, "Java"));
    }

    [TestMethod]
    public void FailedPluginsText_ReturnsFailedPluginNames()
    {
        var testSubject = CreateTestSubject();

        testSubject.FailedPluginsText.Should().Be("Go");
    }

    [TestMethod]
    public void FailedPluginsText_NoFailedPlugins_ReturnsEmpty()
    {
        pluginStatusesStore.GetAll().Returns(new[]
        {
            new PluginStatusDto("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null)
        });
        var testSubject = CreateTestSubject();

        testSubject.FailedPluginsText.Should().BeEmpty();
    }

    [TestMethod]
    public void IsBannerVisible_True_WhenNoConnection()
    {
        pluginStatusesStore.GetAll().Returns(new[] { new PluginStatusDto("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null) });
        var testSubject = CreateTestSubject();

        testSubject.IsBannerVisible.Should().BeTrue();
    }

    [TestMethod]
    public void IsBannerVisible_False_WhenBound()
    {
        pluginStatusesStore.GetAll().Returns(new[] { new PluginStatusDto("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null) });
        activeSolutionBoundTracker.CurrentConfiguration.Returns(new BindingConfiguration(new BoundServerProject("solution", "project", new ServerConnection.SonarQube(new Uri("http://localhost"))), SonarLintMode.Connected, "dir"));
        var testSubject = CreateTestSubject();

        testSubject.IsBannerVisible.Should().BeFalse();
    }

    [TestMethod]
    public void IsBannerVisible_True_WhenPluginFailed()
    {
        var testSubject = CreateTestSubject();

        testSubject.IsBannerVisible.Should().BeTrue();
    }

    [TestMethod]
    public void IsErrorBanner_True_WhenPluginFailed()
    {
        var testSubject = CreateTestSubject();

        testSubject.IsErrorBanner.Should().BeTrue();
    }

    [TestMethod]
    public void IsErrorBanner_False_WhenNoConnection()
    {
        pluginStatusesStore.GetAll().Returns(new[] { new PluginStatusDto("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null) });
        var testSubject = CreateTestSubject();

        testSubject.IsErrorBanner.Should().BeFalse();
    }

    [TestMethod]
    public void IsErrorBanner_TakesPriorityOverConnectionState()
    {
        activeSolutionBoundTracker.CurrentConfiguration.Returns(new BindingConfiguration(new BoundServerProject("solution", "project", new ServerConnection.SonarQube(new Uri("http://localhost"))), SonarLintMode.Connected, "dir"));
        var testSubject = CreateTestSubject();

        testSubject.IsErrorBanner.Should().BeTrue();
    }

    [TestMethod]
    public void SetUpConnectionText_SetUpConnection_WhenNoConnection()
    {
        pluginStatusesStore.GetAll().Returns(new[] { new PluginStatusDto("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null) });
        var testSubject = CreateTestSubject();

        testSubject.SetUpConnectionText.Should().Be(Strings.PluginStatuses_BannerSetUpConnectionButton);
    }

    [TestMethod]
    public void SetUpConnectionText_BindProject_WhenNotBound()
    {
        pluginStatusesStore.GetAll().Returns(new[] { new PluginStatusDto("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null) });
        serverConnectionsRepository.TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>()).Returns(callInfo =>
        {
            callInfo[0] = new List<ServerConnection> { new ServerConnection.SonarQube(new Uri("http://localhost")) };
            return true;
        });
        var testSubject = CreateTestSubject();

        testSubject.SetUpConnectionText.Should().Be(Strings.PluginStatuses_BannerBindProjectButton);
    }

    [TestMethod]
    public void OnPluginStatusesChanged_UpdatesCollections()
    {
        var testSubject = CreateTestSubject();
        var newPlugins = new[]
        {
            new PluginStatusDto("Ruby", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null),
            new PluginStatusDto("COBOL", PluginStateDto.PREMIUM, null, null, null)
        };
        pluginStatusesStore.GetAll().Returns(newPlugins);

        pluginStatusesStore.PluginStatusesChanged += Raise.EventWith(EventArgs.Empty);

        testSubject.AllPlugins.Should().BeEquivalentTo(newPlugins);
        testSubject.DisplayedPlugins.Single().pluginName.Should().Be("Ruby");
    }

    [TestMethod]
    public void OnPluginStatusesChanged_DelegatesUpdateToUIThread()
    {
        var testSubject = CreateTestSubject();

        pluginStatusesStore.PluginStatusesChanged += Raise.EventWith(EventArgs.Empty);

        threadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
    }

    [TestMethod]
    public void OnPluginStatusesChanged_RaisesPropertyChangedForAllRelevantProperties()
    {
        var testSubject = CreateTestSubject();
        var raisedProperties = new List<string>();
        testSubject.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

        pluginStatusesStore.PluginStatusesChanged += Raise.EventWith(EventArgs.Empty);

        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.PremiumLanguagesTooltip));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.FailedPluginsText));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsBannerVisible));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsErrorBanner));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.SetUpConnectionText));
    }

    [TestMethod]
    public void OnConnectionChanged_DelegatesUpdateToUIThread()
    {
        var testSubject = CreateTestSubject();

        serverConnectionsRepository.ConnectionChanged += Raise.EventWith(EventArgs.Empty);

        threadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
    }

    [TestMethod]
    public void OnConnectionChanged_RaisesPropertyChangedForBannerProperties()
    {
        var testSubject = CreateTestSubject();
        var raisedProperties = new List<string>();
        testSubject.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

        serverConnectionsRepository.ConnectionChanged += Raise.EventWith(EventArgs.Empty);

        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsBannerVisible));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsErrorBanner));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.SetUpConnectionText));
    }

    [TestMethod]
    public void OnSolutionBindingChanged_DelegatesUpdateToUIThread()
    {
        var testSubject = CreateTestSubject();

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));

        threadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
    }

    [TestMethod]
    public void OnSolutionBindingChanged_RaisesPropertyChangedForBannerProperties()
    {
        var testSubject = CreateTestSubject();
        var raisedProperties = new List<string>();
        testSubject.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));

        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsBannerVisible));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.IsErrorBanner));
        raisedProperties.Should().Contain(nameof(SupportedLanguagesDialogViewModel.SetUpConnectionText));
    }

    [TestMethod]
    public void SetUpConnection_OpensManageBindingDialog()
    {
        var testSubject = CreateTestSubject();

        testSubject.SetUpConnection();

        connectedModeUIManager.Received(1).ShowManageBindingDialogAsync();
    }

    [TestMethod]
    public void RestartBackend_ForcesRestartSloop()
    {
        var testSubject = CreateTestSubject();

        testSubject.RestartBackend();

        slCoreHandler.Received(1).ForceRestartSloop();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromAllEvents()
    {
        var testSubject = CreateTestSubject();

        testSubject.Dispose();

        pluginStatusesStore.Received(1).PluginStatusesChanged -= Arg.Any<EventHandler>();
        serverConnectionsRepository.Received(1).ConnectionChanged -= Arg.Any<EventHandler>();
        activeSolutionBoundTracker.ReceivedWithAnyArgs(1).SolutionBindingChanged -= Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
    }
}

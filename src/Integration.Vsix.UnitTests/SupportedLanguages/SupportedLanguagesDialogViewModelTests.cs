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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;
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
    private SupportedLanguagesDialogViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        pluginStatusesStore = Substitute.For<IPluginStatusesStore>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        pluginStatusesStore.GetAll().Returns(DefaultPluginStatuses);
        testSubject = new SupportedLanguagesDialogViewModel(pluginStatusesStore, threadHandling);
    }

    [TestMethod]
    public void Ctor_InitializesAllPluginsFromStore()
    {
        testSubject.AllPlugins.Should().BeEquivalentTo(DefaultPluginStatuses);
    }

    [TestMethod]
    public void Ctor_DisplayedPlugins_FiltersToDisplayedStatesOnly()
    {
        testSubject.DisplayedPlugins.Select(p => p.pluginName).Should().BeEquivalentTo("C#", "Python", "JS", "Go");
    }

    [TestMethod]
    public void Ctor_StoreReturnsEmpty_CollectionsAreEmpty()
    {
        pluginStatusesStore.GetAll().Returns(Array.Empty<PluginStatusDto>());
        var subject = new SupportedLanguagesDialogViewModel(pluginStatusesStore, threadHandling);

        subject.AllPlugins.Should().BeEmpty();
        subject.DisplayedPlugins.Should().BeEmpty();
        subject.PremiumLanguagesTooltip.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_SubscribesToPluginStatusesChangedEvent()
    {
        pluginStatusesStore.Received(1).PluginStatusesChanged += Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void PremiumLanguagesTooltip_ReturnsPremiumPluginNames()
    {
        testSubject.PremiumLanguagesTooltip.Should().Be("Java, COBOL");
    }

    [TestMethod]
    public void PremiumLanguagesTooltip_NoPremiumPlugins_ReturnsEmpty()
    {
        pluginStatusesStore.GetAll().Returns(new[]
        {
            new PluginStatusDto("C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null)
        });
        var subject = new SupportedLanguagesDialogViewModel(pluginStatusesStore, threadHandling);

        subject.PremiumLanguagesTooltip.Should().BeEmpty();
    }

    [TestMethod]
    public void PremiumLanguagesTooltip_DuplicatePremiumNames_ReturnsDistinct()
    {
        pluginStatusesStore.GetAll().Returns(new[]
        {
            new PluginStatusDto("Java", PluginStateDto.PREMIUM, null, null, null),
            new PluginStatusDto("Java", PluginStateDto.PREMIUM, null, null, null)
        });
        var subject = new SupportedLanguagesDialogViewModel(pluginStatusesStore, threadHandling);

        subject.PremiumLanguagesTooltip.Should().Be("Java");
    }

    [TestMethod]
    public void OnPluginStatusesChanged_UpdatesCollections()
    {
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
        pluginStatusesStore.GetAll().Returns(Array.Empty<PluginStatusDto>());

        pluginStatusesStore.PluginStatusesChanged += Raise.EventWith(EventArgs.Empty);

        threadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
    }

    [TestMethod]
    public void OnPluginStatusesChanged_RaisesPropertyChangedForPremiumLanguagesTooltip()
    {
        var propertyChangedRaised = false;
        testSubject.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SupportedLanguagesDialogViewModel.PremiumLanguagesTooltip))
            {
                propertyChangedRaised = true;
            }
        };

        pluginStatusesStore.GetAll().Returns(Array.Empty<PluginStatusDto>());
        pluginStatusesStore.PluginStatusesChanged += Raise.EventWith(EventArgs.Empty);

        propertyChangedRaised.Should().BeTrue();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromPluginStatusesChanged()
    {
        testSubject.Dispose();

        pluginStatusesStore.GetAll().Returns(Array.Empty<PluginStatusDto>());
        pluginStatusesStore.PluginStatusesChanged += Raise.EventWith(EventArgs.Empty);

        // AllPlugins should remain unchanged since the handler was unsubscribed
        testSubject.AllPlugins.Should().BeEquivalentTo(DefaultPluginStatuses);
    }
}

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

using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Plugin;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.SupportedLanguages;

[TestClass]
public class PluginStatusesStoreTests
{
    private const string ConfigScopeId = "configScope1";

    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private IPluginSLCoreService pluginSLCoreService;
    private NoOpThreadHandler threadHandling;
    private TestLogger logger;
    private PluginStatusesStore testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        pluginSLCoreService = Substitute.For<IPluginSLCoreService>();
        threadHandling = new NoOpThreadHandler();
        logger = new TestLogger();

        testSubject = new(activeConfigScopeTracker, slCoreServiceProvider, threadHandling, logger);
        SetCurrentConfigScope(ConfigScopeId);
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<IPluginSLCoreService>()).Returns(info =>
        {
            info[0] = pluginSLCoreService;
            return true;
        });
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<PluginStatusesStore, IPluginStatusesStore>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<PluginStatusesStore>();
    }

    [TestMethod]
    public void GetAll_WhenEmpty_ReturnsEmptyCollection()
    {
        var result = testSubject.GetAll();

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Update_WhenConfigScopeMatches_StoresPluginStatuses()
    {
        var pluginStatuses = CreatePluginStatuses();

        testSubject.Update(ConfigScopeId, pluginStatuses);

        testSubject.GetAll().Should().BeEquivalentTo(pluginStatuses);
    }

    [TestMethod]
    public void Update_WhenConfigScopeMatches_ReplacesPluginStatuses()
    {
        testSubject.Update(ConfigScopeId, CreatePluginStatuses());

        var newStatuses = new List<PluginStatusDto>
        {
            new(Language.PYTHON, "Python", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null)
        };
        testSubject.Update(ConfigScopeId, newStatuses);

        var result = testSubject.GetAll();
        result.Should().BeEquivalentTo(newStatuses);
    }

    [TestMethod]
    public void Update_WhenConfigScopeMatches_RaisesEvent()
    {
        var eventHandler = Substitute.For<EventHandler>();
        testSubject.PluginStatusesChanged += eventHandler;

        testSubject.Update(ConfigScopeId, CreatePluginStatuses());

        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void Update_WhenConfigScopeMismatch_DoesNotStoreAndDoesNotRaiseEvent()
    {
        SetCurrentConfigScope("differentScope");
        var eventHandler = Substitute.For<EventHandler>();
        testSubject.PluginStatusesChanged += eventHandler;

        testSubject.Update(ConfigScopeId, CreatePluginStatuses());

        testSubject.GetAll().Should().BeEmpty();
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
    }

    [TestMethod]
    public void Update_WhenCurrentScopeIsNull_DoesNotStoreAndDoesNotRaiseEvent()
    {
        SetCurrentConfigScope(null);
        var eventHandler = Substitute.For<EventHandler>();
        testSubject.PluginStatusesChanged += eventHandler;

        testSubject.Update(ConfigScopeId, CreatePluginStatuses());

        testSubject.GetAll().Should().BeEmpty();
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
    }

    [TestMethod]
    public void Clear_EmptiesPluginStatuses()
    {
        testSubject.Update(ConfigScopeId, CreatePluginStatuses());

        testSubject.Clear();

        testSubject.GetAll().Should().BeEmpty();
    }

    [TestMethod]
    public void Clear_RaisesPluginStatusesChangedEvent()
    {
        var eventHandler = Substitute.For<EventHandler>();
        testSubject.PluginStatusesChanged += eventHandler;

        testSubject.Clear();

        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void ConfigurationScopeChanged_FetchesPluginStatusesFromSLCore()
    {
        var pluginStatuses = CreatePluginStatuses();
        pluginSLCoreService.GetPluginStatusesAsync(Arg.Is<GetPluginStatusesParams>(p => p.configurationScopeId == ConfigScopeId))
            .Returns(new GetPluginStatusesResponse(pluginStatuses));

        RaiseConfigScopeChanged();

        testSubject.GetAll().Should().BeEquivalentTo(pluginStatuses);
    }

    [TestMethod]
    public void ConfigurationScopeChanged_RaisesPluginStatusesChangedEvent()
    {
        pluginSLCoreService.GetPluginStatusesAsync(Arg.Any<GetPluginStatusesParams>())
            .Returns(new GetPluginStatusesResponse(CreatePluginStatuses()));
        var eventHandler = Substitute.For<EventHandler>();
        testSubject.PluginStatusesChanged += eventHandler;

        RaiseConfigScopeChanged();

        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void ConfigurationScopeChanged_WhenCurrentScopeIsNull_FetchesWithNullScopeId()
    {
        SetCurrentConfigScope(null);
        var pluginStatuses = CreatePluginStatuses();
        pluginSLCoreService.GetPluginStatusesAsync(Arg.Is<GetPluginStatusesParams>(p => p.configurationScopeId == null))
            .Returns(new GetPluginStatusesResponse(pluginStatuses));

        RaiseConfigScopeChanged();

        testSubject.GetAll().Should().BeEquivalentTo(pluginStatuses);
    }

    [TestMethod]
    public void ConfigurationScopeChanged_WhenServiceNotAvailable_DoesNotThrow()
    {
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<IPluginSLCoreService>()).Returns(info =>
        {
            info[0] = null;
            return false;
        });

        var act = () => RaiseConfigScopeChanged();

        act.Should().NotThrow();
        testSubject.GetAll().Should().BeEmpty();
    }

    [TestMethod]
    public void ConfigurationScopeChanged_WhenFetchThrows_DoesNotThrow()
    {
        pluginSLCoreService.GetPluginStatusesAsync(Arg.Any<GetPluginStatusesParams>())
            .ThrowsAsync(new Exception("fetch failed"));

        var act = () => RaiseConfigScopeChanged();

        act.Should().NotThrow();
        testSubject.GetAll().Should().BeEmpty();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromConfigScopeChanged()
    {
        pluginSLCoreService.GetPluginStatusesAsync(Arg.Any<GetPluginStatusesParams>())
            .Returns(new GetPluginStatusesResponse(CreatePluginStatuses()));

        testSubject.Dispose();
        RaiseConfigScopeChanged();

        pluginSLCoreService.DidNotReceive().GetPluginStatusesAsync(Arg.Any<GetPluginStatusesParams>());
    }

    private void SetCurrentConfigScope(string scopeId)
    {
        if (scopeId == null)
        {
            activeConfigScopeTracker.Current.ReturnsNull();
        }
        else
        {
            activeConfigScopeTracker.Current.Returns(new ConfigurationScope(scopeId));
        }
    }

    private void RaiseConfigScopeChanged() =>
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(new ConfigurationScopeChangedEventArgs(true));

    private static List<PluginStatusDto> CreatePluginStatuses() =>
    [
        new PluginStatusDto(Language.JAVA, "Java", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null),
        new PluginStatusDto(Language.CS, "C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "2.0", null, null)
    ];
}

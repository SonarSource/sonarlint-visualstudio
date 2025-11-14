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

using System.Collections.Immutable;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Analysis;

[TestClass]
public class SlCoreUserAnalysisPropertiesSynchronizerTests
{
    private const string DefaultConfigScope = "scope1";
    private readonly ImmutableDictionary<string, string> defaultAnalysisProperties = ImmutableDictionary.Create<string, string>().Add("prop1", "value1");
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IUserSettingsProvider userSettingsProvider;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private ISLCoreServiceProvider serviceProvider;
    private IUserAnalysisPropertiesService userAnalysisPropertiesService;
    private IThreadHandling threadHandling;
    private TestLogger testLogger;

    private IRequireInitialization[] initializationDependencies;

    [TestInitialize]
    public void TestInitialize()
    {
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        userSettingsProvider = Substitute.For<IUserSettingsProvider>();
        initializationProcessorFactory = Substitute.For<IInitializationProcessorFactory>();
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        userAnalysisPropertiesService = Substitute.For<IUserAnalysisPropertiesService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        testLogger = Substitute.ForPartsOf<TestLogger>();

        initializationDependencies = [userSettingsProvider];
        SetUpServiceProvider();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SlCoreUserAnalysisPropertiesSynchronizer, ISlCoreUserAnalysisPropertiesSynchronizer>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IUserSettingsProvider>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<SlCoreUserAnalysisPropertiesSynchronizer>();

    [TestMethod]
    public void Ctor_NoActiveConfigScope_RunsInitialization()
    {
        SetCurrentConfiguration(null, null);
        var testSubject = CreateAndInitializeTestSubjectWithoutClearingReceivedCalls();

        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<SlCoreUserAnalysisPropertiesSynchronizer>(
                Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.SequenceEqual(initializationDependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            testSubject.InitializationProcessor.InitializeAsync();
            activeConfigScopeTracker.CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
            userSettingsProvider.SettingsChanged += Arg.Any<EventHandler>();
            testSubject.InitializationProcessor.InitializeAsync();
        });
        userAnalysisPropertiesService.DidNotReceiveWithAnyArgs().DidSetUserAnalysisProperties(default);
    }

    [TestMethod]
    public void Ctor_HasActiveConfigScope_RunsInitialization()
    {
        SetCurrentConfiguration(DefaultConfigScope, defaultAnalysisProperties);
        var testSubject = CreateAndInitializeTestSubjectWithoutClearingReceivedCalls();

        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<SlCoreUserAnalysisPropertiesSynchronizer>(
                Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.SequenceEqual(initializationDependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            testSubject.InitializationProcessor.InitializeAsync();
            activeConfigScopeTracker.CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
            userSettingsProvider.SettingsChanged += Arg.Any<EventHandler>();
            serviceProvider.TryGetTransientService(out Arg.Any<IUserAnalysisPropertiesService>());
            userAnalysisPropertiesService.DidSetUserAnalysisProperties(Arg.Is<DidChangeAnalysisPropertiesParams>(x => x.configurationScopeId == DefaultConfigScope && x.properties == defaultAnalysisProperties));
            testSubject.InitializationProcessor.InitializeAsync();
        });
    }

    [TestMethod]
    public void Ctor_WhenUninitialized_DoesNotReactToEvents()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);

        RaiseConfigScopeChanged(true);
        RaiseSettingsChanged();
        _ = activeConfigScopeTracker.DidNotReceiveWithAnyArgs().Current;

        barrier.SetResult(0);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        _ = activeConfigScopeTracker.Received(1).Current;

        RaiseConfigScopeChanged(true);
        RaiseSettingsChanged();
        _ = activeConfigScopeTracker.Received(3).Current;
    }

    [TestMethod]
    public void DisposedBeforeInitialized_InitializationDoesNothing()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        testSubject.Dispose();

        barrier.SetResult(0);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        activeConfigScopeTracker.DidNotReceiveWithAnyArgs().CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
        activeConfigScopeTracker.DidNotReceiveWithAnyArgs().CurrentConfigurationScopeChanged -= Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
        userSettingsProvider.DidNotReceiveWithAnyArgs().SettingsChanged += Arg.Any<EventHandler>();
        userSettingsProvider.DidNotReceiveWithAnyArgs().SettingsChanged -= Arg.Any<EventHandler>();
        _ = activeConfigScopeTracker.DidNotReceiveWithAnyArgs().Current;
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_NoConfigurationScope_DoesNotUpdateSLCore()
    {
        CreateAndInitializeTestSubject();
        SetCurrentConfiguration(null, null);

        RaiseConfigScopeChanged(true);

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        userAnalysisPropertiesService.DidNotReceiveWithAnyArgs().DidSetUserAnalysisProperties(default);
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_MiscellaneousUpdate_DoesNotUpdateSLCore()
    {
        CreateAndInitializeTestSubject();
        activeConfigScopeTracker.ClearReceivedCalls();
        SetCurrentConfiguration(DefaultConfigScope, defaultAnalysisProperties);

        RaiseConfigScopeChanged(false);

        threadHandling.DidNotReceive().RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        _ = activeConfigScopeTracker.DidNotReceiveWithAnyArgs().Current;
        userAnalysisPropertiesService.DidNotReceiveWithAnyArgs().DidSetUserAnalysisProperties(default);
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_DefinitionChanged_UpdateSLCore()
    {
        CreateAndInitializeTestSubject();

        SetCurrentConfiguration(DefaultConfigScope, defaultAnalysisProperties);
        RaiseConfigScopeChanged(true);

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        userAnalysisPropertiesService.Received(1).DidSetUserAnalysisProperties(Arg.Is<DidChangeAnalysisPropertiesParams>(x => x.configurationScopeId == DefaultConfigScope && x.properties == defaultAnalysisProperties));
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_SameConfigurationScope_UpdateSLCore()
    {
        SetCurrentConfiguration(DefaultConfigScope, defaultAnalysisProperties);
        CreateAndInitializeTestSubject();

        RaiseConfigScopeChanged(true);

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        userAnalysisPropertiesService.Received(2).DidSetUserAnalysisProperties(Arg.Is<DidChangeAnalysisPropertiesParams>(x => x.configurationScopeId == DefaultConfigScope && x.properties == defaultAnalysisProperties));
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_DifferentConfigurationScope_UpdateSLCore()
    {
        SetCurrentConfiguration(DefaultConfigScope, defaultAnalysisProperties);
        CreateAndInitializeTestSubject();

        const string scope2 = "scope2";
        SetCurrentConfiguration(scope2, defaultAnalysisProperties);
        RaiseConfigScopeChanged(true);

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        userAnalysisPropertiesService.Received(1).DidSetUserAnalysisProperties(Arg.Is<DidChangeAnalysisPropertiesParams>(x => x.configurationScopeId == scope2 && x.properties == defaultAnalysisProperties));
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_ServiceNotAvailable_DoesNothing()
    {
        CreateAndInitializeTestSubject();
        SetCurrentConfiguration(DefaultConfigScope, defaultAnalysisProperties);
        serviceProvider.TryGetTransientService(out Arg.Any<IUserAnalysisPropertiesService>()).Returns(false);

        RaiseConfigScopeChanged(true);

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        userAnalysisPropertiesService.DidNotReceiveWithAnyArgs().DidSetUserAnalysisProperties(default);
    }

    [TestMethod]
    public void SettingsChanged_SynchronizesAnalysisProperties()
    {
        SetCurrentConfiguration(DefaultConfigScope, defaultAnalysisProperties);
        CreateAndInitializeTestSubject();

        // Update settings and raise event
        var newProperties = ImmutableDictionary.Create<string, string>().Add("prop2", "value2");
        SetCurrentConfiguration(DefaultConfigScope, newProperties);
        RaiseSettingsChanged();

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        userAnalysisPropertiesService.Received(1).DidSetUserAnalysisProperties(Arg.Is<DidChangeAnalysisPropertiesParams>(x =>  x.configurationScopeId == DefaultConfigScope && x.properties == newProperties));
    }

    [TestMethod]
    public void SettingsChange_ServiceNotAvailable_DoesNothing()
    {
        CreateAndInitializeTestSubject();
        SetCurrentConfiguration(DefaultConfigScope, defaultAnalysisProperties);
        serviceProvider.TryGetTransientService(out Arg.Any<IUserAnalysisPropertiesService>()).Returns(false);

        RaiseSettingsChanged();

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        userAnalysisPropertiesService.DidNotReceiveWithAnyArgs().DidSetUserAnalysisProperties(default);
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        activeConfigScopeTracker.Received(1).CurrentConfigurationScopeChanged -= Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
        userSettingsProvider.Received(1).SettingsChanged -= Arg.Any<EventHandler>();
    }

    private void RaiseConfigScopeChanged(bool definitionChanged) =>
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith<ConfigurationScopeChangedEventArgs>(new(definitionChanged));

    private void RaiseSettingsChanged() =>
        userSettingsProvider.SettingsChanged += Raise.EventWith(EventArgs.Empty);

    private void SetCurrentConfiguration(string configurationScopeId, ImmutableDictionary<string, string> properties)
    {
        var newConfigScope = configurationScopeId is not null ? new ConfigurationScope(configurationScopeId) : null;
        activeConfigScopeTracker.Current.Returns(newConfigScope);
        var userSettings = new UserSettings(new AnalysisSettings(analysisProperties: properties), "any");
        userSettingsProvider.UserSettings.Returns(userSettings);
    }

    private void SetUpServiceProvider() =>
        serviceProvider.TryGetTransientService(out Arg.Any<IUserAnalysisPropertiesService>()).Returns(info =>
        {
            info[0] = userAnalysisPropertiesService;
            return true;
        });

    private SlCoreUserAnalysisPropertiesSynchronizer CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<SlCoreUserAnalysisPropertiesSynchronizer>(threadHandling, testLogger, processor => MockableInitializationProcessor.ConfigureWithWait(processor, tcs));
        return new SlCoreUserAnalysisPropertiesSynchronizer(
            activeConfigScopeTracker,
            userSettingsProvider,
            initializationProcessorFactory,
            serviceProvider,
            threadHandling);
    }

    private SlCoreUserAnalysisPropertiesSynchronizer CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<SlCoreUserAnalysisPropertiesSynchronizer>(threadHandling, testLogger);
        var testSubject = new SlCoreUserAnalysisPropertiesSynchronizer(
            activeConfigScopeTracker,
            userSettingsProvider,
            initializationProcessorFactory,
            serviceProvider,
            threadHandling);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        activeConfigScopeTracker.ClearReceivedCalls();
        userSettingsProvider.ClearReceivedCalls();
        initializationProcessorFactory.ClearReceivedCalls();
        serviceProvider.ClearReceivedCalls();
        threadHandling.ClearReceivedCalls();
        return testSubject;
    }

    private SlCoreUserAnalysisPropertiesSynchronizer CreateAndInitializeTestSubjectWithoutClearingReceivedCalls()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<SlCoreUserAnalysisPropertiesSynchronizer>(threadHandling, testLogger);
        var testSubject = new SlCoreUserAnalysisPropertiesSynchronizer(
            activeConfigScopeTracker,
            userSettingsProvider,
            initializationProcessorFactory,
            serviceProvider,
            threadHandling);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }
}

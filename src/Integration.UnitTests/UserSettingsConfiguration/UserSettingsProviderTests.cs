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

using System.Collections.Immutable;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.UserSettingsConfiguration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.UserSettingsConfiguration;

[TestClass]
public class UserSettingsProviderTests
{
    private const string SolutionSettingsFilePath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\Solution Settings\My Soution\settings.json";
    private const string SolutionGeneratedSettingsFolderPath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\Solution Settings\My Soution\";
    private const string GlobalSettingsFilePath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\settings.json";
    private const string GlobalGeneratedSettingsFolderPath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\.global";
    private const string SolutionName1 = "SolutionOne";
    private IActiveSolutionTracker activeSolutionTracker;
    private IInitializationProcessorFactory processorFactory;
    private TestLogger testLogger;
    private IThreadHandling threadHandling;
    private IGlobalSettingsStorage globalSettingsStorage;
    private ISolutionSettingsStorage solutionSettingsStorage;

    [TestInitialize]
    public void Initialize()
    {
        testLogger = new TestLogger();
        globalSettingsStorage = Substitute.For<IGlobalSettingsStorage>();
        solutionSettingsStorage = Substitute.For<ISolutionSettingsStorage>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        activeSolutionTracker = Substitute.For<IActiveSolutionTracker>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<UserSettingsProvider, IUserSettingsProvider>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IGlobalSettingsStorage>(),
            MefTestHelpers.CreateExport<ISolutionSettingsStorage>(),
            MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<UserSettingsProvider>();

    [TestMethod]
    public void Initialization_SubscribesToEvents()
    {
        var dependencies = new IRequireInitialization[] { globalSettingsStorage, solutionSettingsStorage, activeSolutionTracker };

        var testSubject = CreateAndInitializeTestSubject();
        var initializationProcessor = testSubject.InitializationProcessor;

        Received.InOrder(() =>
        {
            processorFactory.Create<UserSettingsProvider>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(collection => collection.SequenceEqual(dependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            initializationProcessor.InitializeAsync();
            globalSettingsStorage.SettingsFileChanged += Arg.Any<EventHandler>();
            solutionSettingsStorage.SettingsFileChanged += Arg.Any<EventHandler>();
            activeSolutionTracker.ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
            initializationProcessor.InitializeAsync();
        });
    }

    [TestMethod]
    public void Initialization_NoEventsRaisedBeforeInitialized()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));
        globalSettingsStorage.SettingsFileChanged += Raise.Event();
        solutionSettingsStorage.SettingsFileChanged += Raise.Event();
        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);

        InitializeTestSubject(barrier, testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));
        globalSettingsStorage.SettingsFileChanged += Raise.Event();
        solutionSettingsStorage.SettingsFileChanged += Raise.Event();
        settingsChanged.ReceivedWithAnyArgs(3).Invoke(default, default);
    }

    [TestMethod]
    public void Initialization_Disposed_DoesNothing()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        testSubject.Dispose();

        InitializeTestSubject(barrier, testSubject);

        activeSolutionTracker.DidNotReceiveWithAnyArgs().ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        globalSettingsStorage.DidNotReceiveWithAnyArgs().SettingsFileChanged += Arg.Any<EventHandler>();
        solutionSettingsStorage.DidNotReceiveWithAnyArgs().SettingsFileChanged += Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEventsAndDisposes()
    {
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        activeSolutionTracker.Received(1).ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        globalSettingsStorage.Received(1).SettingsFileChanged -= Arg.Any<EventHandler>();
        solutionSettingsStorage.Received(1).SettingsFileChanged -= Arg.Any<EventHandler>();
        globalSettingsStorage.Received(1).Dispose();
        solutionSettingsStorage.Received(1).Dispose();
    }

    [TestMethod]
    public void SolutionOpen_InvalidatesCacheAndRaisesEvents()
    {
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var testSubject = CreateAndInitializeTestSubject();
        var initialSettings = GetInitialSettings(testSubject);
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));

        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
        settingsChanged.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void SolutionClosed_InvalidatesCacheAndDoesNotRaiseEvent()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var initialSettings = GetInitialSettings(testSubject);
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(false, null));

        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void UserSettings_NoSolution_NoGlobalSettings_ReturnsDefault()
    {
        SetupNoGlobalSettings();
        SetupNoSolutionSettings();
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should()
            .BeEquivalentTo(new UserSettings(new AnalysisSettings(ImmutableDictionary<string, RuleConfig>.Empty,
                    ImmutableArray<string>.Empty,
                    ImmutableArray<string>.Empty,
                    ImmutableDictionary<string, string>.Empty),
                GlobalGeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void UserSettings_NoSolution_ReturnsGlobalSettings()
    {
        var rules = ImmutableDictionary.Create<string, RuleConfig>().Add("rules", default);
        var fileExclusions = ImmutableArray.Create("exclusions");
        SetupGlobalSettings(new GlobalAnalysisSettings(rules, fileExclusions));
        SetupNoSolutionSettings();
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should().BeEquivalentTo(new UserSettings(new AnalysisSettings(rules, fileExclusions, ImmutableArray<string>.Empty, ImmutableDictionary<string, string>.Empty),
            GlobalGeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void UserSettings_Solution_NoSolutionSettings_ReturnsGlobalSettings()
    {
        var rules = ImmutableDictionary.Create<string, RuleConfig>().Add("rules", default);
        var fileExclusions = ImmutableArray.Create("exclusions");
        SetupGlobalSettings(new GlobalAnalysisSettings(rules, fileExclusions));
        SetupSolutionSettings(solutionAnalysisSettings: null);
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should().BeEquivalentTo(new UserSettings(new AnalysisSettings(rules, fileExclusions, ImmutableArray<string>.Empty, ImmutableDictionary<string, string>.Empty),
            GlobalGeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void UserSettings_Solution_ReturnsSolutionSettings()
    {
        var rules = ImmutableDictionary.Create<string, RuleConfig>().Add("rules", default);
        var globalFileExclusions = ImmutableArray.Create("exclusions");
        SetupGlobalSettings(new GlobalAnalysisSettings(rules, globalFileExclusions));
        var properties = ImmutableDictionary.Create<string, string>().Add("properties", default);
        var solutionFileExclusions = ImmutableArray.Create("solution/exclusions");
        SetupSolutionSettings(new SolutionAnalysisSettings(properties, solutionFileExclusions));
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should().BeEquivalentTo(new UserSettings(new AnalysisSettings(rules, globalFileExclusions, solutionFileExclusions, properties), SolutionGeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void UserSettings_Solution_NoGlobal_ReturnsSolutionSettings()
    {
        SetupNoGlobalSettings();
        var properties = ImmutableDictionary.Create<string, string>().Add("properties", default);
        var solutionFileExclusions = ImmutableArray.Create("solution/exclusions");
        SetupSolutionSettings(new SolutionAnalysisSettings(properties, solutionFileExclusions));
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should().BeEquivalentTo(new UserSettings(new AnalysisSettings(ImmutableDictionary<string, RuleConfig>.Empty, ImmutableArray<string>.Empty, solutionFileExclusions, properties),
            SolutionGeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void GlobalSettingsChanged_InvalidatesCacheAndRaisesEvent()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var initialSettings = GetInitialSettings(testSubject);
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        globalSettingsStorage.SettingsFileChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(false, null));

        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
        settingsChanged.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void SolutionSettingsChanged_InvalidatesCacheAndRaisesEvent()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var initialSettings = GetInitialSettings(testSubject);
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        solutionSettingsStorage.SettingsFileChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(false, null));

        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
        settingsChanged.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    private static UserSettings GetInitialSettings(UserSettingsProvider testSubject)
    {
        var initialSettings = testSubject.UserSettings;
        testSubject.UserSettings.Should().BeSameAs(initialSettings); // cache is persistent
        return initialSettings;
    }

    private static EventHandler SubscribeToSettingsChanged(UserSettingsProvider testSubject)
    {
        var settingsChanged = Substitute.For<EventHandler>();
        testSubject.SettingsChanged += settingsChanged;
        return settingsChanged;
    }

    private UserSettingsProvider CreateAndInitializeTestSubject()
    {
        processorFactory = MockableInitializationProcessor.CreateFactory<UserSettingsProvider>(threadHandling, testLogger);
        var testSubject = new UserSettingsProvider(testLogger, globalSettingsStorage, solutionSettingsStorage, activeSolutionTracker, processorFactory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }

    private UserSettingsProvider CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        processorFactory = MockableInitializationProcessor.CreateFactory<UserSettingsProvider>(threadHandling, testLogger, p => MockableInitializationProcessor.ConfigureWithWait(p, tcs));
        return new UserSettingsProvider(testLogger, globalSettingsStorage, solutionSettingsStorage, activeSolutionTracker, processorFactory);
    }

    private static void InitializeTestSubject(TaskCompletionSource<byte> barrier, UserSettingsProvider testSubject)
    {
        barrier.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
    }

    private void SetupNoSolutionSettings()
    {
        solutionSettingsStorage.LoadSettingsFile().ReturnsNull();
        solutionSettingsStorage.SettingsFilePath.Returns((string)null);
        solutionSettingsStorage.ConfigurationBaseDirectory.Returns((string)null);
    }

    private void SetupSolutionSettings(SolutionAnalysisSettings solutionAnalysisSettings)
    {
        solutionSettingsStorage.LoadSettingsFile().Returns(solutionAnalysisSettings);
        solutionSettingsStorage.SettingsFilePath.Returns(SolutionSettingsFilePath);
        solutionSettingsStorage.ConfigurationBaseDirectory.Returns(SolutionGeneratedSettingsFolderPath);
    }

    private void SetupNoGlobalSettings() => SetupGlobalSettings(null);

    private void SetupGlobalSettings(GlobalAnalysisSettings globalAnalysisSettings)
    {
        globalSettingsStorage.LoadSettingsFile().Returns(globalAnalysisSettings);
        globalSettingsStorage.SettingsFilePath.Returns(GlobalSettingsFilePath);
        globalSettingsStorage.ConfigurationBaseDirectory.Returns(GlobalGeneratedSettingsFolderPath);
    }
}

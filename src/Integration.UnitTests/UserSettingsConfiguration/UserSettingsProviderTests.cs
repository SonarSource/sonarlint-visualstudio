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
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.UserSettingsConfiguration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.UserSettingsConfiguration;

[TestClass]
public class UserSettingsProviderTests
{
    private const string AppDataRoot = @"C:\some\path\to\appdata";
    private const string GlobalSettingsFilePath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\settings.json";
    private const string GlobalGeneratedSettingsFolderPath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\.global";
    private const string SolutionName1 = "SolutionOne";
    private const string Solution1SettingsFilePath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\SolutionSettings\SolutionOne\settings.json";
    private const string Solution1GeneratedSettingsFolderPath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\SolutionSettings\SolutionOne";
    private const string SolutionName2 = "Solution TWO";
    private const string Solution2SettingsFilePath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\SolutionSettings\Solution TWO\settings.json";
    private IActiveSolutionTracker activeSolutionTracker;
    private IEnvironmentVariableProvider environmentVariableProvider;
    private IFileSystemService fileSystem;
    private IInitializationProcessorFactory processorFactory;
    private IAnalysisSettingsSerializer serializer;
    private ISingleFileMonitorFactory singleFileMonitorFactory;
    private TestLogger testLogger;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void Initialize()
    {
        testLogger = new TestLogger();
        fileSystem = Substitute.For<IFileSystemService>();
        serializer = Substitute.For<IAnalysisSettingsSerializer>();
        singleFileMonitorFactory = Substitute.For<ISingleFileMonitorFactory>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        environmentVariableProvider = Substitute.For<IEnvironmentVariableProvider>();
        environmentVariableProvider.GetFolderPath(Environment.SpecialFolder.ApplicationData).Returns(AppDataRoot);
        activeSolutionTracker = Substitute.For<IActiveSolutionTracker>();
        activeSolutionTracker.CurrentSolutionName.Returns(null as string);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<UserSettingsProvider, IUserSettingsProvider>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<ISingleFileMonitorFactory>(),
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<IAnalysisSettingsSerializer>(),
            MefTestHelpers.CreateExport<IEnvironmentVariableProvider>(),
            MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<UserSettingsProvider>();

    [TestMethod]
    public void EnsureGlobalAnalysisSettingsFileExists_CreatedIfMissing()
    {
        fileSystem.File.Exists(GlobalSettingsFilePath).Returns(false);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.EnsureGlobalAnalysisSettingsFileExists();

        fileSystem.File.Received().Exists(GlobalSettingsFilePath);
        serializer.Received().SafeSave(GlobalSettingsFilePath, Arg.Any<GlobalAnalysisSettings>());
    }

    [TestMethod]
    public void EnsureSolutionAnalysisSettingsFileExists_NoSolutionOpen_DoesNotCreate()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(null as string);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.EnsureSolutionAnalysisSettingsFileExists();

        fileSystem.File.DidNotReceiveWithAnyArgs().Exists(default);
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(GlobalAnalysisSettings));
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(SolutionAnalysisSettings));
    }

    [TestMethod]
    public void EnsureSolutionAnalysisSettingsFileExists_CreatedIfMissing()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        fileSystem.File.Exists(Solution1SettingsFilePath).Returns(false);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.EnsureSolutionAnalysisSettingsFileExists();

        fileSystem.File.Received().Exists(Solution1SettingsFilePath);
        serializer.Received().SafeSave(Solution1SettingsFilePath, Arg.Any<SolutionAnalysisSettings>());
    }

    [TestMethod]
    public void EnsureGlobalAnalysisSettingsFileExists_NotCreatedIfExists()
    {
        fileSystem.File.Exists(GlobalSettingsFilePath).Returns(true);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.EnsureGlobalAnalysisSettingsFileExists();

        fileSystem.File.Received().Exists(GlobalSettingsFilePath);
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(GlobalAnalysisSettings));
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(SolutionAnalysisSettings));
    }

    [TestMethod]
    public void EnsureSolutionAnalysisSettingsFileExists_NotCreatedIfExists()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        fileSystem.File.Exists(Solution1SettingsFilePath).Returns(true);
        fileSystem.ClearReceivedCalls();
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.EnsureSolutionAnalysisSettingsFileExists();

        fileSystem.File.Received().Exists(Solution1SettingsFilePath);
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(GlobalAnalysisSettings));
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(SolutionAnalysisSettings));
    }

    [TestMethod]
    public void Initialization_NoSolutionOpen_CreatesGlobalWatcher_SubscribesToEvents()
    {
        var dependencies = new[] { activeSolutionTracker };
        var globalSettingsFileMonitor = Substitute.For<ISingleFileMonitor>();
        singleFileMonitorFactory.Create(GlobalSettingsFilePath).Returns(globalSettingsFileMonitor);

        var testSubject = CreateAndInitializeTestSubject();
        var initializationProcessor = testSubject.InitializationProcessor;

        Received.InOrder(() =>
        {
            processorFactory.Create<UserSettingsProvider>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(collection => collection.SequenceEqual(dependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            initializationProcessor.InitializeAsync();
            environmentVariableProvider.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            singleFileMonitorFactory.Create(GlobalSettingsFilePath);
            _ = activeSolutionTracker.CurrentSolutionName;
            globalSettingsFileMonitor.FileChanged += Arg.Any<EventHandler>();
            activeSolutionTracker.ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
            initializationProcessor.InitializeAsync();
        });
    }

    [TestMethod]
    public void Initialization_SolutionOpen_CreatesSolutionAndGlobalWatcher_SubscribesToEvents()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var dependencies = new[] { activeSolutionTracker };
        var globalSettingsFileMonitor = Substitute.For<ISingleFileMonitor>();
        singleFileMonitorFactory.Create(GlobalSettingsFilePath).Returns(globalSettingsFileMonitor);
        var solutionSettingsFileMonitor = Substitute.For<ISingleFileMonitor>();
        singleFileMonitorFactory.Create(Solution1SettingsFilePath).Returns(solutionSettingsFileMonitor);

        var testSubject = CreateAndInitializeTestSubject();
        var initializationProcessor = testSubject.InitializationProcessor;

        Received.InOrder(() =>
        {
            processorFactory.Create<UserSettingsProvider>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(collection => collection.SequenceEqual(dependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            initializationProcessor.InitializeAsync();
            environmentVariableProvider.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            singleFileMonitorFactory.Create(GlobalSettingsFilePath);
            globalSettingsFileMonitor.FileChanged += Arg.Any<EventHandler>();
            _ = activeSolutionTracker.CurrentSolutionName;
            singleFileMonitorFactory.Create(Solution1SettingsFilePath);
            solutionSettingsFileMonitor.FileChanged += Arg.Any<EventHandler>();
            activeSolutionTracker.ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
            initializationProcessor.InitializeAsync();
        });
    }

    [TestMethod]
    public void Initialization_NoEventsRaisedBeforeInitialized()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var globalFileMonitor = CreateSingleFileMonitor(GlobalSettingsFilePath);
        var solutionFileMonitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));
        globalFileMonitor.FileChanged += Raise.Event();
        solutionFileMonitor.FileChanged += Raise.Event();
        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);

        barrier.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));
        globalFileMonitor.FileChanged += Raise.Event();
        solutionFileMonitor.FileChanged += Raise.Event();
        settingsChanged.ReceivedWithAnyArgs(3).Invoke(default, default);
    }

    [TestMethod]
    public void Initialization_Disposed_DoesNothing()
    {
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        testSubject.Dispose();

        barrier.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        activeSolutionTracker.DidNotReceiveWithAnyArgs().ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        activeSolutionTracker.DidNotReceiveWithAnyArgs().ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        singleFileMonitorFactory.DidNotReceiveWithAnyArgs().Create(default);
    }

    [TestMethod]
    public void Dispose_NoSolution_UnsubscribesFromEvents()
    {
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var globalFileMonitor = CreateSingleFileMonitor(GlobalSettingsFilePath);
        var solutionFileMonitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        activeSolutionTracker.Received(1).ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        globalFileMonitor.Received(1).FileChanged -= Arg.Any<EventHandler>();
        solutionFileMonitor.DidNotReceiveWithAnyArgs().FileChanged -= Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void Dispose_Solution_UnsubscribesFromEvents()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var globalFileMonitor = CreateSingleFileMonitor(GlobalSettingsFilePath);
        var solutionFileMonitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        activeSolutionTracker.Received(1).ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        globalFileMonitor.Received(1).FileChanged -= Arg.Any<EventHandler>();
        solutionFileMonitor.Received(1).FileChanged -= Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void SolutionOpen_CreatesNewWatcher_RaisesEvent()
    {
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var solution1Monitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));

        settingsChanged.ReceivedWithAnyArgs().Invoke(default, default);
        singleFileMonitorFactory.Received().Create(Solution1SettingsFilePath);
        solution1Monitor.Received().FileChanged += Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void SolutionOpen_DoesNotDisposeGlobalWatcher()
    {
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var globalMonitor = CreateSingleFileMonitor(GlobalSettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));

        settingsChanged.ReceivedWithAnyArgs().Invoke(default, default);
        globalMonitor.DidNotReceiveWithAnyArgs().FileChanged -= Arg.Any<EventHandler>();
        globalMonitor.DidNotReceiveWithAnyArgs().Dispose();
    }

    [TestMethod]
    public void SolutionOpen_InvalidatesCache()
    {
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var testSubject = CreateAndInitializeTestSubject();
        var initialSettings = GetInitialSettings(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));

        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
        testSubject.SolutionAnalysisSettingsFilePath.Should().Be(Solution1SettingsFilePath);
    }

    [TestMethod]
    public void SolutionClosed_DisposesWatcher_DoesNotRaiseEvent()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var solution1Monitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(false, null));

        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);
        solution1Monitor.Received().FileChanged -= Arg.Any<EventHandler>();
        solution1Monitor.Received().Dispose();
    }

    [TestMethod]
    public void SolutionClosed_DoesNotDisposeGlobalWatcher_DoesNotRaiseEvent()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var globalMonitor = CreateSingleFileMonitor(GlobalSettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(false, null));

        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);
        globalMonitor.DidNotReceiveWithAnyArgs().FileChanged -= Arg.Any<EventHandler>();
        globalMonitor.DidNotReceiveWithAnyArgs().Dispose();
    }

    [TestMethod]
    public void SolutionClosed_InvalidatesCache()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var testSubject = CreateAndInitializeTestSubject();
        var initialSettings = GetInitialSettings(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(false, null));

        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
        testSubject.SolutionAnalysisSettingsFilePath.Should().Be(null);
    }

    [TestMethod]
    public void DifferentSolutionOpen_DisposesOldWatcher_CreatesNewWatcher_RaisesEvent()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var solution1Monitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var solution2Monitor = CreateSingleFileMonitor(Solution2SettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName2));

        settingsChanged.ReceivedWithAnyArgs().Invoke(default, default);
        solution1Monitor.Received().FileChanged -= Arg.Any<EventHandler>();
        solution1Monitor.Received().Dispose();
        singleFileMonitorFactory.Received().Create(Solution2SettingsFilePath);
        solution2Monitor.Received().FileChanged += Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void DifferentSolutionOpen_DoesNotDisposeGlobalWatcher()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var globalMonitor = CreateSingleFileMonitor(GlobalSettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName2));

        settingsChanged.ReceivedWithAnyArgs().Invoke(default, default);
        globalMonitor.DidNotReceiveWithAnyArgs().FileChanged -= Arg.Any<EventHandler>();
        globalMonitor.DidNotReceiveWithAnyArgs().Dispose();
    }

    [TestMethod]
    public void DifferentSolutionOpen_InvalidatesCache()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var testSubject = CreateAndInitializeTestSubject();
        var initialSettings = GetInitialSettings(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName2));

        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
        testSubject.SolutionAnalysisSettingsFilePath.Should().Be(Solution2SettingsFilePath);
    }

    [TestMethod]
    public void UserSettings_NoSolution_NoGlobalSettings_ReturnsDefault()
    {
        serializer.SafeLoad<GlobalAnalysisSettings>(GlobalSettingsFilePath).Returns((GlobalAnalysisSettings)null);
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should()
            .BeEquivalentTo(new UserSettings(new AnalysisSettings(ImmutableDictionary<string, RuleConfig>.Empty,
                    ImmutableArray<string>.Empty,
                    ImmutableDictionary<string, string>.Empty),
                GlobalGeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void UserSettings_NoSolution_ReturnsGlobalSettings()
    {
        var rules = ImmutableDictionary.Create<string, RuleConfig>().Add("rules", default);
        var fileExclusions = ImmutableArray.Create("exclusions");
        serializer.SafeLoad<GlobalAnalysisSettings>(GlobalSettingsFilePath).Returns(new GlobalAnalysisSettings(rules, fileExclusions));
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should().BeEquivalentTo(new UserSettings(new AnalysisSettings(rules, fileExclusions, ImmutableDictionary<string, string>.Empty), GlobalGeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void UserSettings_Solution_NoSolutionSettings_ReturnsGlobalSettings()
    {
        var rules = ImmutableDictionary.Create<string, RuleConfig>().Add("rules", default);
        var fileExclusions = ImmutableArray.Create("exclusions");
        serializer.SafeLoad<GlobalAnalysisSettings>(GlobalSettingsFilePath).Returns(new GlobalAnalysisSettings(rules, fileExclusions));
        serializer.SafeLoad<SolutionAnalysisSettings>(Solution1SettingsFilePath).ReturnsNull();
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should().BeEquivalentTo(new UserSettings(new AnalysisSettings(rules, fileExclusions, ImmutableDictionary<string, string>.Empty), GlobalGeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void UserSettings_Solution_ReturnsSolutionSettings()
    {
        var rules = ImmutableDictionary.Create<string, RuleConfig>().Add("rules", default);
        var fileExclusions = ImmutableArray.Create("exclusions");
        serializer.SafeLoad<GlobalAnalysisSettings>(GlobalSettingsFilePath).Returns(new GlobalAnalysisSettings(rules, fileExclusions));
        var properties = ImmutableDictionary.Create<string, string>().Add("properties", default);
        serializer.SafeLoad<SolutionAnalysisSettings>(Solution1SettingsFilePath).Returns(new SolutionAnalysisSettings(properties));
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should().BeEquivalentTo(new UserSettings(new AnalysisSettings(rules, fileExclusions, properties), Solution1GeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void UserSettings_Solution_NoGlobal_ReturnsSolutionSettings()
    {
        serializer.SafeLoad<GlobalAnalysisSettings>(GlobalSettingsFilePath).ReturnsNull();
        var properties = ImmutableDictionary.Create<string, string>().Add("properties", default);
        serializer.SafeLoad<SolutionAnalysisSettings>(Solution1SettingsFilePath).Returns(new SolutionAnalysisSettings(properties));
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var testSubject = CreateAndInitializeTestSubject();

        var userSettings = testSubject.UserSettings;

        userSettings.Should().BeEquivalentTo(new UserSettings(new AnalysisSettings(ImmutableDictionary<string, RuleConfig>.Empty, ImmutableArray<string>.Empty, properties),
            Solution1GeneratedSettingsFolderPath));
    }

    [TestMethod]
    public void SettingsChanged_NoSolution_GlobalFileChanged_InvalidatesCacheAndRaisesEvent()
    {
        var globalFileMonitor = CreateSingleFileMonitor(GlobalSettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);
        var initialSettings = GetInitialSettings(testSubject);

        globalFileMonitor.FileChanged += Raise.Event();

        settingsChanged.ReceivedWithAnyArgs().Invoke(default, default);
        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
    }

    [TestMethod]
    public void SettingsChanged_Solution_GlobalFileChanged_InvalidatesCacheAndRaisesEvent()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var globalFileMonitor = CreateSingleFileMonitor(GlobalSettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);
        var initialSettings = GetInitialSettings(testSubject);

        globalFileMonitor.FileChanged += Raise.Event();

        settingsChanged.ReceivedWithAnyArgs().Invoke(default, default);
        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
    }

    [TestMethod]
    public void SettingsChanged_Solution_SolutionFileChanged_InvalidatesCacheAndRaisesEvent()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var solutionFileMonitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);
        var initialSettings = GetInitialSettings(testSubject);

        solutionFileMonitor.FileChanged += Raise.Event();

        settingsChanged.ReceivedWithAnyArgs().Invoke(default, default);
        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
    }

    [TestMethod]
    public void SettingsChanged_SolutionChanged_SolutionFileChanged_InvalidatesCacheAndRaisesEvent()
    {
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var solutionFileMonitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();
        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));
        var settingsChanged = SubscribeToSettingsChanged(testSubject);
        var initialSettings = GetInitialSettings(testSubject);

        solutionFileMonitor.FileChanged += Raise.Event();

        settingsChanged.ReceivedWithAnyArgs().Invoke(default, default);
        testSubject.UserSettings.Should().NotBeSameAs(initialSettings);
    }

    [TestMethod]
    public void DisableRule_UpdatesGlobalSettingsWithoutRaisingEvent()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        testSubject.DisableRule("somerule");

        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);
        serializer.Received().SafeSave(GlobalSettingsFilePath, Arg.Is<GlobalAnalysisSettings>(x => x.Rules.ContainsKey("somerule") && x.Rules["somerule"].Level == RuleLevel.Off));
    }

    [TestMethod]
    public void DisableRule_EnabledRule_UpdatesGlobalSettingsWithoutRaisingEvent()
    {
        serializer.SafeLoad<GlobalAnalysisSettings>(GlobalSettingsFilePath).Returns(new GlobalAnalysisSettings(rules: ImmutableDictionary.Create<string, RuleConfig>().Add("somerule", new RuleConfig(RuleLevel.On)), ImmutableArray<string>.Empty));
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        testSubject.DisableRule("somerule");

        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);
        serializer.Received().SafeSave(GlobalSettingsFilePath, Arg.Is<GlobalAnalysisSettings>(x => x.Rules.ContainsKey("somerule") && x.Rules["somerule"].Level == RuleLevel.Off));
    }

    [TestMethod]
    public void DisableRule_OtherRuleNotDisabled_UpdatesGlobalSettingsWithoutRaisingEvent()
    {
        serializer.SafeLoad<GlobalAnalysisSettings>(GlobalSettingsFilePath).Returns(new GlobalAnalysisSettings(rules: ImmutableDictionary.Create<string, RuleConfig>().Add("someotherrule", new RuleConfig(RuleLevel.On)), ImmutableArray<string>.Empty));
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        testSubject.DisableRule("somerule");

        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);
        serializer.Received().SafeSave(GlobalSettingsFilePath, Arg.Is<GlobalAnalysisSettings>(x => x.Rules.ContainsKey("somerule") && x.Rules["somerule"].Level == RuleLevel.Off && x.Rules.ContainsKey("someotherrule") && x.Rules["someotherrule"].Level == RuleLevel.On));
    }

    [TestMethod]
    public void UpdateFileExclusions_UpdatesGlobalSettingsWithoutRaisingEvent()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var settingsChanged = SubscribeToSettingsChanged(testSubject);
        string[] exclusions = ["1", "two", "3"];

        testSubject.UpdateFileExclusions(exclusions);

        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);
        serializer.Received().SafeSave(GlobalSettingsFilePath, Arg.Is<GlobalAnalysisSettings>(x => x.UserDefinedFileExclusions.SequenceEqual(exclusions, default)));
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

    private ISingleFileMonitor CreateSingleFileMonitor(string path)
    {
        var singleFileMonitor = Substitute.For<ISingleFileMonitor>();
        singleFileMonitor.MonitoredFilePath.Returns(path);
        singleFileMonitorFactory.Create(path).Returns(singleFileMonitor);
        return singleFileMonitor;
    }

    private UserSettingsProvider CreateAndInitializeTestSubject()
    {
        processorFactory = MockableInitializationProcessor.CreateFactory<UserSettingsProvider>(threadHandling, testLogger);
        var testSubject = new UserSettingsProvider(testLogger, singleFileMonitorFactory, fileSystem, serializer, environmentVariableProvider, activeSolutionTracker, processorFactory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }

    private UserSettingsProvider CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        processorFactory = MockableInitializationProcessor.CreateFactory<UserSettingsProvider>(threadHandling, testLogger, p => MockableInitializationProcessor.ConfigureWithWait(p, tcs));
        return new UserSettingsProvider(testLogger, singleFileMonitorFactory, fileSystem, serializer, environmentVariableProvider, activeSolutionTracker, processorFactory);
    }
}

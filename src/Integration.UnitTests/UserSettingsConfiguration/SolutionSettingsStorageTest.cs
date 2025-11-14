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

using System.IO;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.UserSettingsConfiguration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.UserSettingsConfiguration;

[TestClass]
public class SolutionSettingsStorageTest
{
    private const string AppDataRoot = @"C:\some\path\to\appdata";
    private const string SolutionName1 = "SolutionOne";
    private const string Solution1SettingsFilePath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\SolutionSettings\SolutionOne\settings.json";
    private const string SolutionName2 = "Solution TWO";
    private const string Solution2SettingsFilePath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\SolutionSettings\Solution TWO\settings.json";
    private IActiveSolutionTracker activeSolutionTracker;
    private IEnvironmentVariableProvider environmentVariableProvider;
    private IFileSystemService fileSystem;
    private IInitializationProcessorFactory processorFactory;
    private IAnalysisSettingsSerializer serializer;
    private ISingleFileMonitorFactory singleFileMonitorFactory;
    private IThreadHandling threadHandling;
    private ISingleFileMonitor solution1FileMonitor;
    private TestLogger testLogger;

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
        solution1FileMonitor = Substitute.For<ISingleFileMonitor>();
        MockFileMonitor(solution1FileMonitor, Solution1SettingsFilePath);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SolutionSettingsStorage, ISolutionSettingsStorage>(
            MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
            MefTestHelpers.CreateExport<ISingleFileMonitorFactory>(),
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<IEnvironmentVariableProvider>(),
            MefTestHelpers.CreateExport<IAnalysisSettingsSerializer>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SolutionSettingsStorage>();

    [TestMethod]
    public void Initialization_NoSolutionOpen_DoesNotCreateWatcher()
    {
        var dependencies = new[] { activeSolutionTracker };

        var testSubject = CreateAndInitializeTestSubject();
        var initializationProcessor = testSubject.InitializationProcessor;

        Received.InOrder(() =>
        {
            processorFactory.Create<SolutionSettingsStorage>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(collection => collection.SequenceEqual(dependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            initializationProcessor.InitializeAsync();
            environmentVariableProvider.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _ = activeSolutionTracker.CurrentSolutionName;
            activeSolutionTracker.ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
            initializationProcessor.InitializeAsync();
        });

        singleFileMonitorFactory.DidNotReceiveWithAnyArgs().Create(default);
    }

    [TestMethod]
    public void Initialization_SolutionOpen_CreatesWatcherAndSubscribesToEvents()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var dependencies = new[] { activeSolutionTracker };

        var testSubject = CreateAndInitializeTestSubject();
        var initializationProcessor = testSubject.InitializationProcessor;

        Received.InOrder(() =>
        {
            processorFactory.Create<SolutionSettingsStorage>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(collection => collection.SequenceEqual(dependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            initializationProcessor.InitializeAsync();
            environmentVariableProvider.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _ = activeSolutionTracker.CurrentSolutionName;
            singleFileMonitorFactory.Create(Solution1SettingsFilePath);
            solution1FileMonitor.FileChanged += Arg.Any<EventHandler>();
            activeSolutionTracker.ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
            initializationProcessor.InitializeAsync();
        });
        testSubject.SettingsFilePath.Should().Be(Solution1SettingsFilePath);
        testSubject.ConfigurationBaseDirectory.Should().Be(Path.GetDirectoryName(Solution1SettingsFilePath));
    }

    [TestMethod]
    public void Initialization_NoEventsRaisedBeforeInitialized()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));
        solution1FileMonitor.FileChanged += Raise.Event();
        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);

        InitializeTestSubject(barrier, testSubject);

        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(true, SolutionName1));
        solution1FileMonitor.FileChanged += Raise.Event();
        settingsChanged.ReceivedWithAnyArgs(2).Invoke(default, default);
    }

    [TestMethod]
    public void Initialization_Disposed_DoesNothing()
    {
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        testSubject.Dispose();

        InitializeTestSubject(barrier, testSubject);

        activeSolutionTracker.DidNotReceiveWithAnyArgs().ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        activeSolutionTracker.DidNotReceiveWithAnyArgs().ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        singleFileMonitorFactory.DidNotReceiveWithAnyArgs().Create(default);
    }

    [TestMethod]
    public void EnsureSettingsFileExists_NoSolutionOpen_DoesNotCreate()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(null as string);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.EnsureSettingsFileExists();

        fileSystem.File.DidNotReceiveWithAnyArgs().Exists(default);
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(GlobalRawAnalysisSettings));
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(SolutionRawAnalysisSettings));
    }

    [TestMethod]
    public void EnsureSettingsFileExists_CreatedIfMissing()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        fileSystem.File.Exists(Solution1SettingsFilePath).Returns(false);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.EnsureSettingsFileExists();

        fileSystem.File.Received().Exists(Solution1SettingsFilePath);
        serializer.Received().SafeSave(Solution1SettingsFilePath, Arg.Any<SolutionRawAnalysisSettings>());
    }

    [TestMethod]
    public void EnsureSettingsFileExists_NotCreatedIfExists()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        fileSystem.File.Exists(Solution1SettingsFilePath).Returns(true);
        fileSystem.ClearReceivedCalls();
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.EnsureSettingsFileExists();

        fileSystem.File.Received().Exists(Solution1SettingsFilePath);
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(SolutionRawAnalysisSettings));
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
        testSubject.SettingsFilePath.Should().Be(Solution1SettingsFilePath);
        testSubject.ConfigurationBaseDirectory.Should().Be(Path.GetDirectoryName(Solution1SettingsFilePath));
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
        testSubject.SettingsFilePath.Should().BeNull();
        testSubject.ConfigurationBaseDirectory.Should().BeNull();
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
        testSubject.SettingsFilePath.Should().Be(Solution2SettingsFilePath);
        testSubject.ConfigurationBaseDirectory.Should().Be(Path.GetDirectoryName(Solution2SettingsFilePath));
    }

    [TestMethod]
    public void SaveSettingsFile_SavesSettings()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var testSubject = CreateAndInitializeTestSubject();
        var analysisSettings = new SolutionRawAnalysisSettings();

        testSubject.SaveSettingsFile(analysisSettings);

        serializer.Received(1).SafeSave(Solution1SettingsFilePath, analysisSettings);
    }

    [TestMethod]
    public void LoadSettingsFile_SavesSettings()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var testSubject = CreateAndInitializeTestSubject();
        var expectedSettings = new SolutionRawAnalysisSettings();
        serializer.SafeLoad<SolutionRawAnalysisSettings>(Solution1SettingsFilePath).Returns(expectedSettings);

        var result = testSubject.LoadSettingsFile();

        serializer.Received(1).SafeLoad<SolutionRawAnalysisSettings>(Solution1SettingsFilePath);
        result.Should().BeSameAs(expectedSettings);
    }

    [TestMethod]
    public void Dispose_NoSolution_UnsubscribesFromEvents()
    {
        activeSolutionTracker.CurrentSolutionName.ReturnsNull();
        var solutionFileMonitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        activeSolutionTracker.Received(1).ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        solutionFileMonitor.DidNotReceiveWithAnyArgs().FileChanged -= Arg.Any<EventHandler>();
        solutionFileMonitor.DidNotReceiveWithAnyArgs().Dispose();
    }

    [TestMethod]
    public void Dispose_Solution_UnsubscribesFromEvents()
    {
        activeSolutionTracker.CurrentSolutionName.Returns(SolutionName1);
        var solutionFileMonitor = CreateSingleFileMonitor(Solution1SettingsFilePath);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        activeSolutionTracker.Received(1).ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        solutionFileMonitor.Received(1).FileChanged -= Arg.Any<EventHandler>();
        solutionFileMonitor.Received(1).Dispose();
    }

    private SolutionSettingsStorage CreateAndInitializeTestSubject()
    {
        processorFactory = MockableInitializationProcessor.CreateFactory<SolutionSettingsStorage>(threadHandling, testLogger);
        var testSubject = new SolutionSettingsStorage(activeSolutionTracker, singleFileMonitorFactory, fileSystem, environmentVariableProvider, serializer, processorFactory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }

    private SolutionSettingsStorage CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        processorFactory = MockableInitializationProcessor.CreateFactory<SolutionSettingsStorage>(threadHandling, testLogger, p => MockableInitializationProcessor.ConfigureWithWait(p, tcs));
        return new SolutionSettingsStorage(activeSolutionTracker, singleFileMonitorFactory, fileSystem, environmentVariableProvider, serializer, processorFactory);
    }

    private ISingleFileMonitor CreateSingleFileMonitor(string path)
    {
        var fileMonitor = Substitute.For<ISingleFileMonitor>();
        MockFileMonitor(fileMonitor, path);
        return fileMonitor;
    }

    private void MockFileMonitor(ISingleFileMonitor fileMonitor, string path)
    {
        fileMonitor.MonitoredFilePath.Returns(path);
        singleFileMonitorFactory.Create(path).Returns(fileMonitor);
    }

    private static EventHandler SubscribeToSettingsChanged(SolutionSettingsStorage testSubject)
    {
        var settingsChanged = Substitute.For<EventHandler>();
        testSubject.SettingsFileChanged += settingsChanged;
        return settingsChanged;
    }

    private static void InitializeTestSubject(TaskCompletionSource<byte> barrier, SolutionSettingsStorage testSubject)
    {
        barrier.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
    }
}

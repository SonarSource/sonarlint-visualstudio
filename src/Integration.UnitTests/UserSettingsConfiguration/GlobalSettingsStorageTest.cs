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

using NuGet;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.UserSettingsConfiguration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.UserSettingsConfiguration;

[TestClass]
public class GlobalSettingsStorageTest
{
    private const string AppDataRoot = @"C:\some\path\to\appdata";
    private const string GlobalSettingsFilePath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\settings.json";
    private const string GlobalGeneratedSettingsFolderPath = @"C:\some\path\to\appdata\SonarLint for Visual Studio\.global";
    private IEnvironmentVariableProvider environmentVariableProvider;
    private IFileSystemService fileSystem;
    private IInitializationProcessorFactory processorFactory;
    private IAnalysisSettingsSerializer serializer;
    private ISingleFileMonitorFactory singleFileMonitorFactory;
    private IThreadHandling threadHandling;
    private ISingleFileMonitor fileMonitor;
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
        MockFileMonitor(GlobalSettingsFilePath);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<GlobalSettingsStorage, IGlobalSettingsStorage>(
            MefTestHelpers.CreateExport<ISingleFileMonitorFactory>(),
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<IEnvironmentVariableProvider>(),
            MefTestHelpers.CreateExport<IAnalysisSettingsSerializer>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<GlobalSettingsStorage>();

    [TestMethod]
    public void Initialization_CreatesWatcherAndSubscribesToEvents()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var initializationProcessor = testSubject.InitializationProcessor;

        Received.InOrder(() =>
        {
            processorFactory.Create<GlobalSettingsStorage>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(collection => collection.IsEmpty()),
                Arg.Any<Func<IThreadHandling, Task>>());
            initializationProcessor.InitializeAsync();
            environmentVariableProvider.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            serializer.SafeSave(GlobalSettingsFilePath, Arg.Any<GlobalRawAnalysisSettings>());
            singleFileMonitorFactory.Create(GlobalSettingsFilePath);
            fileMonitor.FileChanged += Arg.Any<EventHandler>();
            initializationProcessor.InitializeAsync();
        });
        testSubject.SettingsFilePath.Should().Be(GlobalSettingsFilePath);
        testSubject.ConfigurationBaseDirectory.Should().Be(GlobalGeneratedSettingsFolderPath);
    }

    [TestMethod]
    public void Initialization_NoEventsRaisedBeforeInitialized()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        var settingsChanged = SubscribeToSettingsChanged(testSubject);

        fileMonitor.FileChanged += Raise.Event();
        settingsChanged.DidNotReceiveWithAnyArgs().Invoke(default, default);

        InitializeTestSubject(barrier, testSubject);

        fileMonitor.FileChanged += Raise.Event();
        settingsChanged.ReceivedWithAnyArgs(1).Invoke(default, default);
    }

    [TestMethod]
    public void Initialization_Disposed_DoesNothing()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        testSubject.Dispose();

        InitializeTestSubject(barrier, testSubject);

        singleFileMonitorFactory.DidNotReceiveWithAnyArgs().Create(default);
    }

    [TestMethod]
    public void Initialization_CreatesSettingsFileIfMissing()
    {
        fileSystem.File.Exists(GlobalSettingsFilePath).Returns(false);

        CreateAndInitializeTestSubject();

        fileSystem.File.Received().Exists(GlobalSettingsFilePath);
        serializer.Received().SafeSave(GlobalSettingsFilePath, Arg.Any<GlobalRawAnalysisSettings>());
    }

    [TestMethod]
    public void Initialization_SettingsFileNotCreatedIfExists()
    {
        fileSystem.File.Exists(GlobalSettingsFilePath).Returns(true);
        fileSystem.ClearReceivedCalls();

        CreateAndInitializeTestSubject();

        fileSystem.File.Received().Exists(GlobalSettingsFilePath);
        serializer.DidNotReceiveWithAnyArgs().SafeSave(default, default(GlobalRawAnalysisSettings));
    }

    [TestMethod]
    public void SaveSettingsFile_SavesSettings()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var analysisSettings = new GlobalRawAnalysisSettings();

        testSubject.SaveSettingsFile(analysisSettings);

        serializer.Received(1).SafeSave(GlobalSettingsFilePath, analysisSettings);
    }

    [TestMethod]
    public void LoadSettingsFile_SavesSettings()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var expectedSettings = new GlobalRawAnalysisSettings();
        serializer.SafeLoad<GlobalRawAnalysisSettings>(GlobalSettingsFilePath).Returns(expectedSettings);

        var result = testSubject.LoadSettingsFile();

        serializer.Received(1).SafeLoad<GlobalRawAnalysisSettings>(GlobalSettingsFilePath);
        result.Should().BeSameAs(expectedSettings);
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        fileMonitor.Received(1).FileChanged -= Arg.Any<EventHandler>();
        fileMonitor.Received(1).Dispose();
    }

    private GlobalSettingsStorage CreateAndInitializeTestSubject()
    {
        processorFactory = MockableInitializationProcessor.CreateFactory<GlobalSettingsStorage>(threadHandling, testLogger);
        var testSubject = new GlobalSettingsStorage(singleFileMonitorFactory, fileSystem, environmentVariableProvider, serializer, processorFactory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }

    private GlobalSettingsStorage CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        processorFactory = MockableInitializationProcessor.CreateFactory<GlobalSettingsStorage>(threadHandling, testLogger, p => MockableInitializationProcessor.ConfigureWithWait(p, tcs));
        return new GlobalSettingsStorage(singleFileMonitorFactory, fileSystem, environmentVariableProvider, serializer, processorFactory);
    }

    private void MockFileMonitor(string path)
    {
        fileMonitor = Substitute.For<ISingleFileMonitor>();
        fileMonitor.MonitoredFilePath.Returns(path);
        singleFileMonitorFactory.Create(path).Returns(fileMonitor);
    }

    private static EventHandler SubscribeToSettingsChanged(GlobalSettingsStorage testSubject)
    {
        var settingsChanged = Substitute.For<EventHandler>();
        testSubject.SettingsFileChanged += settingsChanged;
        return settingsChanged;
    }

    private static void InitializeTestSubject(TaskCompletionSource<byte> barrier, GlobalSettingsStorage testSubject)
    {
        barrier.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
    }
}

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.IO.Abstractions;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.Core.Resources;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.UserRuleSettings;

[TestClass]
public class UserSettingsProviderTests
{
    private const string SettingsFilePath = "settings.json";

    private TestLogger testLogger;
    private UserSettingsProvider userSettingsProvider;
    private IFileSystem fileSystem;
    private ISingleFileMonitorFactory singleFileMonitorFactory;
    private ISingleFileMonitor singleFileMonitor;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void Initialize()
    {
        testLogger = new TestLogger();
        fileSystem = Substitute.For<IFileSystem>();
        singleFileMonitorFactory = Substitute.For<ISingleFileMonitorFactory>();
        singleFileMonitor = Substitute.For<ISingleFileMonitor>();

        singleFileMonitor.MonitoredFilePath.Returns(SettingsFilePath);
        singleFileMonitorFactory.Create(Arg.Any<string>()).Returns(singleFileMonitor);

        userSettingsProvider = CreateUserSettingsProvider(testLogger, fileSystem, singleFileMonitorFactory);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<UserSettingsProvider, IUserSettingsProvider>(
            MefTestHelpers.CreateExport<ILogger>(), 
            MefTestHelpers.CreateExport<ISingleFileMonitorFactory>(singleFileMonitorFactory));
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<UserSettingsProvider>();
    }

    [TestMethod]
    public void Ctor_NoSettingsFile_EmptySettingsReturned()
    {
        // Arrange
        fileSystem.File.Exists(SettingsFilePath).Returns(false);

        // Assert
        CheckSettingsAreEmpty(userSettingsProvider.UserSettings);
        testLogger.AssertOutputStringExists(Strings.Settings_UsingDefaultSettings);
    }

    [TestMethod]
    public void Ctor_NullArguments()
    {
        Action act = () => CreateUserSettingsProvider(null, fileSystem, singleFileMonitorFactory);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

        act = () => CreateUserSettingsProvider(testLogger, null, singleFileMonitorFactory);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");

        act = () => CreateUserSettingsProvider(testLogger, fileSystem, singleFileMonitorFactoryMock:null);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("singleFileMonitorFactory");
    }

    [TestMethod]
    public void Ctor_ErrorLoadingSettings_ErrorSquashed_AndEmptySettingsReturned()
    {
        // Arrange
        fileSystem.File.Exists(SettingsFilePath).Returns(true);
        fileSystem.File.ReadAllText(SettingsFilePath).Throws(new InvalidOperationException("custom error message"));

        CreateUserSettingsProvider(testLogger, fileSystem, singleFileMonitorFactory);

        // Assert
        CheckSettingsAreEmpty(userSettingsProvider.UserSettings);
        testLogger.AssertPartialOutputStringExists("custom error message");
    }

    [TestMethod]
    public void Ctor_DoesNotCallAnyNonFreeThreadedServices()
    {
        var logger = Substitute.For<ILogger>();
        var fileMonitor = Substitute.For<ISingleFileMonitor>();
        singleFileMonitorFactory.Create(Arg.Any<string>()).Returns(fileMonitor);

        CreateUserSettingsProvider(logger, fileSystem, singleFileMonitorFactory);

        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls.
        logger.ReceivedCalls().Should().BeEmpty();
        fileSystem.ReceivedCalls().Should().BeEmpty();
        fileMonitor.ReceivedCalls().Count().Should().Be(1);
        fileMonitor.Received(1).FileChanged += Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void EnsureFileExists_CreatedIfMissing()
    {
        // Arrange
        fileSystem.File.Exists(SettingsFilePath).Returns(false);

        // Act
        userSettingsProvider.EnsureFileExists();

        // Assert
        fileSystem.File.Received(2).Exists(SettingsFilePath);
        fileSystem.File.WriteAllText(SettingsFilePath, Arg.Any<string>());
    }

    [TestMethod]
    public void EnsureFileExists_NotCreatedIfExists()
    {
        // Arrange
        fileSystem.File.Exists(SettingsFilePath).Returns(true);
        fileSystem.ClearReceivedCalls();

        // Act
        userSettingsProvider.EnsureFileExists();

        // Assert
        fileSystem.File.Received(1).Exists(SettingsFilePath);
        fileSystem.File.DidNotReceive().WriteAllText(Arg.Any<string>(), Arg.Any<string>());
    }

    [TestMethod]
    public void RealFile_DisableRule_FileDoesNotExist_FileCreated()
    {
        var dir = CreateTestSpecificDirectory();
        var settingsFile = Path.Combine(dir, "settings.txt");

        var logger = new TestLogger(logToConsole: true);
        var testSubject = CreateUserSettingsProvider(logger, new FileSystem(), singleFileMonitorFactory, settingsFile);

        // Sanity check of test setup
        testSubject.UserSettings.RulesSettings.Rules.Count.Should().Be(0);
        File.Exists(settingsFile).Should().BeFalse();

        // Act - Disable a rule
        testSubject.DisableRule("cpp:S123");

        // Check the data on disc
        File.Exists(settingsFile).Should().BeTrue();

        var reloadedSettings = LoadSettings(settingsFile);
        reloadedSettings.Rules.Count.Should().Be(1);
        reloadedSettings.Rules["cpp:S123"].Level.Should().Be(RuleLevel.Off);
    }

    [TestMethod]
    public void RealFile_DisablePreviouslyEnabledRule()
    {
        var dir = CreateTestSpecificDirectory();
        var settingsFile = Path.Combine(dir, "settings.txt");

        var initialSettings = new RulesSettings
        {
            Rules = new Dictionary<string, RuleConfig>
            {
                { "javascript:S111", new RuleConfig { Level = RuleLevel.On } },
                { "cpp:S111", new RuleConfig { Level = RuleLevel.On } },
                { "xxx:S222", new RuleConfig { Level = RuleLevel.On } }
            }
        };

        SaveSettings(settingsFile, initialSettings);

        var logger = new TestLogger(logToConsole: true);
        var testSubject = CreateUserSettingsProvider(logger, new FileSystem(), singleFileMonitorFactory, settingsFile);

        // Sanity check of test setup
        testSubject.UserSettings.RulesSettings.Rules.Count.Should().Be(3);

        // Act - Disable a rule
        testSubject.DisableRule("cpp:S111");

        // Check the data on disc
        File.Exists(settingsFile).Should().BeTrue();

        var reloadedSettings = LoadSettings(settingsFile);
        reloadedSettings.Rules.Count.Should().Be(3);
        reloadedSettings.Rules["javascript:S111"].Level.Should().Be(RuleLevel.On);
        reloadedSettings.Rules["cpp:S111"].Level.Should().Be(RuleLevel.Off);
        reloadedSettings.Rules["xxx:S222"].Level.Should().Be(RuleLevel.On);
    }

    [TestMethod]
    public void SafeLoadUserSettings_UpdatesUserSettings()
    {
        var dir = CreateTestSpecificDirectory();
        var settingsFile = Path.Combine(dir, "settings.txt");
        var testSubject = CreateUserSettingsProvider(testLogger, new FileSystem(), singleFileMonitorFactory, settingsFile);
        testSubject.UserSettings.RulesSettings.Rules.Count.Should().Be(0);

        var newSettings = new RulesSettings
        {
            Rules = new Dictionary<string, RuleConfig>
            {
                { "javascript:S111", new RuleConfig { Level = RuleLevel.On } },
            }
        };
        SaveSettings(settingsFile, newSettings);
        testSubject.SafeLoadUserSettings();

        testSubject.UserSettings.RulesSettings.Rules.Count.Should().Be(1);
    }

    [TestMethod]
    public void FileChanges_EventsRaised()
    {
        int settingsChangedEventCount = 0;

        userSettingsProvider.SettingsChanged += (s, args) => settingsChangedEventCount++;

        singleFileMonitor.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
        settingsChangedEventCount.Should().Be(1);

        // 2. Simulate another event when the file is valid - valid settings should be returned
        singleFileMonitor.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
        settingsChangedEventCount.Should().Be(2);
    }

    [TestMethod]
    public void FileChanges_UserSettingsAreLoaded()
    {
        singleFileMonitor.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
      
        fileSystem.Received(1).File.Exists(SettingsFilePath);
    }

    [TestMethod]
    public void ConstructAndDispose()
    {
        singleFileMonitor.DidNotReceive().Dispose();

        userSettingsProvider.Dispose();

        singleFileMonitor.Received(1).Dispose();
        singleFileMonitor.Received(1).FileChanged -= Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void SettingsChangeNotificationIsRaised()
    {
        // We're deliberately returning different data on each call to IFile.ReadAllText
        // so we can check that the provider is correctly reloading and using the file data,
        // and not re-using the in-memory version.
        const string originalData = "{}";
        const string modifiedData = @"{
    'sonarlint.rules': {
        'typescript:S2685': {
            'level': 'on'
        }
    }
}";
        var fileSystemMock = CreateMockFile(SettingsFilePath, originalData);
        var settingsProvider = new UserSettingsProvider(Substitute.For<ILogger>(), singleFileMonitorFactory, fileSystemMock, SettingsFilePath);
        int eventCount = 0;
        var settingsChangedEventReceived = new ManualResetEvent(initialState: false);

        settingsProvider.UserSettings.RulesSettings.Rules.Count.Should().Be(0); // sanity check of setup

        settingsProvider.SettingsChanged += (s, args) =>
        {
            eventCount++;
            settingsChangedEventReceived.Set();
        };

        // 1. Disable a rule
        // Should trigger a save, but should not *directly* raise a "SettingsChanged" event
        settingsProvider.DisableRule("dummyRule");
        eventCount.Should().Be(0);

        // 2. Now simulate a file-change event
        fileSystemMock.File.ReadAllText(SettingsFilePath).Returns(modifiedData);
        singleFileMonitor.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

        // Check the settings change event was raised
        eventCount.Should().Be(1);

        // Check the data was actually reloaded from the file
        settingsProvider.UserSettings.RulesSettings.Rules.Count.Should().Be(1);
        settingsProvider.UserSettings.RulesSettings.Rules["typescript:S2685"].Level.Should().Be(RuleLevel.On);
    }

    private UserSettingsProvider CreateUserSettingsProvider(ILogger logger, IFileSystem fileSystemMock, ISingleFileMonitorFactory singleFileMonitorFactoryMock, string settingsPath = null)
    {
        settingsPath ??= SettingsFilePath;

        return new UserSettingsProvider(logger, singleFileMonitorFactoryMock, fileSystemMock, settingsPath);    
    }

    private static void CheckSettingsAreEmpty(UserSettings settings)
    {
        settings.Should().NotBeNull();
        settings.RulesSettings.Should().NotBeNull();
        settings.RulesSettings.Rules.Should().NotBeNull();
        settings.RulesSettings.Rules.Count.Should().Be(0);
    }

    private static void SaveSettings(string filePath, RulesSettings userSettings)
    {
        var serializer = new RulesSettingsSerializer(new FileSystem(), new TestLogger());
        serializer.SafeSave(filePath, userSettings);
    }

    private RulesSettings LoadSettings(string filePath)
    {
        var serializer = new RulesSettingsSerializer(new FileSystem(), new TestLogger());
        return serializer.SafeLoad(filePath);
    }

    private string CreateTestSpecificDirectory()
    {
        var dir = Path.Combine(TestContext.DeploymentDirectory, TestContext.TestName);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static IFileSystem CreateMockFile(string filePath, string contents)
    {
        var mockFile = Substitute.For<IFileSystem>();
        mockFile.File.Exists(filePath).Returns(true);
        mockFile.File.ReadAllText(filePath).Returns(contents);
        return mockFile;
    }
}

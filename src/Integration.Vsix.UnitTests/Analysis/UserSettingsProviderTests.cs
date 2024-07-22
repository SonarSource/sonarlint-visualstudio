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
using EnvDTE;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Rules;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class UserSettingsProviderTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<UserSettingsProvider, IUserSettingsProvider>(
                MefTestHelpers.CreateExport<ILogger>(), MefTestHelpers.CreateExport<ISLCoreServiceProvider>());
        }

        [TestMethod]
        public void Ctor_DoesNotCallAnyNonFreeThreadedServices()
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            var fileSystemMock = Substitute.For<IFileSystem>();
            var fileMonitorMock = Substitute.For<ISingleFileMonitor>();

            // Act
            _ = CreateTestSubject(logger, fileSystemMock, fileMonitorMock);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            logger.ReceivedCalls().Should().BeEmpty();
            fileSystemMock.ReceivedCalls().Should().BeEmpty();

            fileMonitorMock.ReceivedCalls().Count().Should().Be(2);
            _ = fileMonitorMock.Received(1).MonitoredFilePath;
            fileMonitorMock.Received(1).FileChanged += Arg.Any<EventHandler>();
        }

        [TestMethod]
        public void Ctor_NullArguments()
        {
            var mockSingleFileMonitor = Substitute.For<ISingleFileMonitor>();var mockSlCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();

            Action act = () => CreateUserSettingsProvider(null, new FileSystem(), mockSingleFileMonitor, mockSlCoreServiceProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            act = () => CreateUserSettingsProvider(new TestLogger(), null, mockSingleFileMonitor, mockSlCoreServiceProvider); 
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");

            act = () => CreateUserSettingsProvider(new TestLogger(), new FileSystem(), null, mockSlCoreServiceProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("settingsFileMonitor");
        }

        [TestMethod]
        public void Ctor_NoSettingsFile_EmptySettingsReturned()
        {
            // Arrange
            var fileSystemMock = Substitute.For<IFileSystem>();
            fileSystemMock.File.Exists("nonExistentFile").Returns(false);
            var testLogger = new TestLogger();

            // Act
            var testSubject = CreateTestSubject(testLogger, fileSystemMock, CreateMockFileMonitor("nonexistentFile"));

            // Assert
            CheckSettingsAreEmpty(testSubject.UserSettings);
            testLogger.AssertOutputStringExists(AnalysisStrings.Settings_UsingDefaultSettings);
        }

        [TestMethod]
        public void Ctor_ErrorLoadingSettings_ErrorSquashed_AndEmptySettingsReturned()
        {
            // Arrange
            var fileSystemMock = Substitute.For<IFileSystem>();
            fileSystemMock.File.Exists("settings.file").Returns(true);
            fileSystemMock.File.ReadAllText("settings.file").Throws(new System.InvalidOperationException("custom error message"));

            var logger = new TestLogger(logToConsole: true);

            // Act
            var testSubject = CreateTestSubject(logger, fileSystemMock, CreateMockFileMonitor("settings.file"));

            // Assert
            CheckSettingsAreEmpty(testSubject.UserSettings);
            logger.AssertPartialOutputStringExists("custom error message");
        }

        [TestMethod]
        public void FileChanges_EventsRaised()
        {
            var fileSystemMock = Substitute.For<IFileSystem>();
            fileSystemMock.File.Exists("settings.file").Returns(true);
            var fileMonitorMock = CreateMockFileMonitor("settings.file");

            int settingsChangedEventCount = 0;

            const string invalidSettingsData = "NOT VALID JSON";
            const string validSettingsData = @"{
    'sonarlint.rules': {
        'typescript:S2685': {
            'level': 'on'
        }
    }
}";
            var logger = new TestLogger();
            var testSubject = CreateTestSubject(logger, fileSystemMock, fileMonitorMock);
            testSubject.SettingsChanged += (s, args) => settingsChangedEventCount++;
            logger.Reset();

            // 1. Simulate the file change when the file is invalid
            fileSystemMock.File.ReadAllText("settings.file").Returns(invalidSettingsData);
            fileMonitorMock.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

            // Assert
            settingsChangedEventCount.Should().Be(1);
            CheckSettingsAreEmpty(testSubject.UserSettings);

            // 2. Simulate another event when the file is valid - valid settings should be returned
            fileSystemMock.File.ReadAllText("settings.file").Returns(validSettingsData);
            fileMonitorMock.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

            // Assert
            settingsChangedEventCount.Should().Be(2);
            testSubject.UserSettings.Should().NotBeNull();
            testSubject.UserSettings.RulesSettings.Should().NotBeNull();
            testSubject.UserSettings.RulesSettings.Rules.Should().NotBeNull();
            testSubject.UserSettings.RulesSettings.Rules.Count.Should().Be(1);
        }

        [TestMethod]
        public void EnsureFileExists_CreatedIfMissing()
        {
            // Arrange
            const string fileName = "c:\\missingFile.txt";

            var fileSystemMock = Substitute.For<IFileSystem>();
            fileSystemMock.File.Exists(fileName).Returns(false);

            var testSubject = CreateTestSubject(new TestLogger(), fileSystemMock, CreateMockFileMonitor(fileName));

            // Act
            testSubject.EnsureFileExists();

            // Assert
            fileSystemMock.File.Received(2).Exists(fileName);
            fileSystemMock.File.WriteAllText(fileName, Arg.Any<string>());
        }

        [TestMethod]
        public void EnsureFileExists_NotCreatedIfExists()
        {
            // Arrange
            const string fileName = "c:\\subDir1\\existingFile.txt";

            var fileSystemMock = Substitute.For<IFileSystem>();
            fileSystemMock.File.Exists(fileName).Returns(true);

            var testSubject = CreateTestSubject(new TestLogger(), fileSystemMock, CreateMockFileMonitor(fileName));
            fileSystemMock.ClearReceivedCalls();

            // Act
            testSubject.EnsureFileExists();

            // Assert
            fileSystemMock.File.Received(1).Exists(fileName);
            fileSystemMock.File.DidNotReceive().WriteAllText(Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public void ConstructAndDispose()
        {
            const string fileName = "c:\\aaa\\bbb\\file.txt";
            // Arrange
            var fileSystemMock = Substitute.For<IFileSystem>();
            fileSystemMock.File.Exists(fileName).Returns(false);

            var fileMonitorMock = CreateMockFileMonitor(fileName);

            // 1. Construct
            var testSubject = CreateTestSubject(new TestLogger(), fileSystemMock, fileMonitorMock);
            testSubject.SettingsFilePath.Should().Be(fileName);
            fileMonitorMock.DidNotReceive().Dispose();

            // 2. Dispose
            testSubject.Dispose();
            fileMonitorMock.Received(1).Dispose();
        }

        [TestMethod]
        public void RealFile_DisableRule_FileDoesNotExist_FileCreated()
        {
            var dir = CreateTestSpecificDirectory();
            var settingsFile = Path.Combine(dir, "settings.txt");

            var testLogger = new TestLogger(logToConsole: true);
            var testSubject = CreateTestSubject(testLogger, new FileSystem(), new SingleFileMonitor(settingsFile, testLogger));

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
                Rules = new System.Collections.Generic.Dictionary<string, RuleConfig>
                {
                    { "javascript:S111", new RuleConfig { Level = RuleLevel.On } },
                    { "cpp:S111", new RuleConfig { Level = RuleLevel.On } },
                    { "xxx:S222", new RuleConfig { Level = RuleLevel.On } }
                }
            };

            SaveSettings(settingsFile, initialSettings);

            var testLogger = new TestLogger(logToConsole: true);
            var testSubject = CreateTestSubject(testLogger, new FileSystem(), new SingleFileMonitor(settingsFile, testLogger));

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
        public void SettingsChangeNotificationIsRaised()
        {
            int pause = System.Diagnostics.Debugger.IsAttached ? 20000 : 300;

            const string fileName = "mySettings.xxx";

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
            var fileSystemMock = CreateMockFile(fileName, originalData);

            var singleFileMonitorMock = CreateMockFileMonitor(fileName);
            int eventCount = 0;
            var settingsChangedEventReceived = new ManualResetEvent(initialState: false);

            var testSubject = CreateTestSubject(new TestLogger(), fileSystemMock, singleFileMonitorMock);
            testSubject.UserSettings.RulesSettings.Rules.Count.Should().Be(0); // sanity check of setup

            testSubject.SettingsChanged += (s, args) =>
                {
                    eventCount++;
                    settingsChangedEventReceived.Set();
                };

            // 1. Disable a rule
            // Should trigger a save, but should not *directly* raise a "SettingsChanged" event
            testSubject.DisableRule("dummyRule");

            // Timing - unfortunately, we can't reliably test for the absence of an event. We
            // can only wait for a certain amount of time and check no events arrive in that period.
            System.Threading.Thread.Sleep(pause);
            eventCount.Should().Be(0);

            // 2. Now simulate a file-change event
            fileSystemMock.File.ReadAllText(fileName).Returns(modifiedData);
            singleFileMonitorMock.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
            settingsChangedEventReceived.WaitOne(pause);

            // Check the settings change event was raised
            eventCount.Should().Be(1);

            // Check the data was actually reloaded from the file
            testSubject.UserSettings.RulesSettings.Rules.Count.Should().Be(1);
            testSubject.UserSettings.RulesSettings.Rules["typescript:S2685"].Level.Should().Be(RuleLevel.On);
        }

        [TestMethod]
        public void FileChanged_CallsUpdateStandaloneRulesConfiguration()
        {
            var mockSlCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
            var mockRulesSlCoreService = MockRulesSlCoreService(mockSlCoreServiceProvider);

            SetupFileChangedInUserSettingsProvider(mockSlCoreServiceProvider);

            mockRulesSlCoreService.Received(1).UpdateStandaloneRulesConfiguration(Arg.Any<UpdateStandaloneRulesConfigurationParams>());
        }

        [TestMethod]
        public void FileChanged_UpdatingStandaloneRulesConfigurationInSlCoreFails_WritesLog()
        {
            var mockSlCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
            var mockLogger = Substitute.For<ILogger>();
            var mockRulesSlCoreService = MockRulesSlCoreService(mockSlCoreServiceProvider);
            mockRulesSlCoreService.When(x => x.UpdateStandaloneRulesConfiguration(Arg.Any<UpdateStandaloneRulesConfigurationParams>()))
                .Do(x => throw new Exception("update failed"));

            SetupFileChangedInUserSettingsProvider(mockSlCoreServiceProvider, mockLogger);

            mockLogger.Received(1).WriteLine(Arg.Is<string>(msg => msg.Contains("update failed")));
        }

        private string CreateTestSpecificDirectory()
        {
            var dir = Path.Combine(TestContext.DeploymentDirectory, TestContext.TestName);
            Directory.CreateDirectory(dir);
            return dir;
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

        private static ISingleFileMonitor CreateMockFileMonitor(string filePathToMonitor)
        {
            var mockSettingsFileMonitor = Substitute.For<ISingleFileMonitor>();
            mockSettingsFileMonitor.MonitoredFilePath.Returns(filePathToMonitor);
            return mockSettingsFileMonitor;
        }

        private static IFileSystem CreateMockFile(string filePath, string contents)
        {
            var mockFile = Substitute.For<IFileSystem>();
            mockFile.File.Exists(filePath).Returns(true);
            mockFile.File.ReadAllText(filePath).Returns(contents);
            return mockFile;
        }

        private static void CheckSettingsAreEmpty(UserSettings settings)
        {
            settings.Should().NotBeNull();
            settings.RulesSettings.Should().NotBeNull();
            settings.RulesSettings.Rules.Should().NotBeNull();
            settings.RulesSettings.Rules.Count.Should().Be(0);
        }

        private static UserSettingsProvider CreateTestSubject(ILogger logger = null, IFileSystem fileSystem = null, 
            ISingleFileMonitor singleFileMonitor = null, ISLCoreServiceProvider slCoreService = null)
        {
            logger ??= Substitute.For<ILogger>();
            fileSystem ??= Substitute.For<IFileSystem>();
            singleFileMonitor ??= Substitute.For<ISingleFileMonitor>();
            slCoreService ??= Substitute.For<ISLCoreServiceProvider>();

            return CreateUserSettingsProvider(logger, fileSystem, singleFileMonitor, slCoreService);
        }

        private static UserSettingsProvider CreateUserSettingsProvider(ILogger logger, IFileSystem fileSystem, 
            ISingleFileMonitor singleFileMonitor, ISLCoreServiceProvider slCoreService)
        {
            return new UserSettingsProvider(logger, fileSystem, singleFileMonitor, slCoreService);
        }

        private static IRulesSLCoreService MockRulesSlCoreService(ISLCoreServiceProvider slCoreServiceProvider)
        {
            var mockRulesSlCoreService = Substitute.For<IRulesSLCoreService>();
            slCoreServiceProvider.TryGetTransientService(out Arg.Any<IRulesSLCoreService>()).Returns(callInfo =>
            {
                callInfo[0] = mockRulesSlCoreService;
                return true;
            });


            return mockRulesSlCoreService;
        }

        private static void SetupFileChangedInUserSettingsProvider(ISLCoreServiceProvider slCoreServiceProvider, ILogger logger = null)
        {
            var fileMonitorMock = Substitute.For<ISingleFileMonitor>();
            CreateUserSettingsProvider(
                logger ?? Substitute.For<ILogger>(),
                CreateMockFile("dummyPath", "dummyContent"),
                fileMonitorMock, 
                slCoreServiceProvider);

            fileMonitorMock.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
        }
    }
}

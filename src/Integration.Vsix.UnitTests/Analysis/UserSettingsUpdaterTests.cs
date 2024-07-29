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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.SLCore.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class UserSettingsUpdaterTests
{
    private ISingleFileMonitor settingsFileMonitor;
    private ISingleFileMonitorFactory singleFileMonitorFactory;
    private ISLCoreRuleSettings slCoreRuleSettings;
    private IUserSettingsProvider userSettingsProvider;
    private UserSettingsUpdater testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        settingsFileMonitor = Substitute.For<ISingleFileMonitor>();
        singleFileMonitorFactory = Substitute.For<ISingleFileMonitorFactory>();
        slCoreRuleSettings ??= Substitute.For<ISLCoreRuleSettings>();
        userSettingsProvider ??= Substitute.For<IUserSettingsProvider>();

        singleFileMonitorFactory.Create(Arg.Any<string>()).Returns(settingsFileMonitor);
        testSubject = CreateUserSettingsUpdater(singleFileMonitorFactory, slCoreRuleSettings, userSettingsProvider);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<UserSettingsUpdater, IUserSettingsUpdater>(
            MefTestHelpers.CreateExport<ISLCoreRuleSettings>(),
            MefTestHelpers.CreateExport<IUserSettingsProvider>(),
            MefTestHelpers.CreateExport<ISingleFileMonitorFactory>(singleFileMonitorFactory));
    }

    [TestMethod]
    public void Ctor_DoesNotCallAnyNonFreeThreadedServices()
    {
        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls.
        settingsFileMonitor.ReceivedCalls().Count().Should().Be(1);
        settingsFileMonitor.Received(1).FileChanged += Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void Ctor_NullArguments()
    {
        Action act = () => CreateUserSettingsUpdater(singleFileMonitorFactory, slCoreRuleSettings, userSettingsProvider);
        act.Should().NotThrow<ArgumentNullException>();

        act = () => CreateUserSettingsUpdater(null, slCoreRuleSettings, userSettingsProvider);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be(nameof(singleFileMonitorFactory));

        act = () => CreateUserSettingsUpdater(singleFileMonitorFactory, slCoreRuleSettings, null);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be(nameof(userSettingsProvider));

        act = () => CreateUserSettingsUpdater(singleFileMonitorFactory, null, userSettingsProvider);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be(nameof(slCoreRuleSettings));
    }

    [TestMethod]
    public void FileChanges_EventsRaised()
    {
        MockPath("settings.file");
        int settingsChangedEventCount = 0;

        testSubject.SettingsChanged += (s, args) => settingsChangedEventCount++;

        settingsFileMonitor.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
        settingsChangedEventCount.Should().Be(1);

        // 2. Simulate another event when the file is valid - valid settings should be returned
        settingsFileMonitor.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
        settingsChangedEventCount.Should().Be(2);
    }

    [TestMethod]
    public void ConstructAndDispose()
    {
        // Arrange
        const string fileName = "c:\\aaa\\bbb\\file.txt";
        MockPath(fileName);

        // 1. Construct
        settingsFileMonitor.DidNotReceive().Dispose();

        // 2. Dispose
        testSubject.Dispose();
        settingsFileMonitor.Received(1).Dispose();
    }

    [TestMethod]
    public void SettingsChangeNotificationIsRaised()
    {
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
        var settingsProvider = new UserSettingsProvider(Substitute.For<ILogger>(), fileSystemMock, fileName);
        MockPath(fileName);
        int eventCount = 0;
        var settingsChangedEventReceived = new ManualResetEvent(initialState: false);

        var userSettingsUpdater = CreateUserSettingsUpdater(singleFileMonitorFactory, slCoreRuleSettings, settingsProvider);
        settingsProvider.UserSettings.RulesSettings.Rules.Count.Should().Be(0); // sanity check of setup

        userSettingsUpdater.SettingsChanged += (s, args) =>
        {
            eventCount++;
            settingsChangedEventReceived.Set();
        };

        // 1. Disable a rule
        // Should trigger a save, but should not *directly* raise a "SettingsChanged" event
        settingsProvider.DisableRule("dummyRule");
        eventCount.Should().Be(0);

        // 2. Now simulate a file-change event
        fileSystemMock.File.ReadAllText(fileName).Returns(modifiedData);
        settingsFileMonitor.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

        // Check the settings change event was raised
        eventCount.Should().Be(1);

        // Check the data was actually reloaded from the file
        settingsProvider.UserSettings.RulesSettings.Rules.Count.Should().Be(1);
        settingsProvider.UserSettings.RulesSettings.Rules["typescript:S2685"].Level.Should().Be(RuleLevel.On);
    }

    [TestMethod]
    public void FileChanged_CallsUpdateStandaloneRulesConfiguration()
    {
        SetupFileChangedInUserSettingsUpdater();

        slCoreRuleSettings.Received(1).UpdateStandaloneRulesConfiguration();
    }

    private void MockPath(string filePathToMonitor)
    {
        settingsFileMonitor.MonitoredFilePath.Returns(filePathToMonitor);
    }

    private static IFileSystem CreateMockFile(string filePath, string contents)
    {
        var mockFile = Substitute.For<IFileSystem>();
        mockFile.File.Exists(filePath).Returns(true);
        mockFile.File.ReadAllText(filePath).Returns(contents);
        return mockFile;
    }

    private static UserSettingsUpdater CreateUserSettingsUpdater(ISingleFileMonitorFactory singleFileMonitorFactory, ISLCoreRuleSettings slCoreRuleSettings, IUserSettingsProvider userSettingsProvider)
    {
        return new UserSettingsUpdater(singleFileMonitorFactory, slCoreRuleSettings, userSettingsProvider);
    }

    private void SetupFileChangedInUserSettingsUpdater()
    {
        userSettingsProvider.UserSettings.Returns(new UserSettings(new RulesSettings()));
        settingsFileMonitor.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
    }
}

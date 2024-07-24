﻿/*
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
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.SLCore.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class UserSettingsUpdaterTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<UserSettingsUpdater, IUserSettingsUpdater>(
            MefTestHelpers.CreateExport<ILogger>(), 
            MefTestHelpers.CreateExport<ISLCoreRuleSettings>(),
            MefTestHelpers.CreateExport<IUserSettingsProvider>());
    }

    [TestMethod]
    public void Ctor_DoesNotCallAnyNonFreeThreadedServices()
    {
        // Arrange
        var fileMonitorMock = Substitute.For<ISingleFileMonitor>();

        // Act
        _ = CreateTestSubject(fileMonitorMock);

        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls.
        fileMonitorMock.ReceivedCalls().Count().Should().Be(1);
        fileMonitorMock.Received(1).FileChanged += Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void Ctor_NullArguments()
    {
        var settingsFileMonitor = Substitute.For<ISingleFileMonitor>();
        var slCoreRuleSettings = Substitute.For<ISLCoreRuleSettings>();
        var userSettingsProvider = Substitute.For<IUserSettingsProvider>();

        Action act = () => CreateUserSettingsUpdater(settingsFileMonitor, slCoreRuleSettings, userSettingsProvider);
        act.Should().NotThrow<ArgumentNullException>();

        act = () => CreateUserSettingsUpdater(null, slCoreRuleSettings, userSettingsProvider);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be(nameof(settingsFileMonitor));

        act = () => CreateUserSettingsUpdater(settingsFileMonitor, slCoreRuleSettings, null);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be(nameof(userSettingsProvider));

        act = () => CreateUserSettingsUpdater(settingsFileMonitor, null, userSettingsProvider);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be(nameof(slCoreRuleSettings));
    }

    [TestMethod]
    public void FileChanges_EventsRaised()
    {
        var fileMonitorMock = CreateMockFileMonitor("settings.file");
        int settingsChangedEventCount = 0;
        var testSubject = CreateTestSubject(fileMonitorMock);

        testSubject.SettingsChanged += (s, args) => settingsChangedEventCount++;

        fileMonitorMock.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
        settingsChangedEventCount.Should().Be(1);

        // 2. Simulate another event when the file is valid - valid settings should be returned
        fileMonitorMock.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
        settingsChangedEventCount.Should().Be(2);
    }

    [TestMethod]
    public void ConstructAndDispose()
    {
        // Arrange
        const string fileName = "c:\\aaa\\bbb\\file.txt";
        var fileMonitorMock = CreateMockFileMonitor(fileName);

        // 1. Construct
        var testSubject = CreateTestSubject(singleFileMonitor:fileMonitorMock);
        fileMonitorMock.DidNotReceive().Dispose();

        // 2. Dispose
        testSubject.Dispose();
        fileMonitorMock.Received(1).Dispose();
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
        var userSettingsProvider = new UserSettingsProvider(Substitute.For<ILogger>(), fileSystemMock, fileName);
        var singleFileMonitorMock = CreateMockFileMonitor(fileName);
        int eventCount = 0;
        var settingsChangedEventReceived = new ManualResetEvent(initialState: false);

        var testSubject = CreateTestSubject(singleFileMonitorMock, userSettingsProvider: userSettingsProvider);
        userSettingsProvider.UserSettings.RulesSettings.Rules.Count.Should().Be(0); // sanity check of setup

        testSubject.SettingsChanged += (s, args) =>
        {
            eventCount++;
            settingsChangedEventReceived.Set();
        };

        // 1. Disable a rule
        // Should trigger a save, but should not *directly* raise a "SettingsChanged" event
        userSettingsProvider.DisableRule("dummyRule");

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
        userSettingsProvider.UserSettings.RulesSettings.Rules.Count.Should().Be(1);
        userSettingsProvider.UserSettings.RulesSettings.Rules["typescript:S2685"].Level.Should().Be(RuleLevel.On);
    }

    [TestMethod]
    public void FileChanged_CallsUpdateStandaloneRulesConfiguration()
    {
        var slCoreRuleSettings = Substitute.For<ISLCoreRuleSettings>();

        SetupFileChangedInUserSettingsUpdater(slCoreRuleSettings:slCoreRuleSettings);

        slCoreRuleSettings.Received(1).UpdateStandaloneRulesConfiguration();
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

    private static UserSettingsUpdater CreateTestSubject(
        ISingleFileMonitor singleFileMonitor = null, 
        ISLCoreRuleSettings islCoreRuleSettings = null, 
        IUserSettingsProvider userSettingsProvider = null)
    {
        singleFileMonitor ??= Substitute.For<ISingleFileMonitor>();
        islCoreRuleSettings ??= Substitute.For<ISLCoreRuleSettings>();
        userSettingsProvider ??= Substitute.For<IUserSettingsProvider>();

        return CreateUserSettingsUpdater(singleFileMonitor, islCoreRuleSettings, userSettingsProvider);
    }

    private static UserSettingsUpdater CreateUserSettingsUpdater(ISingleFileMonitor singleFileMonitor, ISLCoreRuleSettings slCoreRuleSettings, IUserSettingsProvider userSettingsProvider)
    {
        return new UserSettingsUpdater(singleFileMonitor, slCoreRuleSettings, userSettingsProvider);
    }

    private static void SetupFileChangedInUserSettingsUpdater(ISLCoreRuleSettings slCoreRuleSettings = null, IUserSettingsProvider userSettingsProvider = null)
    {
        var fileMonitorMock = Substitute.For<ISingleFileMonitor>();
        userSettingsProvider ??= Substitute.For<IUserSettingsProvider>();
        userSettingsProvider.UserSettings.Returns(new UserSettings(new RulesSettings()));

      CreateUserSettingsUpdater(
            fileMonitorMock, 
            slCoreRuleSettings ?? Substitute.For<ISLCoreRuleSettings>(),
            userSettingsProvider);
      fileMonitorMock.FileChanged += Raise.EventWith(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
    }
}

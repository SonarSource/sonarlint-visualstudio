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

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void Initialize()
    {
        testLogger = new TestLogger();
        fileSystem = Substitute.For<IFileSystem>();

        userSettingsProvider = CreateUserSettingsProvider(testLogger, fileSystem);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<UserSettingsProvider, IUserSettingsProvider>(MefTestHelpers.CreateExport<ILogger>());
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
        Action act = () => CreateUserSettingsProvider(null, fileSystem);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

        act = () => CreateUserSettingsProvider(testLogger, null);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
    }

    [TestMethod]
    public void Ctor_ErrorLoadingSettings_ErrorSquashed_AndEmptySettingsReturned()
    {
        // Arrange
        fileSystem.File.Exists(SettingsFilePath).Returns(true);
        fileSystem.File.ReadAllText(SettingsFilePath).Throws(new InvalidOperationException("custom error message"));

        CreateUserSettingsProvider(testLogger, fileSystem);

        // Assert
        CheckSettingsAreEmpty(userSettingsProvider.UserSettings);
        testLogger.AssertPartialOutputStringExists("custom error message");
    }

    [TestMethod]
    public void Ctor_DoesNotCallAnyNonFreeThreadedServices()
    {
        var logger = Substitute.For<ILogger>();

        CreateUserSettingsProvider(logger, fileSystem);

        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls.
        logger.ReceivedCalls().Should().BeEmpty();
        fileSystem.ReceivedCalls().Should().BeEmpty();
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
        var testSubject = CreateUserSettingsProvider(logger, new FileSystem(), settingsFile);

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
        var testSubject = CreateUserSettingsProvider(logger, new FileSystem(), settingsFile);

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
        var testSubject = CreateUserSettingsProvider(testLogger, new FileSystem(), settingsFile);
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

    private UserSettingsProvider CreateUserSettingsProvider(ILogger logger, IFileSystem fileSystem, string settings = null)
    {
        settings ??= SettingsFilePath;

        return new UserSettingsProvider(logger, fileSystem, settings);    
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
}

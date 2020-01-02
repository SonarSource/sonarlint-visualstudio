/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class UserSettingsSerializerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void RealFile_Load_CheckRulesAndParametersDictionarySerialization()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true);

            var dir = CreateTestSpecificDirectory();

            var filePath1 = Path.Combine(dir, "settings.json");

            var validSettingsData = @"{
    'sonarlint.rules': {
        'xxx:xxx': {
            'Level': 'On',
            'Parameters': {
              'key1': 'value1'
            }
        },
        'xxx:yyy': {
            'level': 'off',
            'parameters': null
        },
        'xxx:zzz': {
            'level': 'off'
        }
    }
}";
            File.WriteAllText(filePath1, validSettingsData);

            var testSubject = new UserSettingsSerializer(new FileWrapper(), testLogger);

            // 1. Load from disc
            var loadedSettings = testSubject.SafeLoad(filePath1);
            loadedSettings.Should().NotBeNull();
            loadedSettings.Rules.Should().NotBeNull();
            loadedSettings.Rules.Count.Should().Be(3);

            // Check loaded data

            // 1. Rules dictionary: lookup should be case-insensitive
            loadedSettings.Rules.TryGetValue("xxx:xxx", out var rulesConfig).Should().BeTrue();
            loadedSettings.Rules.TryGetValue("XXX:XXX", out rulesConfig).Should().BeTrue();

            // 2. Parameters dictionary: lookup should be case-insensitive
            rulesConfig.Parameters.TryGetValue("key1", out var paramValue).Should().BeTrue();
            // BUG - we want it to be case-insenstive, but it isn't
            // rulesConfig.Parameters.TryGetValue("KEY1", out paramValue).Should().BeTrue();
            paramValue.Should().Be("value1");

            // 3. Parameters dictionary: explicitly null in file -> null on deserialization
            loadedSettings.Rules["xxx:yyy"].Parameters.Should().BeNull();

            // 4. Parameters dictionary: missing parameters setting in dictionary -> null on deserialization
            loadedSettings.Rules["xxx:zzz"].Parameters.Should().BeNull();
        }

        [TestMethod]
        public void RealFile_RoundTripLoadAndSave_WithFirstCapsPropertyNames()
        {
            // Note: the JSON serializer is configured to save property names
            // to lower-case by default i.e. "level", "severity" etc.
            // However, it should tolerate loading files with first-caps names
            // e.g. "Level", "Parameters" etc.
            // This snippet uses upper-case "Level", "Severity" and "Parameters"
            // to check they are processed correctly.

            // Arrange
            var testLogger = new TestLogger(logToConsole: true);

            var dir = CreateTestSpecificDirectory();

            var filePath1 = Path.Combine(dir, "settings.json");
            var filePath2 = Path.Combine(dir, "settings.json.txt");

            var validSettingsData = @"{
    'UnknownData' : 'will be dropped on save',

    'sonarlint.rules': {
        'typescript:S2685': {
            'Level': 'On',
            'Parameters': {
              'key1': 'value1'
            },
            'Severity': 'Critical' 
        },
        'xxx:yyy': {
            'level': 'off',
            'parameters': {
              'key2': 'value2',
              'key3': 'value3'
            },
            'severity': 'Blocker'
        }
    },

    'More UnknownData' : 'will also be dropped on save',
}";
            File.WriteAllText(filePath1, validSettingsData);

            var testSubject = new UserSettingsSerializer(new FileWrapper(), testLogger);

            // 1. Load from disc
            var loadedSettings = testSubject.SafeLoad(filePath1);
            loadedSettings.Should().NotBeNull();
            loadedSettings.Rules.Should().NotBeNull();
            loadedSettings.Rules.Count.Should().Be(2);

            // Check loaded data
            loadedSettings.Rules["typescript:S2685"].Level.Should().Be(RuleLevel.On);
            loadedSettings.Rules["xxx:yyy"].Level.Should().Be(RuleLevel.Off);

            loadedSettings.Rules["typescript:S2685"].Parameters.Should().NotBeNull();
            loadedSettings.Rules["typescript:S2685"].Parameters["key1"].Should().Be("value1");

            loadedSettings.Rules["xxx:yyy"].Parameters.Should().NotBeNull();
            loadedSettings.Rules["xxx:yyy"].Parameters["key2"].Should().Be("value2");
            loadedSettings.Rules["xxx:yyy"].Parameters["key3"].Should().Be("value3");

            loadedSettings.Rules["typescript:S2685"].Severity.Should().Be(IssueSeverity.Critical);
            loadedSettings.Rules["xxx:yyy"].Severity.Should().Be(IssueSeverity.Blocker);

            // 2. Save and reload
            testSubject.SafeSave(filePath2, loadedSettings);
            File.Exists(filePath2).Should().BeTrue();
            var reloadedSettings = testSubject.SafeLoad(filePath2);

            TestContext.AddResultFile(filePath2);

            reloadedSettings.Should().NotBeNull();
            reloadedSettings.Rules.Should().NotBeNull();
            reloadedSettings.Rules.Count.Should().Be(2);

            // Check loaded data
            reloadedSettings.Rules["typescript:S2685"].Level.Should().Be(RuleLevel.On);
            reloadedSettings.Rules["xxx:yyy"].Level.Should().Be(RuleLevel.Off);

            loadedSettings.Rules["typescript:S2685"].Parameters.Should().NotBeNull();
            loadedSettings.Rules["typescript:S2685"].Parameters["key1"].Should().Be("value1");

            loadedSettings.Rules["xxx:yyy"].Parameters.Should().NotBeNull();
            loadedSettings.Rules["xxx:yyy"].Parameters["key2"].Should().Be("value2");
            loadedSettings.Rules["xxx:yyy"].Parameters["key3"].Should().Be("value3");
        }

        [TestMethod]
        public void RealFile_RoundTripSaveAndLoad()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true);

            var dir = CreateTestSpecificDirectory();
            var filePath = Path.Combine(dir, "settings.txt");

            var settings = new UserSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    { "repo1:key1", new RuleConfig { Level = RuleLevel.Off, Severity = IssueSeverity.Major } },
                    { "repo1:key2", new RuleConfig { Level = RuleLevel.On, Severity = IssueSeverity.Blocker } },
                    { "repox:keyy",
                        new RuleConfig
                        {
                            Level = RuleLevel.On,
                            Parameters = new Dictionary<string, string>
                            {
                                { "key1", "value1" },
                                { "key2", "value2" }
                            }
                        }
                    }
                }
            };

            var testSubject = new UserSettingsSerializer(new FileWrapper(), testLogger);

            // Act: save and reload
            testSubject.SafeSave(filePath, settings);
            File.Exists(filePath).Should().BeTrue();

            var reloadedSettings = testSubject.SafeLoad(filePath);

            TestContext.AddResultFile(filePath);

            reloadedSettings.Should().NotBeNull();
            reloadedSettings.Rules.Should().NotBeNull();
            reloadedSettings.Rules.Count.Should().Be(3);

            // Check loaded data
            reloadedSettings.Rules["repo1:key1"].Level.Should().Be(RuleLevel.Off);
            reloadedSettings.Rules["repo1:key2"].Level.Should().Be(RuleLevel.On);
            reloadedSettings.Rules["repox:keyy"].Level.Should().Be(RuleLevel.On);

            reloadedSettings.Rules["repo1:key1"].Severity.Should().Be(IssueSeverity.Major);
            reloadedSettings.Rules["repo1:key2"].Severity.Should().Be(IssueSeverity.Blocker);
            reloadedSettings.Rules["repox:keyy"].Severity.Should().BeNull();

            reloadedSettings.Rules["repo1:key1"].Parameters.Should().BeNull();
            reloadedSettings.Rules["repo1:key2"].Parameters.Should().BeNull();

            var rulexParams = reloadedSettings.Rules["repox:keyy"].Parameters;
            rulexParams.Should().NotBeNull();

            rulexParams.Keys.Should().BeEquivalentTo("key1", "key2");
            rulexParams["key1"].Should().Be("value1");
            rulexParams["key2"].Should().Be("value2");
        }

        [TestMethod]
        public void Load_MissingFile_NullReturned()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("settings.file")).Returns(false);

            var logger = new TestLogger(logToConsole: true);

            var testSubject = new UserSettingsSerializer(fileMock.Object, logger);

            // Act
            var result = testSubject.SafeLoad("settings.file");

            // Assert
            result.Should().BeNull();
            fileMock.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Load_NonCriticalError_IsSquashed_AndNullReturned()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("settings.file")).Returns(true);
            fileMock.Setup(x => x.ReadAllText("settings.file")).Throws(new System.InvalidOperationException("custom error message"));

            var logger = new TestLogger(logToConsole: true);
            var testSubject = new UserSettingsSerializer(fileMock.Object, logger);

            // Act
            var result = testSubject.SafeLoad("settings.file");

            // Assert
            result.Should().BeNull();
            logger.AssertPartialOutputStringExists("custom error message");
        }

        [TestMethod]
        public void Load_CriticalError_IsNotSquashed()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("settings.file")).Returns(true);
            fileMock.Setup(x => x.ReadAllText("settings.file")).Throws(new System.StackOverflowException("critical custom error message"));

            var logger = new TestLogger(logToConsole: true);
            var testSubject = new UserSettingsSerializer(fileMock.Object, logger);

            // Act
            Action act = () => testSubject.SafeLoad("settings.file");

            // Assert
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("critical custom error message");
            logger.AssertPartialOutputStringDoesNotExist("critical custom error message");
        }

        [TestMethod]
        public void Save_NonCriticalError_IsSquashed()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.WriteAllText("settings.file", It.IsAny<string>())).Throws(new System.InvalidOperationException("custom error message"));

            var logger = new TestLogger(logToConsole: true);
            var testSubject = new UserSettingsSerializer(fileMock.Object, logger);

            // Act - should not throw
            testSubject.SafeSave("settings.file", new UserSettings());

            // Assert
            logger.AssertPartialOutputStringExists("settings.file", "custom error message");
        }

        [TestMethod]
        public void Save_CriticalError_IsNotSquashed()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.WriteAllText("settings.file", It.IsAny<string>())).Throws(new System.StackOverflowException("critical custom error message"));

            var logger = new TestLogger(logToConsole: true);
            var testSubject = new UserSettingsSerializer(fileMock.Object, logger);

            // Act
            Action act = () => testSubject.SafeSave("settings.file", new UserSettings());

            // Assert
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("critical custom error message");
            logger.AssertPartialOutputStringDoesNotExist("critical custom error message");
        }

        private string CreateTestSpecificDirectory()
        {
            var dir = Path.Combine(TestContext.DeploymentDirectory, TestContext.TestName);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}

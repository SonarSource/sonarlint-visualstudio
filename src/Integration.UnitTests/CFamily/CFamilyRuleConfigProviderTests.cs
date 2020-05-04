/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Collections.Generic;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyRuleConfigProviderTests
    {
        [TestMethod]
        public void Get_StandaloneMode()
        {
            // Arrange
            var builder = new TestEnvironmentBuilder(SonarLintMode.Standalone)
            {
                ConnectedModeSettings = new UserSettings(new RulesSettings
                {
                    Rules = new Dictionary<string, RuleConfig>
                    {
                        {  "cpp:rule1", new RuleConfig { Level = RuleLevel.Off } },
                        {  "cpp:rule2", new RuleConfig { Level = RuleLevel.On } },
                        {  "cpp:rule3", new RuleConfig { Level = RuleLevel.On } },
                        {  "XXX:rule3", new RuleConfig { Level = RuleLevel.On } }
                    }
                }),

                StandaloneModeSettings = new UserSettings(new RulesSettings
                {
                    Rules = new Dictionary<string, RuleConfig>
                    {
                        {  "cpp:rule1", new RuleConfig { Level = RuleLevel.On } },
                        {  "cpp:rule2", new RuleConfig { Level = RuleLevel.Off } },
                        {  "cpp:rule4", new RuleConfig { Level = RuleLevel.On } },
                        {  "XXX:rule3", new RuleConfig { Level = RuleLevel.On } }
                    }
                }),

                SonarWayConfig = new DummyCFamilyRulesConfig("cpp")
                    .AddRule("rule1", IssueSeverity.Blocker, isActive: false)
                    .AddRule("rule2", IssueSeverity.Critical, isActive: false)
                    .AddRule("rule3", IssueSeverity.Major, isActive: true)
                    .AddRule("rule4", IssueSeverity.Minor, isActive: false)
            };

            var testSubject = builder.CreateTestSubject();

            // Act
            var result = testSubject.GetRulesConfiguration("cpp");

            // Assert
            result.ActivePartialRuleKeys.Should().BeEquivalentTo("rule1", "rule3", "rule4");
            result.AllPartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2", "rule3", "rule4");

            builder.AssertConnectedSettingsNotAccessed();
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void Get_ConnectedMode(SonarLintMode mode)
        {
            // Arrange
            var builder = new TestEnvironmentBuilder(mode)
            {
                ConnectedModeSettings = new UserSettings(new RulesSettings
                {
                    Rules = new Dictionary<string, RuleConfig>
                    {
                        { "cpp:rule1", new RuleConfig { Level = RuleLevel.Off, Severity = null } },
                        { "cpp:rule2", new RuleConfig { Level = RuleLevel.On, Severity = IssueSeverity.Blocker } },
                        { "cpp:rule3", new RuleConfig { Level = RuleLevel.On, Severity = IssueSeverity.Critical } },
                        { "XXX:rule4", new RuleConfig { Level = RuleLevel.On, Severity = IssueSeverity.Info } }
                    }
                }),

                StandaloneModeSettings = new UserSettings(new RulesSettings
                {
                    Rules = new Dictionary<string, RuleConfig>
                    {
                        { "cpp:rule1", new RuleConfig { Level = RuleLevel.On } },
                        { "cpp:rule2", new RuleConfig { Level = RuleLevel.Off } },
                        { "cpp:rule4", new RuleConfig { Level = RuleLevel.On } },
                        { "XXX:rule4", new RuleConfig { Level = RuleLevel.On } }
                    }
                }),

                SonarWayConfig = new DummyCFamilyRulesConfig("cpp")
                    .AddRule("rule1", IssueSeverity.Info, isActive: false)
                    .AddRule("rule2", IssueSeverity.Major, isActive: false)
                    .AddRule("rule3", IssueSeverity.Minor, isActive: true)
                    .AddRule("rule4", IssueSeverity.Blocker, isActive: false)
            };

            var testSubject = builder.CreateTestSubject();

            // Act
            var result = testSubject.GetRulesConfiguration("cpp");

            // Assert
            result.ActivePartialRuleKeys.Should().BeEquivalentTo("rule2", "rule3");
            result.AllPartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2", "rule3", "rule4");

            result.RulesMetadata["rule1"].DefaultSeverity.Should().Be(IssueSeverity.Info);     // not set in ConnectedModeSettings so should use default
            result.RulesMetadata["rule2"].DefaultSeverity.Should().Be(IssueSeverity.Blocker);  // ConnectedModeSetting should override the default
            result.RulesMetadata["rule3"].DefaultSeverity.Should().Be(IssueSeverity.Critical); // ConnectedModeSetting should override the default
            result.RulesMetadata["rule4"].DefaultSeverity.Should().Be(IssueSeverity.Blocker); // ConnectedModeSetting should override the default

            builder.AssertStandaloneSettingsNotAccessed();
            builder.Logger.AssertOutputStringExists(Resources.Strings.CFamily_UsingConnectedModeSettings);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void Get_ConnectedMode_MissingSettings_StandaloneModeSettingsUsed(SonarLintMode mode)
        {
            // Arrange
            var builder = new TestEnvironmentBuilder(mode)
            {
                ConnectedSettingsFileExists = false,

                StandaloneModeSettings = new UserSettings(new RulesSettings
                {
                    Rules = new Dictionary<string, RuleConfig>
                    {
                        {  "cpp:rule1", new RuleConfig { Level = RuleLevel.On } },
                        {  "cpp:rule2", new RuleConfig { Level = RuleLevel.Off } },
                        {  "cpp:rule4", new RuleConfig { Level = RuleLevel.On } },
                        {  "XXX:rule3", new RuleConfig { Level = RuleLevel.On } }
                    }
                }),

                SonarWayConfig = new DummyCFamilyRulesConfig("cpp")
                    .AddRule("rule1", IssueSeverity.Info, isActive: false)
                    .AddRule("rule2", IssueSeverity.Major, isActive: false)
                    .AddRule("rule3", IssueSeverity.Minor, isActive: true)
                    .AddRule("rule4", IssueSeverity.Blocker, isActive: false)
            };

            var testSubject = builder.CreateTestSubject();

            // Act
            var result = testSubject.GetRulesConfiguration("cpp");

            // Assert
            result.ActivePartialRuleKeys.Should().BeEquivalentTo("rule1", "rule3", "rule4");
            result.AllPartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2", "rule3", "rule4");

            builder.Logger.AssertOutputStringExists(Resources.Strings.CFamily_UnableToLoadConnectedModeSettings);
        }

        private class TestEnvironmentBuilder
        {
            private readonly Mock<IUserSettingsProvider> standaloneSettingsProviderMock;
            private readonly Mock<ICFamilyRulesConfigProvider> sonarWayProviderMock;

            private readonly Mock<IFileSystem> fileSystemMock;
            private readonly Mock<ISolutionBindingFilePathGenerator> solutionBindingFilePathGeneratorMock;

            private readonly ConfigurableActiveSolutionBoundTracker activeSolutionBoundTracker;

            private readonly SonarLintMode bindingMode;

            public UserSettings StandaloneModeSettings { get; set; }
            public UserSettings ConnectedModeSettings { get; set; }
            public DummyCFamilyRulesConfig SonarWayConfig { get; set; }

            public bool ConnectedSettingsFileExists { get; set; } = true;

            public TestLogger Logger { get; }

            public TestEnvironmentBuilder(SonarLintMode mode)
            {
                standaloneSettingsProviderMock = new Mock<IUserSettingsProvider>();
                fileSystemMock = new Mock<IFileSystem>();
                Logger = new TestLogger();

                sonarWayProviderMock = new Mock<ICFamilyRulesConfigProvider>();
                activeSolutionBoundTracker = new ConfigurableActiveSolutionBoundTracker();

                solutionBindingFilePathGeneratorMock = new Mock<ISolutionBindingFilePathGenerator>();

                bindingMode = mode;
            }

            public CFamilyRuleConfigProvider CreateTestSubject()
            {
                // Data: set up the binding configuration
                var projectToReturn = new BoundSonarQubeProject(new System.Uri("http://localhost:9000"),
                    "sqProjectKey", "sqProjectName");
                activeSolutionBoundTracker.CurrentConfiguration = new BindingConfiguration(projectToReturn, bindingMode, null);

                // Data: user-configured settings
                standaloneSettingsProviderMock.Setup(x => x.UserSettings).Returns(StandaloneModeSettings);

                // Data: connected mode settings
                const string connectedSettingsFilesPath = "zzz\\foo.bar";
                var connectedSettingsData = JsonConvert.SerializeObject(ConnectedModeSettings?.RulesSettings, Formatting.Indented);

                solutionBindingFilePathGeneratorMock.Setup(x => x.Generate(null, "sqProjectKey", It.IsAny<string>()))
                    .Returns(connectedSettingsFilesPath);

                fileSystemMock.Setup(x => x.File.Exists(connectedSettingsFilesPath)).Returns(ConnectedSettingsFileExists);
                fileSystemMock.Setup(x => x.File.ReadAllText(connectedSettingsFilesPath))
                    .Returns(connectedSettingsData);

                // Data: SonarWay configuration
                sonarWayProviderMock.Setup(x => x.GetRulesConfiguration(It.IsAny<string>()))
                    .Returns(SonarWayConfig);

                var testSubject = new CFamilyRuleConfigProvider(activeSolutionBoundTracker, standaloneSettingsProviderMock.Object, Logger,
                    sonarWayProviderMock.Object, fileSystemMock.Object, solutionBindingFilePathGeneratorMock.Object);

                return testSubject;
            }

            public void AssertConnectedSettingsNotAccessed()
            {
                solutionBindingFilePathGeneratorMock.Verify(x => x.Generate(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>()), Times.Never);

                fileSystemMock.Verify(x => x.File.Exists(It.IsAny<string>()), Times.Never);
                fileSystemMock.Verify(x => x.File.ReadAllText(It.IsAny<string>()), Times.Never);
            }

            public void AssertStandaloneSettingsNotAccessed()
            {
                standaloneSettingsProviderMock.Verify(x => x.UserSettings, Times.Never);
            }
        }
    }
}

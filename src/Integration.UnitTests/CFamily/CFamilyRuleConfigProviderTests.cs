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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.CFamily;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyRuleConfigProviderTests
    {
        [TestMethod]
        public void Get_StandaloneMode()
        {
            // Arrange
            var builder = new TestEnvironmentBuilder(SonarLintMode.Standalone);

            builder.ConnectedModeSettings = new UserSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    {  "cpp:rule1", new RuleConfig { Level = RuleLevel.Off } },
                    {  "cpp:rule2", new RuleConfig { Level = RuleLevel.On } },
                    {  "cpp:rule3", new RuleConfig { Level = RuleLevel.On } },
                    {  "XXX:rule3", new RuleConfig { Level = RuleLevel.On } }
                }
            };

            builder.StandaloneModeSettings = new UserSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    {  "cpp:rule1", new RuleConfig { Level = RuleLevel.On } },
                    {  "cpp:rule2", new RuleConfig { Level = RuleLevel.Off } },
                    {  "cpp:rule4", new RuleConfig { Level = RuleLevel.On } },
                    {  "XXX:rule3", new RuleConfig { Level = RuleLevel.On } }
                }
            };

            builder.SonarWayConfig = new DummyCFamilyRulesConfig
            {
                LanguageKey = "cpp",
                RuleKeyToActiveMap = new Dictionary<string, bool>
                {
                    { "rule1", false },
                    { "rule2", false },
                    { "rule3", true },
                    { "rule4", false }
                }
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
            var builder = new TestEnvironmentBuilder(mode);

            builder.ConnectedModeSettings = new UserSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    {  "cpp:rule1", new RuleConfig { Level = RuleLevel.Off } },
                    {  "cpp:rule2", new RuleConfig { Level = RuleLevel.On } },
                    {  "cpp:rule3", new RuleConfig { Level = RuleLevel.On } },
                    {  "XXX:rule3", new RuleConfig { Level = RuleLevel.On } }
                }
            };

            builder.StandaloneModeSettings = new UserSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    {  "cpp:rule1", new RuleConfig { Level = RuleLevel.On } },
                    {  "cpp:rule2", new RuleConfig { Level = RuleLevel.Off } },
                    {  "cpp:rule4", new RuleConfig { Level = RuleLevel.On } },
                    {  "XXX:rule3", new RuleConfig { Level = RuleLevel.On } }
                }
            };

            builder.SonarWayConfig = new DummyCFamilyRulesConfig
            {
                LanguageKey = "cpp",
                RuleKeyToActiveMap = new Dictionary<string, bool>
                {
                    { "rule1", false },
                    { "rule2", false },
                    { "rule3", true },
                    { "rule4", false }
                }
            };

            var testSubject = builder.CreateTestSubject();

            // Act
            var result = testSubject.GetRulesConfiguration("cpp");

            // Assert
            result.ActivePartialRuleKeys.Should().BeEquivalentTo("rule2", "rule3");
            result.AllPartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2", "rule3", "rule4");

            builder.AssertStandaloneSettingsNotAccessed();
            builder.Logger.AssertOutputStringExists(Resources.Strings.CFamily_UsingConnectedModeSettings);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void Get_ConnectedMode_MissingSettings_StandaloneModeSettingsUsed(SonarLintMode mode)
        {
            // Arrange
            var builder = new TestEnvironmentBuilder(mode);

            builder.ConnectedSettingsFileExists = false;

            builder.StandaloneModeSettings = new UserSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    {  "cpp:rule1", new RuleConfig { Level = RuleLevel.On } },
                    {  "cpp:rule2", new RuleConfig { Level = RuleLevel.Off } },
                    {  "cpp:rule4", new RuleConfig { Level = RuleLevel.On } },
                    {  "XXX:rule3", new RuleConfig { Level = RuleLevel.On } }
                }
            };

            builder.SonarWayConfig = new DummyCFamilyRulesConfig
            {
                LanguageKey = "cpp",
                RuleKeyToActiveMap = new Dictionary<string, bool>
                {
                    { "rule1", false },
                    { "rule2", false },
                    { "rule3", true },
                    { "rule4", false }
                }
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

            private readonly Mock<IFile> fileWrapperMock;

            private readonly Mock<IHost> host;
            private readonly ConfigurableConfigurationProvider configProvider;
            private readonly Mock<ISolutionRuleSetsInformationProvider> rulesetInfoProviderMock;

            private SonarLintMode bindingMode;

            public UserSettings StandaloneModeSettings { get; set; }
            public UserSettings ConnectedModeSettings { get; set; }
            public DummyCFamilyRulesConfig SonarWayConfig { get; set; }

            public bool ConnectedSettingsFileExists { get; set; } = true;

            public TestLogger Logger { get; }

            public TestEnvironmentBuilder(SonarLintMode mode)
            {
                standaloneSettingsProviderMock = new Mock<IUserSettingsProvider>();
                fileWrapperMock = new Mock<IFile>();
                Logger = new TestLogger();

                sonarWayProviderMock = new Mock<ICFamilyRulesConfigProvider>();
                configProvider = new ConfigurableConfigurationProvider();
                rulesetInfoProviderMock = new Mock<ISolutionRuleSetsInformationProvider>();

                // Register the local services
                host = new Mock<IHost>();
                host.Setup(x => x.GetService(typeof(IConfigurationProvider))).Returns(configProvider);
                host.Setup(x => x.GetService(typeof(ISolutionRuleSetsInformationProvider))).Returns(rulesetInfoProviderMock.Object);

                bindingMode = mode;
            }

            public CFamilyRuleConfigProvider CreateTestSubject()
            {
                // Data: set up the binding configuration
                configProvider.ModeToReturn = bindingMode;
                configProvider.ProjectToReturn = new Persistence.BoundSonarQubeProject(new System.Uri("http://localhost:9000"),
                    "sqProjectKey", "sqProjectName");

                // Data: user-configured settings
                standaloneSettingsProviderMock.Setup(x => x.UserSettings).Returns(StandaloneModeSettings);

                // Data: connected mode settings
                const string connectedSettingsFilesPath = "zzz\\foo.bar"; 
                var connectedSettingsData = JsonConvert.SerializeObject(ConnectedModeSettings, Formatting.Indented);
                
                rulesetInfoProviderMock.Setup(x => x.CalculateSolutionSonarQubeRuleSetFilePath("sqProjectKey", It.IsAny<Language>(), bindingMode))
                    .Returns(connectedSettingsFilesPath);

                fileWrapperMock.Setup(x => x.Exists(connectedSettingsFilesPath)).Returns(ConnectedSettingsFileExists);
                fileWrapperMock.Setup(x => x.ReadAllText(connectedSettingsFilesPath))
                    .Returns(connectedSettingsData);

                // Data: SonarWay configuration
                sonarWayProviderMock.Setup(x => x.GetRulesConfiguration(It.IsAny<string>()))
                    .Returns(SonarWayConfig);

                var testSubject = new CFamilyRuleConfigProvider(host.Object, standaloneSettingsProviderMock.Object, Logger,
                    sonarWayProviderMock.Object, fileWrapperMock.Object);

                return testSubject;
            }

            public void AssertConnectedSettingsNotAccessed()
            {
                rulesetInfoProviderMock.Verify(x => x.CalculateSolutionSonarQubeRuleSetFilePath(It.IsAny<string>(),
                    It.IsAny<Language>(), It.IsAny<SonarLintMode>()), Times.Never);
                fileWrapperMock.Verify(x => x.Exists(It.IsAny<string>()), Times.Never);
                fileWrapperMock.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Never);
            }

            public void AssertStandaloneSettingsNotAccessed()
            {
                standaloneSettingsProviderMock.Verify(x => x.UserSettings, Times.Never);
            }
        }
    }
}

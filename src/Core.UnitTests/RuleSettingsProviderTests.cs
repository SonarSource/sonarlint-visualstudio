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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class RuleSettingsProviderTests
    {
        private readonly Language validLanguage = Language.C;

        private readonly RulesSettings standaloneSettings = new()
        {
            Rules = new Dictionary<string, RuleConfig>
            {
                {"standalone", new RuleConfig()}
            }
        };

        private readonly RulesSettings connectedModeSettings = new()
        {
            Rules = new Dictionary<string, RuleConfig>
            {
                {"connected", new RuleConfig()}
            }
        };

        [TestMethod]
        public void Get_NotInConnectedMode_UserSettings()
        {
            var userSettingsProvider = CreateUserSettingsProvider(standaloneSettings);
            var ruleSettingsSerializer = new Mock<IRulesSettingsSerializer>();

            var testSubject = CreateTestSubject(BindingConfiguration.Standalone, userSettingsProvider.Object, ruleSettingsSerializer.Object);

            var result = testSubject.Get();

            result.Should().Be(standaloneSettings);

            userSettingsProvider.VerifyGet(x=> x.UserSettings, Times.Once);
            userSettingsProvider.VerifyNoOtherCalls();

            ruleSettingsSerializer.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void Get_InConnectedMode_CantFindConnectModeFile_UserSettings()
        {
            var userSettingsProvider = CreateUserSettingsProvider(standaloneSettings);
            var bindingConfiguration = GetConnectedModeConfiguration();
            var ruleSettingsSerializer = CreateRulesSettingsSerializer(bindingConfiguration, null);

            var testSubject = CreateTestSubject(bindingConfiguration, userSettingsProvider.Object, ruleSettingsSerializer.Object);

            var result = testSubject.Get();

            result.Should().Be(standaloneSettings);

            userSettingsProvider.VerifyGet(x => x.UserSettings, Times.Once);
            userSettingsProvider.VerifyNoOtherCalls();

            ruleSettingsSerializer.Verify(x => x.SafeLoad(ConnectedModeFilePath(bindingConfiguration)), Times.Once);
            ruleSettingsSerializer.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Get_InConnectedMode_FoundConnectModeFile_ConnectedModeSettings()
        {
            var userSettingsProvider = new Mock<IUserSettingsProvider>();
            var bindingConfiguration = GetConnectedModeConfiguration();
            var ruleSettingsSerializer = CreateRulesSettingsSerializer(bindingConfiguration, connectedModeSettings);

            var testSubject = CreateTestSubject(bindingConfiguration, userSettingsProvider.Object, ruleSettingsSerializer.Object);

            var result = testSubject.Get();

            result.Should().Be(connectedModeSettings);

            userSettingsProvider.Invocations.Count.Should().Be(0);

            ruleSettingsSerializer.Verify(x => x.SafeLoad(ConnectedModeFilePath(bindingConfiguration)), Times.Once);
            ruleSettingsSerializer.VerifyNoOtherCalls();
        }

        private RuleSettingsProvider CreateTestSubject(BindingConfiguration configuration, 
            IUserSettingsProvider userSettingsProvider,
            IRulesSettingsSerializer rulesSettingsSerializer)
        {
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            activeSolutionBoundTracker.Setup(x => x.CurrentConfiguration).Returns(configuration);

            return new RuleSettingsProvider(activeSolutionBoundTracker.Object,
                userSettingsProvider,
                rulesSettingsSerializer,
                validLanguage,
                Mock.Of<ILogger>());
        }

        private Mock<IUserSettingsProvider> CreateUserSettingsProvider(RulesSettings ruleSettings)
        {
            var userSettingsProvider = new Mock<IUserSettingsProvider>();
            userSettingsProvider.Setup(x => x.UserSettings).Returns(new UserSettings(ruleSettings));

            return userSettingsProvider;
        }

        private BindingConfiguration GetConnectedModeConfiguration()
        {
            return BindingConfiguration.CreateBoundConfiguration(
                new BoundServerProject("solution", "projectKey", new ServerConnection.SonarQube(new Uri("http://localhost:2000"))),
                SonarLintMode.Connected,
                "some directory");
        }

        private Mock<IRulesSettingsSerializer> CreateRulesSettingsSerializer(BindingConfiguration bindingConfiguration, RulesSettings settings)
        {
            var serializer = new Mock<IRulesSettingsSerializer>();
            serializer.Setup(x => x.SafeLoad(ConnectedModeFilePath(bindingConfiguration))).Returns(settings);

            return serializer;
        }

        private string ConnectedModeFilePath(BindingConfiguration bindingConfiguration)
        {
            return bindingConfiguration.BuildPathUnderConfigDirectory(validLanguage.FileSuffixAndExtension);
        }
    }
}

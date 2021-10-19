/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CloudSecrets;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry
{
    [TestClass]
    public class CloudSecretsTelemetryManagerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CloudSecretsTelemetryManager, ICloudSecretsTelemetryManager>(null,new []
            {
                MefTestHelpers.CreateExport<ITelemetryDataRepository>(Mock.Of<ITelemetryDataRepository>()),
                MefTestHelpers.CreateExport<IUserSettingsProvider>(Mock.Of<IUserSettingsProvider>())
            });
        }

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var userSettingsProvider = new Mock<IUserSettingsProvider>();
            userSettingsProvider.SetupAdd(x => x.SettingsChanged += (_, _) => { });

            CreateTestSubject(Mock.Of<ITelemetryDataRepository>(), userSettingsProvider.Object);

            userSettingsProvider.VerifyAdd(x => x.SettingsChanged += It.IsAny<System.EventHandler>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromEvents()
        {
            var userSettingsProvider = new Mock<IUserSettingsProvider>();
            userSettingsProvider.SetupAdd(x => x.SettingsChanged += (_, _) => { });

            var testSubject = CreateTestSubject(Mock.Of<ITelemetryDataRepository>(), userSettingsProvider.Object);
            
            userSettingsProvider.VerifyRemove(x => x.SettingsChanged -= It.IsAny<System.EventHandler>(), Times.Never);

            testSubject.Dispose();

            userSettingsProvider.VerifyRemove(x => x.SettingsChanged -= It.IsAny<System.EventHandler>(), Times.Once);
        }

        [TestMethod]
        public void UserSettingsChanged_DisabledSecretRulesAreUpdated()
        {
            var ruleSettings = new RulesSettings();
            ruleSettings.Rules.Add("rule1", new RuleConfig { Level = RuleLevel.Off }); // wrong repo
            ruleSettings.Rules.Add("secret:rule2", new RuleConfig { Level = RuleLevel.Off }); // wrong repo
            ruleSettings.Rules.Add("secrets:rule3", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("Secrets:rule4", new RuleConfig { Level = RuleLevel.Off }); // wrong case
            ruleSettings.Rules.Add("SECRETS:rule5", new RuleConfig { Level = RuleLevel.Off }); // wrong case
            ruleSettings.Rules.Add("secrets:rule6", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:rule7", new RuleConfig { Level = RuleLevel.On }); // enabled

            var userSettingsProvider = CreateUserSettingsProvider(ruleSettings);
            var telemetryData = CreateTelemetryData();
            var telemetryDataRepository = CreateTelemetryRepository(telemetryData);

            CreateTestSubject(telemetryDataRepository.Object, userSettingsProvider.Object);

            RaiseUserSettingsChangedEvent(userSettingsProvider);

            telemetryData.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEquivalentTo("secrets:rule3", "secrets:rule6");
            telemetryDataRepository.Verify(x=> x.Save(), Times.Once);
        }

        [TestMethod]
        public void UserSettingsChanged_PreviousRulesAreOverriden()
        {
            var ruleSettings = new RulesSettings();
            ruleSettings.Rules.Add("secrets:rule1", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:rule2", new RuleConfig { Level = RuleLevel.On });

            var userSettingsProvider = CreateUserSettingsProvider(ruleSettings);
            var telemetryData = CreateTelemetryData();
            var telemetryDataRepository = CreateTelemetryRepository(telemetryData);

            CreateTestSubject(telemetryDataRepository.Object, userSettingsProvider.Object);

            RaiseUserSettingsChangedEvent(userSettingsProvider);

            telemetryData.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEquivalentTo("secrets:rule1");

            ruleSettings.Rules["secrets:rule1"].Level = RuleLevel.On;
            ruleSettings.Rules["secrets:rule2"].Level = RuleLevel.Off;

            RaiseUserSettingsChangedEvent(userSettingsProvider);

            telemetryData.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEquivalentTo("secrets:rule2");
            telemetryDataRepository.Verify(x => x.Save(), Times.Exactly(2));

            ruleSettings.Rules["secrets:rule2"].Level = RuleLevel.On;

            RaiseUserSettingsChangedEvent(userSettingsProvider);

            telemetryData.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEmpty();
            telemetryDataRepository.Verify(x => x.Save(), Times.Exactly(3));
        }

        [TestMethod]
        public void UserSettingsChanged_ListIsOrdered()
        {
            var ruleSettings = new RulesSettings();
            ruleSettings.Rules.Add("secrets:cccc", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:bbbb", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:aaaa456", new RuleConfig { Level = RuleLevel.Off });
            ruleSettings.Rules.Add("secrets:aaaa123", new RuleConfig { Level = RuleLevel.Off });

            var userSettingsProvider = CreateUserSettingsProvider(ruleSettings);
            var telemetryData = CreateTelemetryData();
            var telemetryDataRepository = CreateTelemetryRepository(telemetryData);

            CreateTestSubject(telemetryDataRepository.Object, userSettingsProvider.Object);

            RaiseUserSettingsChangedEvent(userSettingsProvider);

            telemetryData.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEquivalentTo(
                "secrets:aaaa123",
                "secrets:aaaa456",
                "secrets:bbbb",
                "secrets:cccc");
        }

        [TestMethod]
        public void SecretDetected_NoPreviousIssues_TelemetryUpdated()
        {
            var telemetryData = CreateTelemetryData();
            var telemetryDataRepository = CreateTelemetryRepository(telemetryData);

            var testSubject = CreateTestSubject(telemetryDataRepository.Object);

            testSubject.SecretDetected("rule1");

            telemetryData.RulesUsage.RulesThatRaisedIssues.Should().BeEquivalentTo("rule1");
            telemetryDataRepository.Verify(x=> x.Save(), Times.Once);
        }

        [TestMethod]
        public void SecretDetected_HasPreviousIssues_TelemetryUpdated()
        {
            var telemetryData = CreateTelemetryData();
            var telemetryDataRepository = CreateTelemetryRepository(telemetryData);

            var testSubject = CreateTestSubject(telemetryDataRepository.Object);

            testSubject.SecretDetected("rule1");
            testSubject.SecretDetected("rule2");
            testSubject.SecretDetected("rule3");

            telemetryData.RulesUsage.RulesThatRaisedIssues.Should().BeEquivalentTo("rule1", "rule2", "rule3");
            telemetryDataRepository.Verify(x => x.Save(), Times.Exactly(3));
        }

        [TestMethod]
        public void SecretDetected_DuplicatedIssuesAreIgnored()
        {
            var telemetryData = CreateTelemetryData();
            var telemetryDataRepository = CreateTelemetryRepository(telemetryData);

            var testSubject = CreateTestSubject(telemetryDataRepository.Object);

            testSubject.SecretDetected("rule1");
            testSubject.SecretDetected("rule1");
            testSubject.SecretDetected("rule1");

            telemetryData.RulesUsage.RulesThatRaisedIssues.Should().BeEquivalentTo("rule1");

            // should be called only for the first time
            telemetryDataRepository.Verify(x => x.Save(), Times.Exactly(1));
        }

        [TestMethod]
        public void SecretDetected_RuleAlreadyInTheList_TelemetryNotUpdated()
        {
            var telemetryData = CreateTelemetryData();
            var telemetryDataRepository = CreateTelemetryRepository(telemetryData);

            var testSubject = CreateTestSubject(telemetryDataRepository.Object);

            testSubject.SecretDetected("rule1");

            var oldList = telemetryData.RulesUsage.RulesThatRaisedIssues;

            testSubject.SecretDetected("rule1");

            telemetryData.RulesUsage.RulesThatRaisedIssues.Should().BeSameAs(oldList);
            telemetryData.RulesUsage.RulesThatRaisedIssues.Should().BeEquivalentTo("rule1");

            // should be called only for the first time
            telemetryDataRepository.Verify(x => x.Save(), Times.Exactly(1));
        }

        [TestMethod]
        public void SecretDetected_ListIsOrdered()
        {
            var telemetryData = CreateTelemetryData();
            var telemetryDataRepository = CreateTelemetryRepository(telemetryData);

            var testSubject = CreateTestSubject(telemetryDataRepository.Object);

            testSubject.SecretDetected("cccc");
            testSubject.SecretDetected("bbbb");
            testSubject.SecretDetected("aaaa:456");
            testSubject.SecretDetected("aaaa:123");

            telemetryData.RulesUsage.RulesThatRaisedIssues.Should().BeEquivalentTo(
                "aaaa:123",
                "aaaa:456",
                "bbbb",
                "cccc");
        }

        private CloudSecretsTelemetryManager CreateTestSubject(ITelemetryDataRepository telemetryDataRepository, IUserSettingsProvider userSettingsProvider = null)
        {
            userSettingsProvider ??= Mock.Of<IUserSettingsProvider>();

            return new(telemetryDataRepository, userSettingsProvider);
        }

        private static Mock<ITelemetryDataRepository> CreateTelemetryRepository(TelemetryData data)
        {
            var telemetryRepository = new Mock<ITelemetryDataRepository>();
            telemetryRepository.SetupGet(x => x.Data).Returns(data);

            return telemetryRepository;
        }

        private TelemetryData CreateTelemetryData() =>
            new()
            {
                RulesUsage = new RulesUsage()
            };

        private static void RaiseUserSettingsChangedEvent(Mock<IUserSettingsProvider> userSettingsProvider)
        {
            userSettingsProvider.Raise(x => x.SettingsChanged += null, EventArgs.Empty);
        }

        private static Mock<IUserSettingsProvider> CreateUserSettingsProvider(RulesSettings ruleSettings)
        {
            var userSettingsProvider = new Mock<IUserSettingsProvider>();
            userSettingsProvider.Setup(x => x.UserSettings).Returns(new UserSettings(ruleSettings));

            return userSettingsProvider;
        }
    }
}

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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CloudSecrets;
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
                MefTestHelpers.CreateExport<ITelemetryDataRepository>(Mock.Of<ITelemetryDataRepository>())
            });
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
            telemetryDataRepository.Verify(x => x.Save(), Times.Exactly(3));
        }

        private CloudSecretsTelemetryManager CreateTestSubject(ITelemetryDataRepository telemetryDataRepository) => 
            new(telemetryDataRepository);

        private static Mock<ITelemetryDataRepository> CreateTelemetryRepository(TelemetryData data)
        {
            var telemetryRepository = new Mock<ITelemetryDataRepository>();
            telemetryRepository.SetupGet(x => x.Data).Returns(data);

            return telemetryRepository;
        }

        private TelemetryData CreateTelemetryData() =>
            new TelemetryData
            {
                RulesUsage = new RulesUsage()
            };
    }
}

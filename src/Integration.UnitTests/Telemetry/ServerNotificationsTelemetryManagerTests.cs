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

using System.Collections.Generic;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry
{
    [TestClass]
    public class ServerNotificationsTelemetryManagerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerNotificationsTelemetryManager, IServerNotificationsTelemetryManager>(
                MefTestHelpers.CreateExport<ITelemetryDataRepository>());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void NotificationsToggled_ChangeNotificationsEnabledFlag(bool areNotificationsEnabled)
        {
            var telemetryData = CreateServerNotificationsData();
            var telemetryRepository = CreateTelemetryRepository(telemetryData);
            var testSubject = CreateTestSubject(telemetryRepository.Object);

            testSubject.NotificationsToggled(areNotificationsEnabled);

            telemetryData.ServerNotifications.IsDisabled.Should().Be(!areNotificationsEnabled);
            telemetryRepository.Verify(x => x.Save(), Times.Once);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        public void NotificationReceived_CounterIncremented(int numberOfNotifications)
        {
            const string counterName = "Test";
            var telemetryData = CreateServerNotificationsData();

            var telemetryRepository = CreateTelemetryRepository(telemetryData);
            var testSubject = CreateTestSubject(telemetryRepository.Object);

            telemetryData.ServerNotifications.ServerNotificationCounters.Should().NotContainKey(counterName);

            for (var i = 0; i < numberOfNotifications; i++)
            {
                testSubject.NotificationReceived(counterName);
            }

            telemetryData.ServerNotifications.ServerNotificationCounters[counterName].ReceivedCount.Should().Be(numberOfNotifications);

            telemetryRepository.Verify(x => x.Save(), Times.Exactly(numberOfNotifications));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        public void NotificationClicked_CounterIncremented(int numberOfNotifications)
        {
            const string counterName = "Test";
            var telemetryData = CreateServerNotificationsData();

            var telemetryRepository = CreateTelemetryRepository(telemetryData);
            var testSubject = CreateTestSubject(telemetryRepository.Object);

            telemetryData.ServerNotifications.ServerNotificationCounters.Should().NotContainKey(counterName);

            for (var i = 0; i < numberOfNotifications; i++)
            {
                testSubject.NotificationClicked(counterName);
            }

            telemetryData.ServerNotifications.ServerNotificationCounters[counterName].ClickedCount.Should().Be(numberOfNotifications);

            telemetryRepository.Verify(x => x.Save(), Times.Exactly(numberOfNotifications));
        }

        private static IServerNotificationsTelemetryManager CreateTestSubject(ITelemetryDataRepository dataRepository)
        {
            return new ServerNotificationsTelemetryManager(dataRepository);
        }

        private static Mock<ITelemetryDataRepository> CreateTelemetryRepository(TelemetryData data)
        {
            var telemetryRepository = new Mock<ITelemetryDataRepository>();
            telemetryRepository.SetupGet(x => x.Data).Returns(data);
            
            return telemetryRepository;
        }

        private TelemetryData CreateServerNotificationsData() =>
            new TelemetryData
            {
                ServerNotifications = new ServerNotifications
                {
                    ServerNotificationCounters = new Dictionary<string, ServerNotificationCounter>()
                }
            };
    }
}

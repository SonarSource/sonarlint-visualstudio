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
            MefTestHelpers.CheckTypeCanBeImported<ServerNotificationsTelemetryManager, IServerNotificationsTelemetryManager>(null, new[]
            {
                MefTestHelpers.CreateExport<ITelemetryDataRepository>(Mock.Of<ITelemetryDataRepository>())
            });
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
        [DataRow(0)]
        [DataRow(1)]
        public void QualityGateNotificationReceived_CounterIncremented(int initialCounter)
        {
            VerifyCounterIncreased(initialCounter,
                notificationCounters => notificationCounters.QualityGateNotificationCounter.ReceivedCount = initialCounter,
                notificationCounters => notificationCounters.QualityGateNotificationCounter.ReceivedCount,
                manager => manager.QualityGateNotificationReceived());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void QualityGateNotificationClicked_CounterIncremented(int initialCounter)
        {
            VerifyCounterIncreased(initialCounter, 
                notificationCounters => notificationCounters.QualityGateNotificationCounter.ClickedCount = initialCounter,
                notificationCounters => notificationCounters.QualityGateNotificationCounter.ClickedCount,
                manager => manager.QualityGateNotificationClicked());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void NewIssueNotificationReceived_CounterIncremented(int initialCounter)
        {
            VerifyCounterIncreased(initialCounter,
                notificationCounters => notificationCounters.NewIssuesNotificationCounter.ReceivedCount = initialCounter,
                notificationCounters => notificationCounters.NewIssuesNotificationCounter.ReceivedCount,
                manager => manager.NewIssueNotificationReceived());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void NewIssueNotificationClicked_CounterIncremented(int initialCounter)
        {
            VerifyCounterIncreased(initialCounter,
                notificationCounters => notificationCounters.NewIssuesNotificationCounter.ClickedCount = initialCounter,
                notificationCounters => notificationCounters.NewIssuesNotificationCounter.ClickedCount,
                manager => manager.NewIssueNotificationClicked());
        }

        private void VerifyCounterIncreased(int initialCounter, 
            Action<ServerNotificationCounters> setInitialCounter, 
            Func<ServerNotificationCounters, int> getCounter,
            Action<IServerNotificationsTelemetryManager> incrementCounter)
        {
            var telemetryData = CreateServerNotificationsData();
            var serverNotificationCounters = telemetryData.ServerNotifications.ServerNotificationCounters;
            setInitialCounter(serverNotificationCounters);

            var telemetryRepository = CreateTelemetryRepository(telemetryData);
            var testSubject = CreateTestSubject(telemetryRepository.Object);

            incrementCounter(testSubject);

            getCounter(serverNotificationCounters).Should().Be(initialCounter + 1);
            telemetryRepository.Verify(x => x.Save(), Times.Once);
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
                    ServerNotificationCounters = new ServerNotificationCounters
                    {
                        QualityGateNotificationCounter = new ServerNotificationCounter(),
                        NewIssuesNotificationCounter = new ServerNotificationCounter()
                    }
                }
            };
    }
}

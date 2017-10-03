/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryManager_ShouldSendData
    {
        private const int CloseTimeThresholdInMilliseconds = 10000;
        private static readonly DateTime Beginning_Of_Day = new DateTime(2017, 10, 15, 0, 0, 0);
        private static readonly TimeSpan _04h_59m_59s = new TimeSpan(4, 59, 59);
        private static readonly TimeSpan _05h_00m_00s = new TimeSpan(5, 0, 0);
        private static readonly TimeSpan _05h_00m_01s = new TimeSpan(5, 0, 1);
        private static readonly TimeSpan _23h_59m_59s = new TimeSpan(23, 59, 59);
        private static readonly TimeSpan _24h_00m_00s = new TimeSpan(24, 0, 0);

        private Mock<IActiveSolutionBoundTracker> activeSolutionTrackerMock;
        private Mock<ITelemetryDataRepository> telemetryRepositoryMock;
        private Mock<ITelemetryClient> telemetryClientMock;
        private Mock<ITimerFactory> timerFactoryMock;
        private Mock<IKnownUIContexts> knownUIContexts;
        private Mock<IClock> clockMock;

        private TelemetryManager manager;

        [TestInitialize]
        public void TestInitialize()
        {
            activeSolutionTrackerMock = new Mock<IActiveSolutionBoundTracker>();
            telemetryRepositoryMock = new Mock<ITelemetryDataRepository>();
            telemetryClientMock = new Mock<ITelemetryClient>();
            timerFactoryMock = new Mock<ITimerFactory>();
            knownUIContexts = new Mock<IKnownUIContexts>();
            clockMock = new Mock<IClock>();

            telemetryRepositoryMock.SetupGet(repository => repository.Data).Returns(new TelemetryData());

            timerFactoryMock.Setup(factory => factory.Create())
                .Returns(new Queue<ITimer>(new[] { new Mock<ITimer>().Object, new Mock<ITimer>().Object }).Dequeue);

            manager = new TelemetryManager(activeSolutionTrackerMock.Object, telemetryRepositoryMock.Object,
                telemetryClientMock.Object, timerFactoryMock.Object, clockMock.Object, knownUIContexts.Object);
        }

        [TestMethod]
        public void Now_Is_00h_00m_00s()
        {
            // Arrange
            var now = Beginning_Of_Day;
            clockMock.Setup(clock => clock.Now).Returns(now);

            // Act & Assert
            manager.ShouldSendData(lastSentDate: now.Subtract(_04h_59m_59s)).Should().BeFalse();
            manager.ShouldSendData(lastSentDate: now.Subtract(_05h_00m_00s)).Should().BeTrue();
        }

        [TestMethod]
        public void Now_Is_4h_59m_59s()
        {
            // Arrange
            var now = Beginning_Of_Day.Add(_04h_59m_59s);
            clockMock.Setup(clock => clock.Now).Returns(now);

            // Act & Assert
            manager.ShouldSendData(lastSentDate: now.Subtract(_04h_59m_59s)).Should().BeFalse();
            manager.ShouldSendData(lastSentDate: now.Subtract(_05h_00m_00s)).Should().BeTrue();
        }

        [TestMethod]
        public void Now_Is_5h_00m_00s()
        {
            // Arrange
            var now = Beginning_Of_Day.Add(_05h_00m_00s);
            clockMock.Setup(clock => clock.Now).Returns(now);

            // Act & Assert
            manager.ShouldSendData(lastSentDate: now.Subtract(_05h_00m_00s)).Should().BeFalse();
            manager.ShouldSendData(lastSentDate: now.Subtract(_05h_00m_01s)).Should().BeTrue();
        }

        [TestMethod]
        public void Now_Is_23h_59m_59s()
        {
            // Arrange
            var now = Beginning_Of_Day.Add(_23h_59m_59s);
            clockMock.Setup(clock => clock.Now).Returns(now);

            // Act & Assert
            manager.ShouldSendData(lastSentDate: now.Subtract(_23h_59m_59s)).Should().BeFalse();
            manager.ShouldSendData(lastSentDate: now.Subtract(_24h_00m_00s)).Should().BeTrue();
        }
    }
}

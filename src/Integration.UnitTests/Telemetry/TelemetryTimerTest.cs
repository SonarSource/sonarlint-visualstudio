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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration
{
    [TestClass]
    public class TelemetryTimerTest
    {
        private static readonly DateTime Beginning_Of_Day = new DateTime(2017, 10, 15, 0, 0, 0);
        private static readonly TimeSpan _05h_59m_59s = new TimeSpan(5, 59, 59);
        private static readonly TimeSpan _06h_00m_00s = new TimeSpan(6, 0, 0);

        private Mock<ITelemetryDataRepository> telemetryRepositoryMock;
        private Mock<ITimer> timerMock;

        private TelemetryTimer telemetryTimer;

        [TestInitialize]
        public void TestInitialize()
        {
            telemetryRepositoryMock = new Mock<ITelemetryDataRepository>();
            timerMock = new Mock<ITimer>();

            var timerFactoryMock = new Mock<ITimerFactory>();
            timerFactoryMock.Setup(x => x.Create()).Returns(timerMock.Object);

            telemetryTimer = new TelemetryTimer(telemetryRepositoryMock.Object, timerFactoryMock.Object);
        }

        [TestMethod]
        public void Ctor_Throws_ArgumentNullException_For_TelemetryRepository()
        {
            Action action = () => new TelemetryTimer(null, new Mock<ITimerFactory>().Object);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("telemetryRepository");
        }

        [TestMethod]
        public void Ctor_Throws_ArgumentNullException_For_MainTimer()
        {
            Action action = () => new TelemetryTimer(new Mock<ITelemetryDataRepository>().Object, null);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("timerFactory");
        }

        [TestMethod]
        public void Ctor_Initializes_MainTimer()
        {
            // Arrange
            var timer = new Mock<ITimer>();
            timer.SetupSet(x => x.Interval = 300000);
            timer.SetupSet(x => x.AutoReset = true);

            var timerFactoryMock = new Mock<ITimerFactory>();
            timerFactoryMock.Setup(x => x.Create()).Returns(timer.Object);

            // Act
            new TelemetryTimer(new Mock<ITelemetryDataRepository>().Object, timerFactoryMock.Object);

            // Assert
            timer.VerifyAll();
        }

        [TestMethod]
        public void Start_Starts_Timer()
        {
            // Arrange
            timerMock.Setup(x => x.Start());

            // Act
            telemetryTimer.Start();

            // Assert
            timerMock.Verify(x => x.Start(), Times.Once);
        }

        [TestMethod]
        public void Subsequent_Start_Does_Not_Start_Timers()
        {
            // Arrange
            timerMock.Setup(x => x.Start());

            // Act
            telemetryTimer.Start();
            telemetryTimer.Start();
            telemetryTimer.Start();

            // Assert
            timerMock.Verify(x => x.Start(), Times.Once);
        }

        [TestMethod]
        public void Stop_Stops_Timer_If_Started()
        {
            // Arrange
            //timerMock.Setup(x => x.Stop());

            // Act
            telemetryTimer.Start();
            telemetryTimer.Stop();

            // Assert
            timerMock.Verify(x => x.Stop(), Times.Once);
        }

        [TestMethod]
        public void Stop_Does_Not_Stop_Timers_If_Not_Started()
        {
            // Arrange
            timerMock.Setup(x => x.Stop());

            // Act
            telemetryTimer.Stop();

            // Assert
            timerMock.Verify(x => x.Stop(), Times.Never);
        }

        [TestMethod]
        public void Dispose_Stops_Timer_If_Started()
        {
            // Arrange
            timerMock.Setup(x => x.Stop());

            // Act
            telemetryTimer.Start();
            telemetryTimer.Dispose();

            // Assert
            timerMock.Verify(x => x.Stop(), Times.Once);
        }

        [TestMethod]
        public void Dispose_Does_Not_Stop_Timers_If_Not_Started()
        {
            // Arrange
            timerMock.Setup(x => x.Stop());

            // Act
            telemetryTimer.Dispose();

            // Assert
            timerMock.Verify(x => x.Stop(), Times.Never);
        }

        [TestMethod]
        public void MainTimer_Changes_Period_After_First_Elapsed_Event()
        {
            // Arrange to prevent NullReferenceExceptions
            telemetryRepositoryMock.Setup(x => x.Data)
                .Returns(new TelemetryData { LastUploadDate = Beginning_Of_Day });

            // Arrange
            timerMock.SetupSet(x => x.Interval = 21600000);
            telemetryTimer.Start();

            // Act
            timerMock.Raise(x => x.Elapsed += null, new TimerEventArgs(Beginning_Of_Day));

            // Assert
            timerMock.VerifySet(x => x.Interval = 21600000);
        }

        [TestMethod]
        public void LastUpload_Previous_Day_And_Less_Than_6h_Elapsed_Not_Raised()
        {
            // Arrange
            var now = Beginning_Of_Day;

            telemetryRepositoryMock.Setup(x => x.Data)
                .Returns(new TelemetryData { LastUploadDate = now.Subtract(_05h_59m_59s) });

            telemetryTimer.Start();
            telemetryTimer.MonitorEvents();

            // Act
            timerMock.Raise(x => x.Elapsed += null, new TimerEventArgs(now));

            // Assert
            telemetryTimer.ShouldNotRaise(nameof(TelemetryTimer.Elapsed));
        }

        [TestMethod]
        public void LastUpload_Previous_Day_More_Than_6h_Elapsed_Raised()
        {
            // Arrange
            var now = Beginning_Of_Day;

            telemetryRepositoryMock.Setup(x => x.Data)
                .Returns(new TelemetryData { LastUploadDate = now.Subtract(_06h_00m_00s) });

            telemetryTimer.Start();
            telemetryTimer.MonitorEvents();

            // Act
            timerMock.Raise(x => x.Elapsed += null, new TimerEventArgs(now));

            // Assert
            telemetryTimer.ShouldRaise(nameof(TelemetryTimer.Elapsed));
        }

        [TestMethod]
        public void LastUpload_Same_Day_And_Less_Than_6h_Elapsed_Not_Raised()
        {
            // Arrange
            var now = Beginning_Of_Day.AddHours(12);

            telemetryRepositoryMock.Setup(x => x.Data)
                .Returns(new TelemetryData { LastUploadDate = now.Subtract(_05h_59m_59s) });

            telemetryTimer.Start();
            telemetryTimer.MonitorEvents();

            // Act
            timerMock.Raise(x => x.Elapsed += null, new TimerEventArgs(now));

            // Assert
            telemetryTimer.ShouldNotRaise(nameof(TelemetryTimer.Elapsed));
        }

        [TestMethod]
        public void LastUpload_Same_Day_And_More_Than_6h_Elapsed_Not_Raised()
        {
            // Arrange
            var now = Beginning_Of_Day.AddHours(12);

            telemetryRepositoryMock.Setup(x => x.Data)
                .Returns(new TelemetryData { LastUploadDate = now.Subtract(_06h_00m_00s) });

            telemetryTimer.Start();
            telemetryTimer.MonitorEvents();

            // Act
            timerMock.Raise(x => x.Elapsed += null, new TimerEventArgs(now));

            // Assert
            telemetryTimer.ShouldNotRaise(nameof(TelemetryTimer.Elapsed));
        }
    }
}

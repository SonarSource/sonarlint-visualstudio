using System;
using System.Timers;
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
        private Mock<IClock> clockMock;

        private TelemetryTimer telemetryTimer;

        [TestInitialize]
        public void TestInitialize()
        {
            telemetryRepositoryMock = new Mock<ITelemetryDataRepository>();
            timerMock = new Mock<ITimer>();
            clockMock = new Mock<IClock>();

            telemetryTimer = new TelemetryTimer(telemetryRepositoryMock.Object, clockMock.Object, timerMock.Object);
        }

        [TestMethod]
        public void Ctor_Throws_ArgumentNullException_For_TelemetryRepository()
        {
            Action action = () => new TelemetryTimer(null, new Mock<IClock>().Object, new Mock<ITimer>().Object);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("telemetryRepository");
        }

        [TestMethod]
        public void Ctor_Throws_ArgumentNullException_For_Clock()
        {
            Action action = () => new TelemetryTimer(new Mock<ITelemetryDataRepository>().Object, null, new Mock<ITimer>().Object);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("clock");
        }

        [TestMethod]
        public void Ctor_Throws_ArgumentNullException_For_MainTimer()
        {
            Action action = () => new TelemetryTimer(new Mock<ITelemetryDataRepository>().Object, new Mock<IClock>().Object, null);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("timer");
        }

        [TestMethod]
        public void Ctor_Initializes_MainTimer()
        {
            // Arrange
            var timerMock = new Mock<ITimer>();
            timerMock.SetupSet(timer => timer.Interval = 60000);
            timerMock.SetupSet(timer => timer.AutoReset = true);

            // Act
            new TelemetryTimer(new Mock<ITelemetryDataRepository>().Object, new Mock<IClock>().Object, timerMock.Object);

            // Assert
            timerMock.VerifyAll();
        }

        [TestMethod]
        public void Start_Starts_Timer()
        {
            // Arrange
            timerMock.Setup(timer => timer.Start());

            // Act
            telemetryTimer.Start();

            // Assert
            timerMock.Verify(timer => timer.Start(), Times.Once);
        }

        [TestMethod]
        public void Subsequent_Start_Does_Not_Start_Timers()
        {
            // Arrange
            timerMock.Setup(timer => timer.Start());

            // Act
            telemetryTimer.Start();
            telemetryTimer.Start();
            telemetryTimer.Start();

            // Assert
            timerMock.Verify(timer => timer.Start(), Times.Once);
        }

        [TestMethod]
        public void Stop_Stops_Timer_If_Started()
        {
            // Arrange
            timerMock.Setup(timer => timer.Stop());

            // Act
            telemetryTimer.Start();
            telemetryTimer.Stop();

            // Assert
            timerMock.Verify(timer => timer.Stop(), Times.Once);
        }

        [TestMethod]
        public void Stop_Does_Not_Stop_Timers_If_Not_Started()
        {
            // Arrange
            timerMock.Setup(timer => timer.Stop());

            // Act
            telemetryTimer.Stop();

            // Assert
            timerMock.Verify(timer => timer.Stop(), Times.Never);
        }

        [TestMethod]
        public void Dispose_Stops_Timer_If_Started()
        {
            // Arrange
            timerMock.Setup(timer => timer.Stop());

            // Act
            telemetryTimer.Start();
            telemetryTimer.Dispose();

            // Assert
            timerMock.Verify(timer => timer.Stop(), Times.Once);
        }

        [TestMethod]
        public void Dispose_Does_Not_Stop_Timers_If_Not_Started()
        {
            // Arrange
            timerMock.Setup(timer => timer.Stop());

            // Act
            telemetryTimer.Dispose();

            // Assert
            timerMock.Verify(timer => timer.Stop(), Times.Never);
        }

        [TestMethod]
        public void MainTimer_Changes_Period_After_First_Elapsed_Event()
        {
            // Arrange to prevent NullReferenceExceptions
            clockMock.Setup(clock => clock.Now).Returns(Beginning_Of_Day);
            telemetryRepositoryMock.Setup(repository => repository.Data)
                .Returns(new TelemetryData { LastUploadDate = Beginning_Of_Day });

            // Arrange
            timerMock.SetupSet(timer => timer.Interval = 21600000);
            telemetryTimer.Start();

            // Act
            timerMock.Raise(timer => timer.Elapsed += null, (ElapsedEventArgs)null);

            // Assert
            timerMock.VerifySet(timer => timer.Interval = 21600000);
        }

        [TestMethod]
        public void LastUpload_Previous_Day_And_Less_Than_6h_Elapsed_Not_Raised()
        {
            // Arrange
            var now = Beginning_Of_Day;

            clockMock.Setup(clock => clock.Now).Returns(now);

            telemetryRepositoryMock.Setup(repository => repository.Data)
                .Returns(new TelemetryData { LastUploadDate = now.Subtract(_05h_59m_59s) });

            telemetryTimer.Start();
            telemetryTimer.MonitorEvents();

            // Act
            timerMock.Raise(timer => timer.Elapsed += null, (ElapsedEventArgs)null);

            // Assert
            telemetryTimer.ShouldNotRaise(nameof(TelemetryTimer.Elapsed));
        }

        [TestMethod]
        public void LastUpload_Previous_Day_More_Than_6h_Elapsed_Raised()
        {
            // Arrange
            var now = Beginning_Of_Day;

            clockMock.Setup(clock => clock.Now).Returns(now);

            telemetryRepositoryMock.Setup(repository => repository.Data)
                .Returns(new TelemetryData { LastUploadDate = now.Subtract(_06h_00m_00s) });

            telemetryTimer.Start();
            telemetryTimer.MonitorEvents();

            // Act
            timerMock.Raise(timer => timer.Elapsed += null, (ElapsedEventArgs)null);

            // Assert
            telemetryTimer.ShouldRaise(nameof(TelemetryTimer.Elapsed));
        }

        [TestMethod]
        public void LastUpload_Same_Day_And_Less_Than_6h_Elapsed_Not_Raised()
        {
            // Arrange
            var now = Beginning_Of_Day.AddHours(12);

            clockMock.Setup(clock => clock.Now).Returns(now);

            telemetryRepositoryMock.Setup(repository => repository.Data)
                .Returns(new TelemetryData { LastUploadDate = now.Subtract(_05h_59m_59s) });

            telemetryTimer.Start();
            telemetryTimer.MonitorEvents();

            // Act
            timerMock.Raise(timer => timer.Elapsed += null, (ElapsedEventArgs)null);

            // Assert
            telemetryTimer.ShouldNotRaise(nameof(TelemetryTimer.Elapsed));
        }

        [TestMethod]
        public void LastUpload_Same_Day_And_More_Than_6h_Elapsed_Not_Raised()
        {
            // Arrange
            var now = Beginning_Of_Day.AddHours(12);

            clockMock.Setup(clock => clock.Now).Returns(now);

            telemetryRepositoryMock.Setup(repository => repository.Data)
                .Returns(new TelemetryData { LastUploadDate = now.Subtract(_06h_00m_00s) });

            telemetryTimer.Start();
            telemetryTimer.MonitorEvents();

            // Act
            timerMock.Raise(timer => timer.Elapsed += null, (ElapsedEventArgs)null);

            // Assert
            telemetryTimer.ShouldNotRaise(nameof(TelemetryTimer.Elapsed));
        }
    }
}

using System;
using System.Drawing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SystemInterface.Timers;

namespace SonarLint.VisualStudio.Integration.Vsix.Notifications.UnitTests
{
    [TestClass]
    public class SonarQubeNotifications_StartTests
    {
        private Mock<INotifyIconFactory> notifyIconFactoryMock;
        private Mock<ITimerFactory> timerFactoryMock;

        private Mock<INotifyIcon> notifyIconMock;
        private Mock<ITimer> timerMock;

        static SonarQubeNotifications_StartTests()
        {
            // https://stackoverflow.com/questions/6005398/uriformatexception-invalid-uri-invalid-port-specified
            if (!UriParser.IsKnownScheme("pack"))
            {
                new System.Windows.Application();
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            timerMock = new Mock<ITimer>();
            timerMock.SetupProperty(mock => mock.Interval);

            notifyIconMock = new Mock<INotifyIcon>();
            notifyIconMock.SetupProperty(mock => mock.Text);
            notifyIconMock.SetupProperty(mock => mock.Icon);

            timerFactoryMock = new Mock<ITimerFactory>();
            notifyIconFactoryMock = new Mock<INotifyIconFactory>();
        }

        [TestMethod]
        public void Start_Creates_Icon_And_Timer()
        {
            // Arrange
            notifyIconFactoryMock
               .Setup(mock => mock.Create())
               .Returns(notifyIconMock.Object);

            timerFactoryMock
                .Setup(mock => mock.Create())
                .Returns(timerMock.Object);

            var notifications = new SonarQubeNotifications(notifyIconFactoryMock.Object, timerFactoryMock.Object);

            // Act
            notifications.Start();

            // Assert
            notifyIconFactoryMock.Verify(mock => mock.Create(), Times.Once());
            var notifyIcon = notifyIconMock.Object;
            notifyIcon.Text.Should().Be($"Initializing...");
            notifyIcon.Icon.Should().NotBeNull();

            timerFactoryMock.Verify(mock => mock.Create(), Times.Once());
            var timer = timerMock.Object;
            timer.Interval.Should().Be(10000d);
        }

        [TestMethod]
        public void Start_Twise_Creates_Only_One_Icon_And_Timer()
        {
            // Arrange
            notifyIconFactoryMock
               .SetupSequence(mock => mock.Create())
               .Returns(notifyIconMock.Object);

            timerFactoryMock
                .SetupSequence(mock => mock.Create())
                .Returns(timerMock.Object);

            var notifications = new SonarQubeNotifications(notifyIconFactoryMock.Object, timerFactoryMock.Object);

            // Act
            notifications.Start();
            notifications.Start();

            // Assert
            notifyIconFactoryMock.Verify(mock => mock.Create(), Times.Once());
            timerFactoryMock.Verify(mock => mock.Create(), Times.Once());
        }
    }
}

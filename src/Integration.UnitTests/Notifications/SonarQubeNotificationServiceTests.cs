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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Notifications.UnitTests
{
    [TestClass]
    public class SonarQubeNotificationServiceTests
    {
        private static readonly DateTimeOffset longTimeAgo = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset yesterday = DateTimeOffset.Now.AddDays(-1);
        private const int _1sec = 1000;

        private Mock<ITimer> timerMock;
        private Mock<INotificationIndicatorViewModel> modelMock;
        private Mock<ISonarQubeService> sonarQubeServiceMock;

        private IList<SonarQubeNotification> serverNotifications = new List<SonarQubeNotification>();
        private Mock<ISonarLintOutput> outputMock;

        private SonarQubeNotificationService notifications;

        [TestInitialize]
        public void TestInitialize()
        {
            timerMock = new Mock<ITimer>();
            outputMock = new Mock<ISonarLintOutput>();

            modelMock = new Mock<INotificationIndicatorViewModel>();
            modelMock.SetupAllProperties();

            sonarQubeServiceMock = new Mock<ISonarQubeService>();
            sonarQubeServiceMock
                .Setup(x => x.GetNotificationEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(serverNotifications));
            sonarQubeServiceMock
                .SetupGet(x => x.IsConnected)
                .Returns(true);

            notifications = new SonarQubeNotificationService(sonarQubeServiceMock.Object,
                modelMock.Object, timerMock.Object, outputMock.Object);
        }

        [TestMethod]
        public void Dispose_Stops_Timer_Hides_Icon()
        {
            notifications.Dispose();

            timerMock.Verify(x => x.Stop(), Times.Once);
            modelMock.VerifySet(x => x.IsIconVisible = false, Times.Once);
        }

        [TestMethod]
        public void Stop_Stops_Timer_Hides_Icon()
        {
            notifications.Stop();

            timerMock.Verify(x => x.Stop(), Times.Once);
            modelMock.VerifySet(x => x.IsIconVisible = false, Times.Once);
        }

        [TestMethod]
        public void StartAsync_Throws_ArgumentNullException_For_ProjectKey()
        {
            // Arrange
            Func<Task> action = async () => await notifications.StartAsync(null, null);

            // Act & Assert
            action.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("projectKey");
        }

        [TestMethod]
        public async Task Exceptions_From_SonarQubeService_Are_Written_To_Output()
        {
            // Arrange
            sonarQubeServiceMock
                .Setup(x => x.GetNotificationEventsAsync("should-throw", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("test exception"));

            // Act
            await notifications.StartAsync("should-throw", null);

            // Assert
            outputMock.Verify(x => x.Write($"Failed to fetch notifications: test exception"), Times.Once);
        }

        [TestMethod]
        public async Task StartAsync_Sets_IsVisible()
        {
            // Arrange

            // Act
            await notifications.StartAsync("test", null);

            // Assert
            timerMock.Verify(x => x.Start(), Times.Once);
            notifications.Model.IsIconVisible.Should().BeTrue();
        }

        [TestMethod]
        public async Task StartAsync_No_NotificationData_Sets_LastNotificationDate_To_One_Day_Ago()
        {
            // Arrange

            // Act
            await notifications.StartAsync("test", null);

            // Assert
            notifications.GetNotificationData().LastNotificationDate
                .Should().BeCloseTo(yesterday, _1sec);
        }

        [TestMethod]
        public async Task StartAsync_Recent_NotificationData_Sets_LastNotificationDate()
        {
            // Arrange
            var earlierToday = DateTimeOffset.Now.AddHours(-6);

            // Act
            await notifications.StartAsync("test", new NotificationData { LastNotificationDate = earlierToday });

            // Assert
            notifications.GetNotificationData().LastNotificationDate.Should().Be(earlierToday);
        }

        [TestMethod]
        public async Task StartAsync_Old_NotificationData_Sets_LastNotificationDate_To_One_Day_Ago()
        {
            // Arrange
            var data = new NotificationData
            {
                IsEnabled = true,
                LastNotificationDate = longTimeAgo
            };

            // Act
            await notifications.StartAsync("test", data);

            // Assert
            notifications.GetNotificationData().LastNotificationDate
                .Should().BeCloseTo(yesterday, _1sec);
        }

        [TestMethod]
        public async Task StartAsync_Gets_Events_For_Project_Sets_LastNotificationDate_Day_Minus_1()
        {
            // Act
            await notifications.StartAsync("test", null);

            // Assert
            sonarQubeServiceMock
                .Verify(x => x.GetNotificationEventsAsync("test", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()));
            notifications.GetNotificationData().LastNotificationDate.Should().BeCloseTo(
                DateTimeOffset.Now.AddDays(-1), 1000);
        }

        [TestMethod]
        public async Task StartAsync_Stops_Timer_Hides_Icon_If_SonarQubeService_Returns_Null()
        {
            // Arrange
            sonarQubeServiceMock
                .Setup(x => x.GetNotificationEventsAsync("test", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<SonarQubeNotification>>(null));

            // Act
            await notifications.StartAsync("test", null);

            // Assert
            sonarQubeServiceMock
                .Verify(x => x.GetNotificationEventsAsync("test", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()));

            timerMock.Verify(x => x.Stop(), Times.Once);
            modelMock.VerifySet(x => x.IsIconVisible = false, Times.Once);
        }

        [TestMethod]
        public async Task Timer_Elapsed_Gets_Events_For_Project_Sets_LastNotificationDate_To_Most_Recent_Event_Date()
        {
            var olderEventDate = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var newerEventDate = new DateTimeOffset(2010, 1, 1, 0, 0, 1, TimeSpan.Zero);
            var newestEventDate = new DateTimeOffset(2010, 1, 1, 0, 0, 2, TimeSpan.Zero);

            var event1 = new SonarQubeNotification(
                    category: "QUALITY_GATE",
                    link: new Uri("http://foo.com"),
                    date: olderEventDate,
                    message: "foo");

            var event2 = new SonarQubeNotification(
                    category: "QUALITY_GATE",
                    link: new Uri("http://foo.com"),
                    date: newerEventDate,
                    message: "foo");

            var event3 = new SonarQubeNotification(
                    category: "QUALITY_GATE",
                    link: new Uri("http://foo.com"),
                    date: newestEventDate,
                    message: "foo");

            serverNotifications.Add(event1);

            // 1. Start monitoring
            // Must call StartAsync so the cancellation token is initialized. This will fetch the event data.
            await notifications.StartAsync("anykey", null);

            sonarQubeServiceMock.Verify(x => x.GetNotificationEventsAsync("anykey", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            notifications.GetNotificationData().LastNotificationDate.Should().BeCloseTo(
                DateTimeOffset.Now.AddDays(-1), 1000);

            // 2. Simulate the timer triggering
            timerMock.Raise(x => x.Elapsed += null, (ElapsedEventArgs)null);
            notifications.GetNotificationData().LastNotificationDate.Should().Be(olderEventDate);
            sonarQubeServiceMock.Verify(x => x.GetNotificationEventsAsync("anykey", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            // 3. Add newer events and simulate the timer firing
            serverNotifications.Add(event2);
            serverNotifications.Add(event3);

            // Simulate the timer triggering
            timerMock.Raise(x => x.Elapsed += null, (ElapsedEventArgs)null);

            sonarQubeServiceMock.Verify(x => x.GetNotificationEventsAsync("anykey", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            notifications.GetNotificationData().LastNotificationDate.Should().Be(newestEventDate);
        }

        [TestMethod]
        public async Task Timer_Elapsed_Stops_Timer_Hides_Icon_If_SonarQubeService_Returns_Null()
        {
            var event1 = new SonarQubeNotification(
                    category: "QUALITY_GATE",
                    link: new Uri("http://foo.com"),
                    date: new DateTimeOffset(2010, 1, 1, 0, 0, 2, TimeSpan.Zero),
                    message: "foo");

            serverNotifications.Add(event1);

            // 1. Start monitoring - server returns one event
            // Must call StartAsync so the cancellation token is initialized
            await notifications.StartAsync("anykey", null);

            sonarQubeServiceMock
                .Verify(x => x.GetNotificationEventsAsync("anykey", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Once);
            timerMock.Verify(x => x.Stop(), Times.Never);

            // 2. Simulate timer elapsing - server returns null
            sonarQubeServiceMock
                .Setup(x => x.GetNotificationEventsAsync("anykey", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<SonarQubeNotification>>(null));

            timerMock.Raise(x => x.Elapsed += null, (ElapsedEventArgs)null);

            sonarQubeServiceMock
                .Verify(x => x.GetNotificationEventsAsync("anykey", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            timerMock.Verify(x => x.Stop(), Times.Once);
            modelMock.VerifySet(x => x.IsIconVisible = false, Times.Once);
        }

        [TestMethod]
        public async Task Start_Stop_Start_Stop_Sequence()
        {
            sonarQubeServiceMock
                .Setup(x => x.GetNotificationEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
                                It.IsAny<CancellationToken>()))
                .Callback<string, DateTimeOffset, CancellationToken>((_, __, token) =>
                    token.IsCancellationRequested.Should().BeFalse())
                .Returns(Task.FromResult(serverNotifications));

            await notifications.StartAsync("test", null);
            timerMock.Verify(x => x.Start(), Times.Once);
            notifications.Model.IsIconVisible.Should().BeTrue();

            notifications.Stop();
            timerMock.Verify(x => x.Stop(), Times.Once);
            notifications.Model.IsIconVisible.Should().BeFalse();

            await notifications.StartAsync("test", null);
            timerMock.Verify(x => x.Start(), Times.Exactly(2));
            notifications.Model.IsIconVisible.Should().BeTrue();

            notifications.Stop();
            timerMock.Verify(x => x.Stop(), Times.Exactly(2));
            notifications.Model.IsIconVisible.Should().BeFalse();
        }

        [TestMethod]
        public async Task Icon_Shown_When_Notifications_Disabled()
        {
            sonarQubeServiceMock
                .Setup(x => x.GetNotificationEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
                                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(serverNotifications));

            var event1 = new SonarQubeNotification(
                category: "QUALITY_GATE",
                link: new Uri("http://foo.com"),
                date: DateTimeOffset.Now,
                message: "foo");

            serverNotifications.Add(event1);

            await notifications.StartAsync("test", new NotificationData
                {
                    IsEnabled = false,
                    LastNotificationDate = DateTimeOffset.MinValue
                });

            timerMock.Verify(x => x.Start(), Times.Once);
            notifications.Model.IsIconVisible.Should().BeTrue();

            // simulate timer tick
            timerMock.Raise(x => x.Elapsed += null, (ElapsedEventArgs)null);

            notifications.Model.IsIconVisible.Should().BeTrue();
        }

        [TestMethod]
        public async Task Notifications_Icon_Shown_If_First_Call_Fails_But_Later_Succeed()
        {
            sonarQubeServiceMock
                .SetupSequence(x => x.GetNotificationEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
                                It.IsAny<CancellationToken>()))
                .Throws(new HttpException())
                .Throws(new Exception())
                .Returns(Task.FromResult(serverNotifications));

            var event1 = new SonarQubeNotification(
                category: "QUALITY_GATE",
                link: new Uri("http://foo.com"),
                date: DateTimeOffset.Now,
                message: "foo");

            serverNotifications.Add(event1);

            await notifications.StartAsync("test", null);

            timerMock.Verify(x => x.Start(), Times.Once);
            notifications.Model.IsIconVisible.Should().BeFalse();

            // simulate timer tick
            timerMock.Raise(x => x.Elapsed += null, (ElapsedEventArgs)null);
            notifications.Model.IsIconVisible.Should().BeFalse();

            // simulate timer tick
            timerMock.Raise(x => x.Elapsed += null, (ElapsedEventArgs)null);

            notifications.Model.IsIconVisible.Should().BeTrue();
        }
    }
}

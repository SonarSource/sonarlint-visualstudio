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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Notifications.UnitTests
{
    [TestClass]
    public class SonarQubeNotificationsTests
    {
        private Mock<ITimer> timerMock;
        private Mock<INotificationIndicatorViewModel> modelMock;

        private async Task<ISonarQubeService> GetConnectedService(params NotificationsResponse[] expectedEvents)
        {
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));

            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "6.6" }));

            client.Setup(x => x.GetNotificationEventsAsync(It.IsAny<NotificationsRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Result<NotificationsResponse[]>(successResponse, expectedEvents));

            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);

            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);
            return service;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            timerMock = new Mock<ITimer>();
            modelMock = new Mock<INotificationIndicatorViewModel>();
            modelMock.SetupAllProperties();
        }

        [TestMethod]
        public async Task Start_Sets_IsVisible()
        {
            // Arrange
            timerMock.Setup(mock => mock.Start());
            var sqService = await GetConnectedService();

            var model = modelMock.Object;
            var notifications = new SonarQubeNotifications(sqService, model, timerMock.Object);

            // Act
            await notifications.StartAsync("test", null);

            // Assert
            model.IsIconVisible.Should().BeTrue();
        }

        [TestMethod]
        public async Task Test_DefaultNotificationDate_IsOneDayAgo()
        {
            // Arrange
            var sqService = await GetConnectedService();
            using (var notifications = new SonarQubeNotifications(sqService, modelMock.Object, timerMock.Object))
            {
                await notifications.StartAsync("test", null);

                // Assert
                notifications.GetNotificationData().LastNotificationDate
                    .Should().BeCloseTo(DateTimeOffset.Now.AddDays(-1), (int)TimeSpan.FromSeconds(5).TotalMilliseconds);
            }
        }

        [TestMethod]
        public async Task Test_OldNotificationDate_IsSetToOneDayAgo()
        {
            // Arrange
            var sqService = await GetConnectedService();
            using (var notifications = new SonarQubeNotifications(sqService, modelMock.Object, timerMock.Object))
            {
                var date = new NotificationData
                {
                    IsEnabled = true,
                    LastNotificationDate =
                       new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(1))
                };

                await notifications.StartAsync("test", date);

                // Assert
                notifications.GetNotificationData().LastNotificationDate
                    .Should().BeCloseTo(DateTimeOffset.Now.AddDays(-1), (int)TimeSpan.FromSeconds(5).TotalMilliseconds);
            }
        }

        [TestMethod]
        public async Task Test_NotificationDate_IsSetToDateOfMostRecentEvent()
        {
            // Arrange
            var event1 = new NotificationsResponse
            {
                Category = "QUALITY_GATE",
                Link = new Uri("http://foo.com"),
                Date = new DateTimeOffset(2010, 1, 1, 14, 59, 59, TimeSpan.FromHours(2)),
                Message = "foo",
                Project = "test"
            };

            var event2 = new NotificationsResponse
            {
                Category = "QUALITY_GATE",
                Link = new Uri("http://foo.com"),
                Date = new DateTimeOffset(2010, 1, 1, 14, 59, 59, TimeSpan.FromHours(1)),
                Message = "foo",
                Project = "test"
            };

            var sqService = await GetConnectedService(event1, event2);
            using (var notifications = new SonarQubeNotifications(sqService, modelMock.Object, timerMock.Object))
            {
                var date = new NotificationData
                {
                    IsEnabled = true,
                    LastNotificationDate =
                       new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(1))
                };

                await notifications.StartAsync("test", date);

                // Assert
                notifications.GetNotificationData().LastNotificationDate
                    .Should().BeCloseTo(event2.Date, (int)TimeSpan.FromSeconds(5).TotalMilliseconds);
            }
        }
    }
}

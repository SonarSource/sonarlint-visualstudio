/*
 * SonarQube Client
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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Tests.Services
{
    [TestClass]
    public class SonarQubeService_GetNotificationEventsAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetNotifications_ExampleFromSonarQube()
        {
            var now = new DateTimeOffset(2017, 10, 19, 13, 0, 0, TimeSpan.FromHours(2));

            await ConnectToSonarQube("6.6.0.0");

            SetupRequest("api/developers/search_events?projects=my_project&from=2017-10-19T13:00:00%2b0200",
                @"{
  ""events"": [
    {
      ""category"": ""QUALITY_GATE"",
      ""message"": ""Quality Gate of project 'My Project' is now Red (was Orange)"",
      ""link"": ""https://sonarcloud.io/dashboard?id=my_project"",
      ""project"": ""my_project"",
      ""date"":""2017-11-15T17:07:34+0100""
    },
    {
      ""category"": ""NEW_ISSUES"",
      ""message"": ""You have 15 new issues on project 'My Project'"",
      ""link"": ""https://sonarcloud.io/project/issues?id=my_project&createdAfter=2017-03-01T00%3A00%3A00%2B0100&assignees=me%40github&resolved=false"",
      ""project"": ""my_project"",
      ""date"":""2017-12-20T17:07:34+0100""
    }
  ]
}");

            var result = await service.GetNotificationEventsAsync("my_project", now, CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(2);
            result.Select(x => x.Category).Should().BeEquivalentTo(new[] { "QUALITY_GATE", "NEW_ISSUES" });
            result.Select(x => x.Date).Should().BeEquivalentTo(
                new[]
                {
                    DateTimeOffset.Parse("2017-11-15T17:07:34+0100"),
                    DateTimeOffset.Parse("2017-12-20T17:07:34+0100")
                });
            result.Select(x => x.Link).Should().BeEquivalentTo(
                new[]
                {
                    "https://sonarcloud.io/dashboard?id=my_project",
                    "https://sonarcloud.io/project/issues?id=my_project&createdAfter=2017-03-01T00%3A00%3A00%2B0100&assignees=me%40github&resolved=false"
                });
            result.Select(x => x.Message).Should().BeEquivalentTo(
                new[]
                {
                    "Quality Gate of project 'My Project' is now Red (was Orange)",
                    "You have 15 new issues on project 'My Project'"
                });
        }

        [TestMethod]
        public async Task GetNotifications_NotFound()
        {
            var now = new DateTimeOffset(2017, 10, 19, 13, 0, 0, TimeSpan.FromHours(2));

            await ConnectToSonarQube("6.6.0.0");

            SetupRequest("api/developers/search_events?projects=my_project&from=2017-10-19T13:00:00%2b0200", "",
                HttpStatusCode.NotFound);

            var result = await service.GetNotificationEventsAsync("my_project", now, CancellationToken.None);

            result.Should().BeNull();

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetNotifications_OtherError()
        {
            var now = new DateTimeOffset(2017, 10, 19, 13, 0, 0, TimeSpan.FromHours(2));

            await ConnectToSonarQube("6.6.0.0");

            SetupRequest("api/developers/search_events?projects=my_project&from=2017-10-19T13:00:00%2b0200", "",
                HttpStatusCode.InternalServerError);

            var result = await service.GetNotificationEventsAsync("my_project", now, CancellationToken.None);

            result.Should().BeEmpty();

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetNotifications_Old_SonarQube()
        {
            await ConnectToSonarQube("6.5.0.0");

            Func<Task> action = async () =>
                await service.GetNotificationEventsAsync("my_project", new DateTimeOffset(), CancellationToken.None);

            action.ShouldThrow<InvalidOperationException>();
        }
    }
}

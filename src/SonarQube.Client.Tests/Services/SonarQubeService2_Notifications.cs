using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Tests.Services
{
    [TestClass]
    public class SonarQubeService2_Notifications : SonarQubeService2_TestBase
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
      ""project"": ""my_project""
    },
    {
      ""category"": ""NEW_ISSUES"",
      ""message"": ""You have 15 new issues on project 'My Project'"",
      ""link"": ""https://sonarcloud.io/project/issues?id=my_project&createdAfter=2017-03-01T00%3A00%3A00%2B0100&assignees=me%40github&resolved=false"",
      ""project"": ""my_project""
    }
  ]
}");

            var result = await service.GetNotificationEventsAsync("my_project", now, CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(2);
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

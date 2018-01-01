using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Services
{
    [TestClass]
    public class SonarQubeService2_Plugins : SonarQubeService2_TestBase
    {
        [TestMethod]
        public async Task GetPlugins_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("api/updatecenter/installed_plugins",
                @"[
  {
    ""key"": ""findbugs"",
    ""name"": ""Findbugs"",
    ""version"": ""2.1""
  },
  {
    ""key"": ""l10nfr"",
    ""name"": ""French Pack"",
    ""version"": ""1.10""
  },
  {
    ""key"": ""jira"",
    ""name"": ""JIRA"",
    ""version"": ""1.2""
  }
]");

            var result = await service.GetAllPluginsAsync(CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(3);
        }

        [TestMethod]
        public async Task GetPlugins_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("api/updatecenter/installed_plugins", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubePlugin>>> func = async () =>
                await service.GetAllPluginsAsync(CancellationToken.None);

            func.ShouldThrow<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}

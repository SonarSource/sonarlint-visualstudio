using System;
using System.Collections.Generic;
using System.IO;
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
    public class SonarQubeService2_Issues : SonarQubeService2_TestBase
    {
        [TestMethod]
        public async Task GetSuppressedIssuesAsync_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("batch/issues?key=project1",
                new StreamReader(@"TestResources\IssuesProtobufResponse").ReadToEnd());

            var result = await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            // TODO: create a protobuf file with more than one issue with different states
            result.Should().HaveCount(0);

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("batch/issues?key=project1", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeIssue>>> func = async () =>
                await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            func.ShouldThrow<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class SonarQubeService2_Organizations : SonarQubeService2_TestBase
    {
        [TestMethod]
        public async Task GetOrganizations_ExampleFromSonarQube()
        {
            await ConnectToSonarQube("6.2.0.0");

            SetupRequest("api/organizations/search?p=1&ps=500",
                @"
{
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 25,
    ""total"": 2
  },
  ""organizations"": [
    {
      ""key"": ""foo-company"",
      ""name"": ""Foo Company"",
      ""guarded"": true
    },
    {
      ""key"": ""bar-company"",
      ""name"": ""Bar Company"",
      ""description"": ""The Bar company produces quality software too."",
      ""url"": ""https://www.bar.com"",
      ""avatar"": ""https://www.bar.com/logo.png"",
      ""guarded"": false
    }
  ]
}");

            var result = await service.GetAllOrganizationsAsync(CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task GetOrganizations_Paging()
        {
            await ConnectToSonarQube("6.2.0.0");

            SetupRequest("api/organizations/search?p=1&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 1,
    ""pageSize"": 500,
    ""total"": 2
  }},
  ""organizations"": [
    {string.Join(",\n", Enumerable.Range(1, 500).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            SetupRequest("api/organizations/search?p=2&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 2,
    ""pageSize"": 500,
    ""total"": 2
  }},
  ""organizations"": [
    {string.Join(",\n", Enumerable.Range(501, 500).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            SetupRequest("api/organizations/search?p=3&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 3,
    ""pageSize"": 500,
    ""total"": 2
  }},
  ""organizations"": [
    {string.Join(",\n", Enumerable.Range(1001, 10).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            var result = await service.GetAllOrganizationsAsync(CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(1010);
        }

        [TestMethod]
        public async Task GetOrganizations_NotFound()
        {
            await ConnectToSonarQube("6.2.0.0");

            SetupRequest("api/organizations/search?p=1&ps=500", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeOrganization>>> func = async () => await service.GetAllOrganizationsAsync(CancellationToken.None);

            func.ShouldThrow<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetOrganizations_Old_SonarQube()
        {
            await ConnectToSonarQube("6.1.0.0");

            Func<Task> action = async () => await service.GetAllOrganizationsAsync(CancellationToken.None);

            action.ShouldThrow<InvalidOperationException>();
        }
    }
}

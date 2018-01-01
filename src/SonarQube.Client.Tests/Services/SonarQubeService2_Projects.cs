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
    public class SonarQubeService2_Projects : SonarQubeService2_TestBase
    {
        [TestMethod]
        public async Task GetProjects_Old_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("api/projects/index",
                @"[
  {
    ""id"": ""5035"",
    ""k"": ""org.jenkins-ci.plugins:sonar"",
    ""nm"": ""Jenkins Sonar Plugin"",
    ""sc"": ""PRJ"",
    ""qu"": ""TRK""
  },
  {
    ""id"": ""5146"",
    ""k"": ""org.codehaus.sonar-plugins:sonar-ant-task"",
    ""nm"": ""Sonar Ant Task"",
    ""sc"": ""PRJ"",
    ""qu"": ""TRK""
  },
  {
    ""id"": ""15964"",
    ""k"": ""org.codehaus.sonar-plugins:sonar-build-breaker-plugin"",
    ""nm"": ""Sonar Build Breaker Plugin"",
    ""sc"": ""PRJ"",
    ""qu"": ""TRK""
  }
]");

            var result = await service.GetAllProjectsAsync("myorganization", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(3);
        }

        [TestMethod]
        public async Task GetProjects_New_ExampleFromSonarQube()
        {
            await ConnectToSonarQube("6.2.0.0");

            SetupRequest("api/components/search_projects?organization=myorganization&asc=true&p=1&ps=500",
                @"{
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 100,
    ""total"": 3
  },
  ""components"": [
    {
      ""organization"": ""myorganization"",
      ""id"": ""AU-Tpxb--iU5OvuD2FLy"",
      ""key"": ""my_project"",
      ""name"": ""My Project 1"",
      ""isFavorite"": true,
      ""tags"": [
        ""finance"",
        ""java""
      ],
      ""visibility"": ""public""
    },
    {
      ""organization"": ""myorganization"",
      ""id"": ""AU-TpxcA-iU5OvuD2FLz"",
      ""key"": ""another_project"",
      ""name"": ""My Project 2"",
      ""isFavorite"": false,
      ""tags"": [],
      ""visibility"": ""public""
    },
    {
      ""organization"": ""myorganization"",
      ""id"": ""AU-TpxcA-iU5OvuD2FL0"",
      ""key"": ""third_project"",
      ""name"": ""My Project 3"",
      ""isFavorite"": false,
      ""tags"": [
        ""sales"",
        ""offshore"",
        ""java""
      ],
      ""visibility"": ""public""
    }
  ],
  ""facets"": [
    {
      ""property"": ""coverage"",
      ""values"": [
        {
          ""val"": ""NO_DATA"",
          ""count"": 0
        },
        {
          ""val"": ""*-30.0"",
          ""count"": 1
        },
        {
          ""val"": ""30.0-50.0"",
          ""count"": 0
        },
        {
          ""val"": ""50.0-70.0"",
          ""count"": 0
        },
        {
          ""val"": ""70.0-80.0"",
          ""count"": 0
        },
        {
          ""val"": ""80.0-*"",
          ""count"": 2
        }
      ]
    }
  ]
}");

            var result = await service.GetAllProjectsAsync("myorganization", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(3);
        }

        [TestMethod]
        public async Task GetProjects_New_Paging()
        {
            await ConnectToSonarQube("6.2.0.0");

            var projects1to500 = string.Join(",\n", Enumerable.Range(1, 500).Select(i => $@"{{
      ""organization"": ""myorganization"",
      ""id"": ""{i}"",
      ""key"": ""{i}"",
      ""name"": ""Project{i}"" }}"));

            SetupRequest($"api/components/search_projects?organization=myorganization&asc=true&p={1}&ps=500",
                $@"{{
  ""paging"": {{
    ""pageIndex"": 1,
    ""pageSize"": 500,
    ""total"": 3
  }},
  ""components"": [ {projects1to500} ],
  ""facets"": []
}}");

            var projects501to1000 = string.Join(",\n", Enumerable.Range(501, 500).Select(i => $@"{{
      ""organization"": ""myorganization"",
      ""id"": ""{i}"",
      ""key"": ""{i}"",
      ""name"": ""Project{i}"" }}"));

            SetupRequest($"api/components/search_projects?organization=myorganization&asc=true&p={2}&ps=500",
                $@"{{
  ""paging"": {{
    ""pageIndex"": 2,
    ""pageSize"": 500,
    ""total"": 3
  }},
  ""components"": [ {projects501to1000} ],
  ""facets"": []
}}");

            var projects1001to1010 = string.Join(",\n", Enumerable.Range(1001, 10).Select(i => $@"{{
      ""organization"": ""myorganization"",
      ""id"": ""{i}"",
      ""key"": ""{i}"",
      ""name"": ""Project{i}"" }}"));

            SetupRequest($"api/components/search_projects?organization=myorganization&asc=true&p={3}&ps=500",
                $@"{{
  ""paging"": {{
    ""pageIndex"": 3,
    ""pageSize"": 500,
    ""total"": 3
  }},
  ""components"": [ {projects1001to1010} ],
  ""facets"": []
}}");

            var result = await service.GetAllProjectsAsync("myorganization", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(1010);
        }

        [TestMethod]
        public async Task GetProjects_Old_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("api/projects/index", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeProject>>> func = async () =>
                await service.GetAllProjectsAsync("myorganization", CancellationToken.None);

            func.ShouldThrow<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetProjects_New_NotFound()
        {
            await ConnectToSonarQube("6.2.0.0");

            SetupRequest("api/components/search_projects?organization=myorganization&asc=true&p=1&ps=500", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeProject>>> func = async () =>
                await service.GetAllProjectsAsync("myorganization", CancellationToken.None);

            func.ShouldThrow<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}

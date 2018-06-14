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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Api
{
    [TestClass]
    public class SonarQubeService_GetAllProjectsAsync : SonarQubeService_TestBase
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
            result.Select(x => x.Key).Should().BeEquivalentTo(
                new[]
                {
                    "org.jenkins-ci.plugins:sonar",
                    "org.codehaus.sonar-plugins:sonar-ant-task",
                    "org.codehaus.sonar-plugins:sonar-build-breaker-plugin"
                });
            result.Select(x => x.Name).Should().BeEquivalentTo(
                new[]
                {
                    "Jenkins Sonar Plugin",
                    "Sonar Ant Task",
                    "Sonar Build Breaker Plugin"
                });
        }

        [TestMethod]
        public async Task GetProjects_Old_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("api/projects/index", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeProject>>> func = async () =>
                await service.GetAllProjectsAsync("myorganization", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetProjects_ExampleFromSonarQube()
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
            result.Select(x => x.Key).Should().BeEquivalentTo(new[] { "my_project", "another_project", "third_project" });
            result.Select(x => x.Name).Should().BeEquivalentTo(new[] { "My Project 1", "My Project 2", "My Project 3" });
        }

        [TestMethod]
        public async Task GetProjects_Paging()
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
            result.Select(x => x.Key).Should().BeEquivalentTo(Enumerable.Range(1, 1010).Select(i => i.ToString()));
            result.Select(x => x.Name).Should().BeEquivalentTo(Enumerable.Range(1, 1010).Select(i => $"Project{i}"));
        }

        [TestMethod]
        public async Task GetProjects_NotFound()
        {
            await ConnectToSonarQube("6.2.0.0");

            SetupRequest("api/components/search_projects?organization=myorganization&asc=true&p=1&ps=500", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeProject>>> func = async () =>
                await service.GetAllProjectsAsync("myorganization", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}

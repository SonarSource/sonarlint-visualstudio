﻿/*
 * SonarQube Client
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Api.V7_20;
using SonarQube.Client.Tests.Infra;
using static SonarQube.Client.Tests.Infra.MocksHelper;

namespace SonarQube.Client.Tests.Requests.Api.V7_20
{
    [TestClass]
    public class GetIssuesRequestTests
    {
        [TestMethod]
        public async Task InvokeAsync_ResponseWithFlows_IsDeserializedCorrectly()
        {
            const string projectKey = "myproject";
            const string statusesToRequest = "some status";
            const string expectedEscapedStatusesInRequest = "some+status";

            var testSubject = CreateTestSubject(projectKey, statusesToRequest);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            var request = $"api/issues/search?projects={projectKey}&statuses={expectedEscapedStatusesInRequest}&p=1&ps=500";
            const string response = @"
{
  ""total"": 1,
  ""p"": 1,
  ""ps"": 100,
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 100,
    ""total"": 1
  },
  ""effortTotal"": 30,
  ""debtTotal"": 30,
  ""issues"": [
    {
      ""key"": ""AXNZHuA7uQ67pPQjI7e7"",
      ""rule"": ""roslyn.sonaranalyzer.security.cs:S5146"",
      ""severity"": ""BLOCKER"",
      ""component"": ""myprojectkey:projectroot/Controllers/WeatherForecastController.cs"",
      ""project"": ""myprojectkey"",
      ""line"": 43,
      ""hash"": ""d8684fca55d4dc80e444a993de15ba18"",
      ""textRange"": {
        ""startLine"": 43,
        ""endLine"": 43,
        ""startOffset"": 19,
        ""endOffset"": 43
      },
      ""flows"": [
        {
          ""locations"": [
            {
              ""component"": ""myprojectkey:projectroot/Controllers/WeatherForecastController.cs"",
              ""textRange"": {
                ""startLine"": 43,
                ""endLine"": 43,
                ""startOffset"": 19,
                ""endOffset"": 43
              },
              ""msg"": ""sink: tainted value is used to perform a security-sensitive operation""
            },
            {
              ""component"": ""myprojectkey:projectroot/Controllers/WeatherForecastController.cs"",
              ""textRange"": {
                ""startLine"": 41,
                ""endLine"": 41,
                ""startOffset"": 16,
                ""endOffset"": 58
              },
              ""msg"": ""tainted value is propagated""
            },
            {
              ""component"": ""myprojectkey:projectroot/Controllers/WeatherForecastController.cs"",
              ""textRange"": {
                ""startLine"": 41,
                ""endLine"": 41,
                ""startOffset"": 28,
                ""endOffset"": 58
              },
              ""msg"": ""tainted value is propagated""
            }
          ]
        },
        {
          ""locations"": [
            {
              ""component"": ""myprojectkey:projectroot/Controllers/Helper.cs"",
              ""textRange"": {
                ""startLine"": 7,
                ""endLine"": 7,
                ""startOffset"": 12,
                ""endOffset"": 29
              },
              ""msg"": ""tainted value is propagated""
            },
            {
              ""component"": ""myprojectkey:projectroot/Controllers/Helper.cs"",
              ""textRange"": {
                ""startLine"": 5,
                ""endLine"": 5,
                ""startOffset"": 29,
                ""endOffset"": 41
              },
              ""msg"": ""tainted value is propagated""
            }
          ],
        }
     ],
      ""status"": ""OPEN"",
      ""message"": ""Refactor this code to not perform redirects based on tainted, user-controlled data."",
      ""effort"": ""30min"",
      ""debt"": ""30min"",
      ""assignee"": ""rita-g-sonarsource@github"",
      ""author"": ""rita.gorokhod@sonarsource.com"",
      ""tags"": [],
      ""creationDate"": ""2020-07-16T21:31:25+0200"",
      ""updateDate"": ""2020-07-16T21:34:05+0200"",
      ""type"": ""VULNERABILITY"",
      ""organization"": ""myorganization"",
      ""fromHotspot"": false
    }
  ],
  ""components"": [
    {
      ""organization"": ""myorganization"",
      ""key"": ""myprojectkey:projectroot/Controllers/WeatherForecastController.cs"",
      ""uuid"": ""AXNZHtnVuQ67pPQjI7ey"",
      ""enabled"": true,
      ""qualifier"": ""FIL"",
      ""name"": ""WeatherForecastController.cs"",
      ""longName"": ""projectroot/Controllers/WeatherForecastController.cs"",
      ""path"": ""projectroot/Controllers/WeatherForecastController.cs""
    },
    {
      ""organization"": ""myorganization"",
      ""key"": ""myprojectkey:projectroot/Controllers/Helper.cs"",
      ""uuid"": ""AXNZHtnVuQ67pPQjI7ez"",
      ""enabled"": true,
      ""qualifier"": ""FIL"",
      ""name"": ""Helper.cs"",
      ""longName"": ""projectroot/Controllers/Helper.cs"",
      ""path"": ""projectroot/Controllers/Helper.cs""
    },
    {
      ""organization"": ""myorganization"",
      ""key"": ""myprojectkey"",
      ""uuid"": ""AXJLrCxxxeWiK2BCzDif"",
      ""enabled"": true,
      ""qualifier"": ""TRK"",
      ""name"": ""sanity-connected"",
      ""longName"": ""sanity-connected""
    }
  ],
  ""organizations"": [
    {
      ""key"": ""myorganization"",
      ""name"": ""a user""
    }
  ],
  ""facets"": []
}
";

            SetupHttpRequest(handlerMock, request, response);

            var results = await testSubject.InvokeAsync(httpClient, CancellationToken.None);
            results.Should().HaveCount(1);

            var result = results[0];

            result.RuleId.Should().Be("roslyn.sonaranalyzer.security.cs:S5146");
            result.Flows.Count().Should().Be(2);

            result.Flows[0].Locations.Count().Should().Be(3);
            result.Flows[1].Locations.Count().Should().Be(2);

            var firstFlowFirstLocation = results[0].Flows[0].Locations[0];
            firstFlowFirstLocation.ModuleKey.Should().Be("myprojectkey:projectroot/Controllers/WeatherForecastController.cs");
            firstFlowFirstLocation.FilePath.Should().Be("projectroot/Controllers/WeatherForecastController.cs");
            firstFlowFirstLocation.Message.Should().Be("sink: tainted value is used to perform a security-sensitive operation");
            firstFlowFirstLocation.TextRange.StartLine.Should().Be(43);
            firstFlowFirstLocation.TextRange.EndLine.Should().Be(43);
            firstFlowFirstLocation.TextRange.StartOffset.Should().Be(19);
            firstFlowFirstLocation.TextRange.EndOffset.Should().Be(43);

            var seconFlowSecondLocation = results[0].Flows[1].Locations[1];
            seconFlowSecondLocation.ModuleKey.Should().Be("myprojectkey:projectroot/Controllers/Helper.cs");
            seconFlowSecondLocation.FilePath.Should().Be("projectroot/Controllers/Helper.cs");
            seconFlowSecondLocation.Message.Should().Be("tainted value is propagated");
            seconFlowSecondLocation.TextRange.StartLine.Should().Be(5);
            seconFlowSecondLocation.TextRange.EndLine.Should().Be(5);
            seconFlowSecondLocation.TextRange.StartOffset.Should().Be(29);
            seconFlowSecondLocation.TextRange.EndOffset.Should().Be(41);
        }

        private static GetIssuesRequest CreateTestSubject(string projectKey, string statusesToRequest)
        {
            var testSubject = new GetIssuesRequest();
            testSubject.Logger = new TestLogger();
            testSubject.ProjectKey = projectKey;
            testSubject.Statuses = statusesToRequest;

            return testSubject;
        }
    }
}

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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_HasOrganizations : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task HasOrganizations_5_60()
        {
            await ConnectToSonarQube();

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizations_6_00()
        {
            await ConnectToSonarQube("6.0.0.0");

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizations_6_10()
        {
            await ConnectToSonarQube("6.1.0.0");

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizations_6_70_SonarQube_Default_ReturnsFalse()
        {
            await ConnectToSonarQube("6.7.0.0");

            SetupRequest("api/organizations/search?p=1&ps=3",
                // default API response from SQ6.7
                @"{""organizations"":[{""key"":""default-organization"",""name"":""Default Organization"",""guarded"":true}],""paging"":{""pageIndex"":1,""pageSize"":3,""total"":1}}");

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizations_6_70_WithOrganizations_ReturnsTrue()
        {
            await ConnectToSonarQube("6.7.0.0");

            SetupRequestWithOrganizations();

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task HasOrganizations_7_90_SonarQube_Default_ReturnsFalse()
        {
            await ConnectToSonarQube("7.9.0.0");

            SetupRequest("api/organizations/search?p=1&ps=3",
                // default response from SonarQube 7.9
                @"{""organizations"":[{""key"":""default-organization"",""name"":""Default Organization"",""guarded"":true,""actions"":{""admin"":false,""delete"":false,""provision"":false}}],""paging"":{""pageIndex"":1,""pageSize"":3,""total"":1}}");

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizations_7_90_WithOrganizations_ReturnsTrue()
        {
            await ConnectToSonarQube("7.9.0.0");

            SetupRequestWithOrganizations();

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task HasOrganizations_7_90_SonarCloud_ReturnsTrue()
        {
            await ConnectToSonarQube("7.9.0.0");

            // Actual response from SonarCloud at the point this test was written
            var sonarCloudResponse = @"{""organizations"":[{""key"":""farmsmart"",""name"":""FarmSmart"",""description"":"""",""url"":""https://www.farmsmart.co"",
""avatar"":""https://avatars3.githubusercontent.com/u/49406855?v=4"",""actions"":{""admin"":false,""delete"":false,""provision"":false}},
{""key"":""user-zero"",""name"":""user-zero"",""description"":"""",""url"":"""",""avatar"":""https://avatars2.githubusercontent.com/u/18621890?v=4"",
""actions"":{""admin"":false,""delete"":false,""provision"":false}},{""key"":""kvalchev"",""name"":""kvalchev"",""description"":"""",""url"":"""",""avatar"":"""",
""actions"":{""admin"":false,""delete"":false,""provision"":false}}],""paging"":{""pageIndex"":1,""pageSize"":3,""total"":41974}}";

            SetupRequest("api/organizations/search?p=1&ps=3", sonarCloudResponse);

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task HasOrganizations_99_99_Default_ReturnsFalse()
        {
            await ConnectToSonarQube("99.99.0.0");

            SetupRequest("api/organizations/search?p=1&ps=3",
                // default response from SonarQube 7.9
                @"{""organizations"":[{""key"":""default-organization"",""name"":""Default Organization"",""guarded"":true,""actions"":{""admin"":false,""delete"":false,""provision"":false}}],""paging"":{""pageIndex"":1,""pageSize"":3,""total"":1}}");

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizations_99_99_WithOrganizations_ReturnsTrue()
        {
            await ConnectToSonarQube("99.99.0.0");

            SetupRequestWithOrganizations();

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task HasOrganizations_EmptyOrganizationList_ReturnsFalse()
        {
            await ConnectToSonarQube("6.2.0.0");

            // Should not fail if the list is empty
            SetupRequest("api/organizations/search?p=1&ps=3",
@"
{
  ""organizations"": []
}");

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizations_99_90_NonOkHttpResponse_ReturnsTrue()
        {
            await ConnectToSonarQube("7.9.0.0");

            SetupRequestWithOrganizations(HttpStatusCode.InternalServerError);

            Action act = () => { var result = service.HasOrganizations(CancellationToken.None).Result; };

            act.Should().ThrowExactly<HttpRequestException>();
        }

        [TestMethod]
        public void HasOrganizationsFeature_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Action action = () => { var result = service.HasOrganizations(CancellationToken.None).Result; };

            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }

        private void SetupRequestWithOrganizations(HttpStatusCode httpResponseStatusCode = HttpStatusCode.OK)
        {
            SetupRequest("api/organizations/search?p=1&ps=3",
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
}", httpResponseStatusCode);
        }
    }
}

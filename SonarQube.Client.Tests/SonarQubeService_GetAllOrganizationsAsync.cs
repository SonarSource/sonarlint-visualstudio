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

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetAllOrganizationsAsync : SonarQubeService_TestBase
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
            result.Select(x => x.Key).Should().BeEquivalentTo(new[] { "foo-company", "bar-company" });
            result.Select(x => x.Name).Should().BeEquivalentTo(new[] { "Foo Company", "Bar Company" });
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
            result.Select(x => x.Key).Should().BeEquivalentTo(Enumerable.Range(1, 1010).Select(i => i.ToString()));
            result.Select(x => x.Name).Should().BeEquivalentTo(Enumerable.Range(1, 1010).Select(i => $"Name{i}"));
        }

        [TestMethod]
        public async Task GetOrganizations_NotFound()
        {
            await ConnectToSonarQube("6.2.0.0");

            SetupRequest("api/organizations/search?p=1&ps=500", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeOrganization>>> func = async () => await service.GetAllOrganizationsAsync(CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetOrganizations_Old_SonarQube()
        {
            await ConnectToSonarQube("6.1.0.0");

            Func<Task> action = async () => await service.GetAllOrganizationsAsync(CancellationToken.None);

            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("Could not find compatible implementation of 'IGetOrganizationsRequest' for SonarQube 6.1.0.0.");
        }

        [TestMethod]
        public async Task GetOrganizations_V7_00_ExampleFromSonarQube()
        {
            await ConnectToSonarQube("7.0.0.0");

            // The only difference between this and the previous version is the "member=true" query string parameter
            // which when specified, should get only the organizations that the current user is member of.
            SetupRequest("api/organizations/search?member=true&p=1&ps=500",
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
            result.Select(x => x.Key).Should().BeEquivalentTo(new[] { "foo-company", "bar-company" });
            result.Select(x => x.Name).Should().BeEquivalentTo(new[] { "Foo Company", "Bar Company" });
        }

        [TestMethod]
        public void GetAllOrganizationsAsync_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<IList<SonarQubeOrganization>>> func = async () =>
                await service.GetAllOrganizationsAsync(CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}

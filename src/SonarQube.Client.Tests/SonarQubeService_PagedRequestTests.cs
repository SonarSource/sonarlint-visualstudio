/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Requests;
using static SonarQube.Client.Tests.Infra.MocksHelper;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_PagedRequestTests
    {
        private Mock<HttpMessageHandler> messageHandler;
        private TestLogger logger;
        private HttpClient client;

        [TestInitialize]
        public void TestInitialize()
        {
            messageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            client = new HttpClient(messageHandler.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };
            logger = new TestLogger();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task MaxItemCount_RetrievesTheSpecifiedNumberOfPages(bool shouldLimit)
        {
            var request = new DummyPagedRequest
            {
                Logger = logger,
            };

            if (shouldLimit)
            {
                request.ItemsLimit = 1100;
            }

            SetupRequest("api/dummy?p=1&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 1,
    ""pageSize"": 4,
    ""total"": 1000
  }},
  ""dummyResponses"": [
    {string.Join(",\n", Enumerable.Range(1, 500).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            SetupRequest("api/dummy?p=2&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 2,
    ""pageSize"": 4,
    ""total"": 1000
  }},
  ""dummyResponses"": [
    {string.Join(",\n", Enumerable.Range(500, 500).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            SetupRequest("api/dummy?p=3&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 3,
    ""pageSize"": 4,
    ""total"": 1000
  }},
  ""dummyResponses"": [
    {string.Join(",\n", Enumerable.Range(1000, 500).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            SetupRequest("api/dummy?p=4&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 3,
    ""pageSize"": 4,
    ""total"": 1000
  }},
  ""dummyResponses"": [
    {string.Join(",\n", Enumerable.Range(1500, 100).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            var expectedCount = shouldLimit ? 1100 : 1600;

            var result = await request.InvokeAsync(client, CancellationToken.None);
            result.Should().HaveCount(expectedCount);
        }

        [TestMethod]
        public async Task PageSize_IsRespected()
        {
            var request = new DummyPagedRequest
            {
                Logger = logger,
                PageSize = 22
            };

            // If the correct page size isn't passed in the query string then the 
            // test will fail because the mock http client won't have a response
            // matches the supplied URL.
            SetupRequest("api/dummy?p=1&ps=22", @"
{
  ""dummyResponses"": [
    {
      ""key"": ""cs"",
      ""name"": ""C#"",
    },
    {
      ""key"": ""vbnet"",
      ""name"": ""VB.NET""
    }
  ]
}");
            var result = await request.InvokeAsync(client, CancellationToken.None);
            result.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task RequestPageSize_IsRespected()
        {
            var request = new DummyPagedRequest
            {
                Logger = logger,
                PageSize = 4
            };

            // Full page of data
            SetupRequest("api/dummy?p=1&ps=4", $@"
{{
  ""paging"": {{
    ""pageIndex"": 1,
    ""pageSize"": 4,
    ""total"": 10
  }},
  ""dummyResponses"": [
    {string.Join(",\n", Enumerable.Range(1, 4).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            // Full page of data
            SetupRequest("api/dummy?p=2&ps=4", $@"
{{
  ""paging"": {{
    ""pageIndex"": 2,
    ""pageSize"": 4,
    ""total"": 10
  }},
  ""dummyResponses"": [
    {string.Join(",\n", Enumerable.Range(5, 4).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            // Partial page of data - should stop
            SetupRequest("api/dummy?p=3&ps=4", $@"
{{
  ""paging"": {{
    ""pageIndex"": 3,
    ""pageSize"": 2,
    ""total"": 10
  }},
  ""dummyResponses"": [
    {string.Join(",\n", Enumerable.Range(9, 2).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            // Additional page - should never be requested
            SetupRequest("api/dummy?p=4&ps=4", $@"
{{
  ""paging"": {{
    ""pageIndex"": 4,
    ""pageSize"": 2,
    ""total"": 10
  }},
  ""dummyResponses"": [
    {string.Join(",\n", Enumerable.Range(11, 2).Select(i => $@"{{ ""key"": ""{i}"", ""name"": ""Name{i}"" }}"))}
  ]
}}");

            var result = await request.InvokeAsync(client, CancellationToken.None);

            // Should stop after two pages of data
            result.Should().HaveCount(10);
        }

        private void SetupRequest(string relativePath, string response, HttpStatusCode statusCode = HttpStatusCode.OK) =>
            SetupHttpRequest(messageHandler, relativePath, response, statusCode, ValidBaseAddress);

        #region Dummy request and response objects

        private class DummyPagedRequest : PagedRequestBase<DummyObject>
        {
            protected override string Path => "api/dummy";

            protected override DummyObject[] ParseResponse(string response) =>
                JObject.Parse(response)["dummyResponses"]
                .ToObject<DummyResponse[]>()
                .Select(ToDummyObject)
                .ToArray();


            private DummyObject ToDummyObject(DummyResponse dummyResponse) =>
                new DummyObject(dummyResponse.Key, dummyResponse.Name);

            private class DummyResponse
            {
                [JsonProperty("key")]
                public string Key { get; set; }

                [JsonProperty("name")]
                public string Name { get; set; }
            }
        }

        /// <summary>
        /// Data object returned by the request
        /// </summary>
        private class DummyObject
        {
            public DummyObject(string key, string name)
            {
                Key = key;
                Name = name;
            }

            public string Key { get; }
            public string Name { get; }
        }

        #endregion
    }
}

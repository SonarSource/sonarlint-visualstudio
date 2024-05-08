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

using System.Net.Http;
using SonarQube.Client.Api.V9_7;
using static SonarQube.Client.Tests.Infra.MocksHelper;

namespace SonarQube.Client.Tests.Requests.Api.V9_7
{
    [TestClass]
    public class SearchHotspotRequestTests
    {
        [TestMethod]
        public async Task InvokeAsync_NormalizesPath()
        {
            var projectKey = "ConsoleAppTest";
            var branch = "main";

            var testSubject = new SearchHotspotRequest { ProjectKey = projectKey, BranchKey = branch, Logger = new TestLogger() };

            var request = $"api/hotspots/search?projectKey={projectKey}&branch={branch}&p=1&ps=500";

            var response = @"{
    ""paging"": {
        ""pageIndex"": 1,
        ""pageSize"": 500,
        ""total"": 3
    },
    ""hotspots"": [
        {
            ""key"": ""AYeAEDJo4iDoE9Luo7Y9"",
            ""component"": ""ConsoleAppTest:oldproject/Program.cs"",
            ""project"": ""ConsoleAppTest"",
            ""securityCategory"": ""insecure-conf"",
            ""vulnerabilityProbability"": ""LOW"",
            ""status"": ""TO_REVIEW"",
            ""line"": 9,
            ""message"": ""Make sure creating this cookie without setting the 'Secure' property is safe here."",
            ""author"": """",
            ""creationDate"": ""2023-04-14T13:58:47+0000"",
            ""updateDate"": ""2023-04-14T13:58:47+0000"",
            ""textRange"": {
                ""startLine"": 9,
                ""endLine"": 9,
                ""startOffset"": 34,
                ""endOffset"": 68
            },
            ""flows"": [],
            ""ruleKey"": ""csharpsquid:S2092"",
            ""messageFormattings"": []
        },
        {
            ""key"": ""AYgqCWBb8zNecLmhXpkj"",
            ""component"": ""ConsoleAppTest:WebApplication1/Controllers/HomeController.cs"",
            ""project"": ""ConsoleAppTest"",
            ""securityCategory"": ""others"",
            ""vulnerabilityProbability"": ""LOW"",
            ""status"": ""TO_REVIEW"",
            ""line"": 77,
            ""message"": ""Make sure using this hardcoded IP address '192.168.12.42' is safe here."",
            ""author"": """",
            ""creationDate"": ""2023-05-17T14:06:47+0000"",
            ""updateDate"": ""2023-05-17T14:06:47+0000"",
            ""textRange"": {
                ""startLine"": 77,
                ""endLine"": 77,
                ""startOffset"": 21,
                ""endOffset"": 36
            },
            ""flows"": [],
            ""ruleKey"": ""csharpsquid:S1313"",
            ""messageFormattings"": []
        },
        {
            ""key"": ""AYeAEDJo4iDoE9Luo7Y8"",
            ""component"": ""ConsoleAppTest:oldproject/Program.cs"",
            ""project"": ""ConsoleAppTest"",
            ""securityCategory"": ""others"",
            ""vulnerabilityProbability"": ""LOW"",
            ""status"": ""TO_REVIEW"",
            ""line"": 9,
            ""message"": ""Make sure creating this cookie without the \""HttpOnly\"" flag is safe."",
            ""author"": """",
            ""creationDate"": ""2023-04-14T13:58:47+0000"",
            ""updateDate"": ""2023-04-14T13:58:47+0000"",
            ""textRange"": {
                ""startLine"": 9,
                ""endLine"": 9,
                ""startOffset"": 34,
                ""endOffset"": 68
            },
            ""flows"": [],
            ""ruleKey"": ""csharpsquid:S3330"",
            ""messageFormattings"": []
        }
    ],
    ""components"": [
        {
            ""key"": ""ConsoleAppTest"",
            ""qualifier"": ""TRK"",
            ""name"": ""ConsoleAppTest"",
            ""longName"": ""ConsoleAppTest""
        },
        {
            ""key"": ""ConsoleAppTest:WebApplication1/Controllers/HomeController.cs"",
            ""qualifier"": ""FIL"",
            ""name"": ""HomeController.cs"",
            ""longName"": ""WebApplication1/Controllers/HomeController.cs"",
            ""path"": ""WebApplication1/Controllers/HomeController.cs""
        },
        {
            ""key"": ""ConsoleAppTest:oldproject/Program.cs"",
            ""qualifier"": ""FIL"",
            ""name"": ""Program.cs"",
            ""longName"": ""oldproject/Program.cs"",
            ""path"": ""oldproject/Program.cs""
        }
    ]
}";

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            SetupHttpRequest(handlerMock, request, response);

            var results = await testSubject.InvokeAsync(httpClient, CancellationToken.None);

            results.Length.Should().Be(3);

            results[0].FilePath.Should().Be("oldproject\\Program.cs");
            results[1].FilePath.Should().Be("WebApplication1\\Controllers\\HomeController.cs");
            results[2].FilePath.Should().Be("oldproject\\Program.cs");
        }
    }
}

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
    public class SonarQubeService_SearchFilesByName : SonarQubeService_TestBase
    {
        [TestMethod]
        public void SearchFilesByName_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task> action = async () => await service.SearchFilesByNameAsync("projectKey", "branch", "fileName", CancellationToken.None);

            action.Should().ThrowExactly<InvalidOperationException>()
                .WithMessage("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }

        [TestMethod]
        public async Task SearchFilesByName_WrongBranch()
        {
            await ConnectToSonarQube("9.9.0.0");

            var branch = "branch";
            var projectKey = "projectKey";
            var fileName = "fileName.cs";

            var request = $"api/components/tree?component={projectKey}&branch={branch}&q={fileName}&qualifiers=FIL%2CUTS&p=1&ps=500";

            var response = "{\r\n    \"errors\": [\r\n        {\r\n            \"msg\": \"Component 'xyz' on branch 'none' not found\"\r\n        }\r\n    ]\r\n}";

            SetupRequest(request, response, HttpStatusCode.NotFound);

            var func = async () => await service.SearchFilesByNameAsync(projectKey, branch, fileName, CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task SearchFilesByName_SingleFile_Returns()
        {
            await ConnectToSonarQube("9.9.0.0");

            var branch = "master";
            var projectKey = "sonarlint-visualstudio";
            var fileName = "LocalHotspotStoreTests.cs";

            var request = $"api/components/tree?component={projectKey}&branch={branch}&q={fileName}&qualifiers=FIL%2CUTS&p=1&ps=500";

            var response = @"{
    ""paging"": {
        ""pageIndex"": 1,
        ""pageSize"": 100,
        ""total"": 1
    },
    ""baseComponent"": {
        ""organization"": ""sonarsource"",
        ""key"": ""sonarlint-visualstudio"",
        ""name"": ""SonarLint for Visual Studio"",
        ""qualifier"": ""TRK"",
        ""tags"": [
            ""dotnet""
        ],
        ""visibility"": ""public""
    },
    ""components"": [
        {
            ""organization"": ""sonarsource"",
            ""key"": ""sonarlint-visualstudio:src/IssueViz.Security.UnitTests/Hotspots/LocalHotspotStoreTests.cs"",
            ""name"": ""LocalHotspotStoreTests.cs"",
            ""qualifier"": ""UTS"",
            ""path"": ""src/IssueViz.Security.UnitTests/Hotspots/LocalHotspotStoreTests.cs"",
            ""language"": ""cs""
        }
    ]
}";
            SetupRequest(request, response);

            var filePaths = await service.SearchFilesByNameAsync(projectKey, branch, fileName, CancellationToken.None);

            filePaths.Should().ContainSingle();
            filePaths.Should().HaveElementAt(0, "src/IssueViz.Security.UnitTests/Hotspots/LocalHotspotStoreTests.cs");
        }

        [TestMethod]
        public async Task SearchFilesByName_MultipleFiles_Returns()
        {
            await ConnectToSonarQube("9.9.0.0");

            var branch = "master";
            var projectKey = "sonarlint-visualstudio";
            var fileName = "LocalHotspotStoreTests.cs";

            var request = $"api/components/tree?component={projectKey}&branch={branch}&q={fileName}&qualifiers=FIL%2CUTS&p=1&ps=500";

            var response = @"{
    ""paging"": {
        ""pageIndex"": 1,
        ""pageSize"": 100,
        ""total"": 1
    },
    ""baseComponent"": {
        ""organization"": ""sonarsource"",
        ""key"": ""sonarlint-visualstudio"",
        ""name"": ""SonarLint for Visual Studio"",
        ""qualifier"": ""TRK"",
        ""tags"": [
            ""dotnet""
        ],
        ""visibility"": ""public""
    },
    ""components"": [
        {
            ""organization"": ""sonarsource"",
            ""key"": ""sonarlint-visualstudio:path0/LocalHotspotStoreTests.cs"",
            ""name"": ""LocalHotspotStoreTests.cs"",
            ""qualifier"": ""UTS"",
            ""path"": ""path0/LocalHotspotStoreTests.cs"",
            ""language"": ""cs""
        },
        {
            ""organization"": ""sonarsource"",
            ""key"": ""sonarlint-visualstudio:path1/LocalHotspotStoreTests.cs"",
            ""name"": ""LocalHotspotStoreTests.cs"",
            ""qualifier"": ""UTS"",
            ""path"": ""path1/LocalHotspotStoreTests.cs"",
            ""language"": ""cs""
        },
        {
            ""organization"": ""sonarsource"",
            ""key"": ""sonarlint-visualstudio:path2/LocalHotspotStoreTests.cs"",
            ""name"": ""LocalHotspotStoreTests.cs"",
            ""qualifier"": ""UTS"",
            ""path"": ""path2/LocalHotspotStoreTests.cs"",
            ""language"": ""cs""
        },
    ]
}";
            SetupRequest(request, response);

            var filePaths = await service.SearchFilesByNameAsync(projectKey, branch, fileName, CancellationToken.None);

            filePaths.Should().HaveCount(3);
            filePaths.Should().HaveElementAt(0, "path0/LocalHotspotStoreTests.cs");
            filePaths.Should().HaveElementAt(1, "path1/LocalHotspotStoreTests.cs");
            filePaths.Should().HaveElementAt(2, "path2/LocalHotspotStoreTests.cs");
        }
    }
}

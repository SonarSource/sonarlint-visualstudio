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

using System;
using System.Collections.Generic;
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
    public class SonarQubeService_GetProjectBranchesAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task Get_V6_6()
        {
            await ConnectToSonarQube("6.6.0.0");

            const string response = @"{
  ""branches"": [
    {
      ""name"": ""feature/foo"",
      ""isMain"": false,
      ""type"": ""SHORT"",
      ""status"": {
        ""qualityGateStatus"": ""OK""
      },
      ""analysisDate"": ""2017-04-03T13:37:00+0100"",
      ""excludedFromPurge"": false
    },
    {
      ""name"": ""master"",
      ""isMain"": true,
      ""type"": ""LONG"",
      ""status"": {
        ""qualityGateStatus"": ""ERROR""
      },
      ""analysisDate"": ""2018-12-01T01:15:42-0400"",
      ""excludedFromPurge"": true
    }
  ]
}";
            SetupRequest("api/project_branches/list?project=my_project",
                response);

            var result = await service.GetProjectBranchesAsync("my_project", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(2);
            result[0].Name.Should().Be("feature/foo");
            result[0].IsMain.Should().Be(false);
            result[0].Type.Should().Be("SHORT");
            result[0].LastAnalysisTimestamp.Should().Be(new DateTimeOffset(2017, 4, 3, 13, 37, 0, TimeSpan.FromHours(1)));

            result[1].Name.Should().Be("master");
            result[1].IsMain.Should().Be(true);
            result[0].Type.Should().Be("LONG");
            result[1].LastAnalysisTimestamp.Should().Be(new DateTimeOffset(2018, 12, 1, 1, 15, 42, TimeSpan.FromHours(-4)));
        }

        [TestMethod]
        public async Task Get_NotFound()
        {
            await ConnectToSonarQube("6.6.0.0");

            SetupRequest("api/project_branches/list?project=missing", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeProjectBranch>>> func = async () =>
                await service.GetProjectBranchesAsync("missing", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public void Get_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<IList<SonarQubeProjectBranch>>> func = async () =>
                await service.GetProjectBranchesAsync("any", CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}

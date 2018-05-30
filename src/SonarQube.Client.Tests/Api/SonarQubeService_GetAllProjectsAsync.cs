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
        public async Task GetProjects_ExampleFromSonarQube()
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
        public async Task GetProjects_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("api/projects/index", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeProject>>> func = async () =>
                await service.GetAllProjectsAsync("myorganization", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}

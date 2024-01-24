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
    public class SonarQubeService_GetExclusionsRequest : SonarQubeService_TestBase
    {
        [TestMethod]
        public void Get_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<ServerExclusions>> func = async () =>
                await service.GetServerExclusions("any", CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }

        [TestMethod]
        public async Task Get_NotFound()
        {
            await ConnectToSonarQube("7.2.0.0");

            const string request = "api/settings/values?component=my_project&keys=sonar.exclusions%2Csonar.global.exclusions%2Csonar.inclusions";

            SetupRequest(request, "", HttpStatusCode.NotFound);

            Func<Task<ServerExclusions>> func = async () =>
                await service.GetServerExclusions("my_project", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task Get_V7_2_ExampleFromSonarQube()
        {
            await ConnectToSonarQube("7.2.0.0");

            const string response = "{\"settings\":[{\"key\":\"sonar.global.exclusions\",\"values\":[\"**/build-wrapper-dump.json\"],\"inherited\":true}]}";
            const string request = "api/settings/values?component=my_project&keys=sonar.exclusions%2Csonar.global.exclusions%2Csonar.inclusions";

            SetupRequest(request, response);

            var result = await service.GetServerExclusions("my_project", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Inclusions.Should().BeEmpty();
            result.Exclusions.Should().BeEmpty();
            result.GlobalExclusions.Should().BeEquivalentTo("**/build-wrapper-dump.json");
        }
    }
}

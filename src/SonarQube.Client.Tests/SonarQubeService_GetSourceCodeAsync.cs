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

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetSourceCodeAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task Get_V5_0_ExampleFromSonarQube()
        {
            await ConnectToSonarQube("5.0.0.0");

            const string sourceCode = @"a
b

ccc";

            SetupRequest("api/sources/raw?key=my_project%3Asrc%2Ffoo%2FBar.php",
                sourceCode);

            var result = await service.GetSourceCodeAsync("my_project:src/foo/Bar.php", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().Be(sourceCode);
        }

        [TestMethod]
        public async Task Get_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("api/sources/raw?key=missing", "", HttpStatusCode.NotFound);

            Func<Task<string>> func = async () =>
                await service.GetSourceCodeAsync("missing", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public void Get_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<string>> func = async () =>
                await service.GetSourceCodeAsync("any", CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}

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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Services
{
    [TestClass]
    public class SonarQubeService_GetSuppressedIssuesAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetSuppressedIssuesAsync_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("batch/issues?key=project1",
                new StreamReader(@"TestResources\IssuesProtobufResponse").ReadToEnd());

            var result = await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            // TODO: create a protobuf file with more than one issue with different states
            // the one above does not have suppressed issues
            result.Should().HaveCount(0);

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("batch/issues?key=project1", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeIssue>>> func = async () =>
                await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}

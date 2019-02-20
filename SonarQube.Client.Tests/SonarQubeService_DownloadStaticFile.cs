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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_DownloadStaticFile : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task DownloadStaticFile_Response_From_SonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("static/csharp/file1.txt", @"that's the content of the file");

            var result = await service.DownloadStaticFileAsync("csharp", "file1.txt", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();

            using (var reader = new StreamReader(result))
            {
                reader.ReadToEnd().Should().Be("that's the content of the file");
            }
        }

        [TestMethod]
        public async Task DownloadStaticFile_File_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("static/csharp/file1.txt", "", HttpStatusCode.NotFound);

            Func<Task<Stream>> func = async () =>
                await service.DownloadStaticFileAsync("csharp", "file1.txt", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public void DownloadStaticFile_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<Stream>> func = async () =>
                await service.DownloadStaticFileAsync(null, null, CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}

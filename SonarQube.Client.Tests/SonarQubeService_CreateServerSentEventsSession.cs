/*
 * SonarQube Client
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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models.ServerSentEvents;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_CreateServerSentEventsSession : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task CreateServerSentEventsSession_SupportedServerVersion_CreatesSession()
        {
            await ConnectToSonarQube("9.4.0.0");

            SetupRequest("api/push/sonarlint_events?languages=cs%2Cvbnet%2Ccpp%2Cc%2Cjs%2Cts&projectKeys=myProject",
                new HttpResponseMessage
                {
                    Content = new StreamContent(Stream.Null)
                },
                MediaTypeHeaderValue.Parse("text/event-stream"));

            var result = await service.CreateServerSentEventsSession("myProject", CancellationToken.None);

            result.Should().NotBeNull();
        }

        [TestMethod]
        public async Task CreateServerSentEventsSession_UnsupportedServerVersion_InvalidOperationException()
        {
            await ConnectToSonarQube("3.3.0.0");

            Func<Task<IServerSentEventsSession>> func = async () => await service.CreateServerSentEventsSession("myProject", CancellationToken.None);

            const string expectedErrorMessage =
                "Could not find compatible implementation of 'IGetSonarLintEventStream' for SonarQube 3.3.0.0.";

            func.Should().ThrowExactly<InvalidOperationException>().WithMessage(expectedErrorMessage);

            logger.ErrorMessages.Should().Contain(expectedErrorMessage);
        }

        [TestMethod]
        public void CreateServerSentEventsSession_NotConnected_InvalidOperationException()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<IServerSentEventsSession>> func = async () => await service.CreateServerSentEventsSession("myProject", CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>()
                .WithMessage("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}

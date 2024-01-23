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
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Models.ServerSentEvents;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_CreateSSEStreamReader : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task CreateSSEStreamReader_SupportedServerVersion_CreatesSession()
        {
            var expectedCreatedSSEStream = Mock.Of<ISSEStreamReader>();

            sseStreamFactory
                .Setup(x => x.Create(It.IsAny<Stream>(), CancellationToken.None))
                .Returns(expectedCreatedSSEStream);

            await ConnectToSonarQube("9.4.0.0");

            SetupRequest("api/push/sonarlint_events?languages=cs%2Cvbnet%2Ccpp%2Cc%2Cjs%2Cts%2Csecrets%2Ccss&projectKeys=myProject",
                new HttpResponseMessage
                {
                    Content = new StreamContent(Stream.Null)
                },
                MediaTypeHeaderValue.Parse("text/event-stream"));

            var result = await service.CreateSSEStreamReader("myProject", CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().Be(expectedCreatedSSEStream);
        }

        [TestMethod]
        public async Task CreateSSEStreamReader_UnsupportedServerVersion_InvalidOperationException()
        {
            await ConnectToSonarQube("3.3.0.0");

            Func<Task<ISSEStreamReader>> func = async () => await service.CreateSSEStreamReader("myProject", CancellationToken.None);

            const string expectedErrorMessage =
                "Could not find compatible implementation of 'IGetSonarLintEventStream' for SonarQube 3.3.0.0.";

            func.Should().ThrowExactly<InvalidOperationException>().WithMessage(expectedErrorMessage);

            logger.ErrorMessages.Should().Contain(expectedErrorMessage);
        }

        [TestMethod]
        public void CreateSSEStreamReader_NotConnected_InvalidOperationException()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<ISSEStreamReader>> func = async () => await service.CreateSSEStreamReader("myProject", CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>()
                .WithMessage("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}

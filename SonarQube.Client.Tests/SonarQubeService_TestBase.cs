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
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using SonarQube.Client.Requests;
using SonarQube.Client.Models;
using SonarQube.Client.Api;

namespace SonarQube.Client.Tests
{
    public class SonarQubeService_TestBase
    {
        protected Mock<HttpMessageHandler> messageHandler;
        protected SonarQubeService service;
        protected TestLogger logger;

        private RequestFactory requestFactory;

        private static readonly Uri BasePath = new Uri("http://localhost");

        private const string UserAgent = "the-test-user-agent/1.0";

        [TestInitialize]
        public void TestInitialize()
        {
            logger = new TestLogger();

            messageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            requestFactory = new RequestFactory(logger);
            DefaultConfiguration.Configure(requestFactory);

            service = new SonarQubeService(messageHandler.Object, requestFactory, UserAgent, logger);
        }

        protected void SetupRequest(string relativePath, string response, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(m =>
                        m.RequestUri == new Uri(BasePath, relativePath) &&
                        m.Headers.UserAgent.ToString() == UserAgent), // UserAgent should be always sent
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(response)
                }));
        }

        protected async Task ConnectToSonarQube(string version = "5.6.0.0")
        {
            SetupRequest("api/server/version", version);
            SetupRequest("api/authentication/validate", "{ \"valid\": true}");

            await service.ConnectAsync(
                new ConnectionInformation(BasePath, "valeri", new SecureString()),
                CancellationToken.None);

            // Sanity checks
            service.IsConnected.Should().BeTrue();
            logger.InfoMessages.Should().Contain(
                new[]
                {
                    "Connecting to 'http://localhost/'.",
                    $"Connected to SonarQube '{version}'.",
                });
        }
    }
}

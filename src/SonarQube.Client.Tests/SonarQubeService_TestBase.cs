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

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using Moq.Protected;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Tests
{
    public class SonarQubeService_TestBase
    {
        protected Mock<HttpMessageHandler> messageHandler;
        protected SonarQubeService service;
        protected TestLogger logger;

        // Note: can't be protected because the interfaces are internal
        internal Mock<ISecondaryIssueHashUpdater> secondaryIssueHashUpdater;
        internal IRequestFactorySelector requestFactorySelector;
        internal Mock<ISSEStreamReaderFactory> sseStreamFactory;

        private const string DefaultBasePath = "http://localhost/";

        protected const string UserAgent = "the-test-user-agent/1.0";

        [TestInitialize]
        public void TestInitialize()
        {
            // Ensure exception messages are not platform dependent to not break assertions on non english platforms
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            logger = new TestLogger();

            messageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            requestFactorySelector = new RequestFactorySelector();
            secondaryIssueHashUpdater = new Mock<ISecondaryIssueHashUpdater>();
            sseStreamFactory = new Mock<ISSEStreamReaderFactory>();

            ResetService();
        }

        protected void SetupRequest(string relativePath, string response, HttpStatusCode statusCode = HttpStatusCode.OK, string serverUrl = DefaultBasePath) =>
            MocksHelper.SetupHttpRequest(messageHandler, relativePath, response, statusCode, serverUrl);

        protected void SetupRequest(string relativePath, HttpResponseMessage response, params MediaTypeHeaderValue[] expectedHeaderValues) =>
            MocksHelper.SetupHttpRequest(messageHandler, relativePath, response, DefaultBasePath, expectedHeaderValues);

        protected void SetupRequestWithOperation(string relativePath, Func<Task<HttpResponseMessage>> op, string serverUrl = DefaultBasePath)
        {
            messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(m =>
                        m.RequestUri == new Uri(new Uri(serverUrl), relativePath) &&
                        m.Headers.UserAgent.ToString() == UserAgent), // UserAgent should be always sent
                    ItExpr.IsAny<CancellationToken>())
                .Returns(op);
        }

        protected async Task ConnectToSonarQube(string version = "5.6.0.0", string serverUrl = DefaultBasePath)
        {
            SetupRequest("api/server/version", version, serverUrl: serverUrl);
            SetupRequest("api/authentication/validate", "{ \"valid\": true}", serverUrl: serverUrl);

            await service.ConnectAsync(
                new ConnectionInformation(new Uri(serverUrl), "valeri", new SecureString()),
                CancellationToken.None);

            // Sanity checks
            service.IsConnected.Should().BeTrue();

            service.GetServerInfo().Version.Should().Be(new Version(version));
            logger.InfoMessages.Should().Contain(
                x => x.StartsWith($"Connecting to '{serverUrl}", StringComparison.OrdinalIgnoreCase));

            var serverTypeText = service.GetServerInfo().ServerType == ServerType.SonarCloud ? "SonarCloud" : "SonarQube";

            logger.InfoMessages.Should().Contain(
                x => x.StartsWith($"Connected to {serverTypeText} '{version}'."));
        }

        protected void ResetService()
        {
            messageHandler.Reset();
            service = CreateTestSubject();
        }

        protected internal virtual SonarQubeService CreateTestSubject()
        {
            return new SonarQubeService(messageHandler.Object, UserAgent, logger, requestFactorySelector, secondaryIssueHashUpdater.Object, sseStreamFactory.Object);
        }
    }
}

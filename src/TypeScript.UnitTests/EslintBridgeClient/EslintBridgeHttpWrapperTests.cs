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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using HttpResponseMessage = System.Net.Http.HttpResponseMessage;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class EslintBridgeHttpWrapperTests
    {
        private static readonly Uri TestUri = new Uri("http://localhost:1234/some-uri");

        [TestMethod]
        public async Task PostAsync_ExecutesRequestsOnTheUrl()
        {
            Uri requestUri = null;

            var httpMessageHandler = SetupHttpMessageHandler("some response", message =>
            {
                requestUri = message.RequestUri;
            });

            var testSubject = CreateTestSubject(httpMessageHandler);

            await testSubject.PostAsync(TestUri, null, CancellationToken.None);

            requestUri.Should().BeEquivalentTo(TestUri);
        }

        [TestMethod]
        public async Task PostAsync_PassesCancellationToken()
        {
            var validationPassed = false;
            var httpMessageHandler = new Mock<HttpMessageHandler>();
            var originalTokenSource = new CancellationTokenSource();

            httpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage {Content = new StringContent("response")})
                .Callback((HttpRequestMessage message, CancellationToken receivedToken) =>
                {
                    // SendAsync uses `CancellationTokenSource.CreateLinkedTokenSource`
                    receivedToken.IsCancellationRequested.Should().BeFalse();
                    originalTokenSource.Cancel();
                    receivedToken.IsCancellationRequested.Should().BeTrue();
                    validationPassed = true;
                })
                .Verifiable();

            var testSubject = CreateTestSubject(httpMessageHandler: httpMessageHandler.Object);

            await testSubject.PostAsync(TestUri, null, originalTokenSource.Token);

            httpMessageHandler.VerifyAll();
            validationPassed.Should().BeTrue();
        }

        [TestMethod]
        public async Task PostAsync_NullContent_SendsRequestWithEmptyContent()
        {
            string requestContentAsString = null;

            var httpMessageHandler = SetupHttpMessageHandler("some response", message =>
            {
                requestContentAsString = message.Content.ReadAsStringAsync().Result;
            });

            var testSubject = CreateTestSubject(httpMessageHandler: httpMessageHandler);

            var response = await testSubject.PostAsync(TestUri, null, CancellationToken.None);
            response.Should().Be("some response");

            requestContentAsString.Should().BeEmpty();
        }

        [TestMethod]
        public async Task PostAsync_HasContent_SendsRequestWithSerializedContent()
        {
            string requestContentAsString = null;

            var httpMessageHandler = SetupHttpMessageHandler("some response", message =>
            {
                requestContentAsString = message.Content.ReadAsStringAsync().Result;
            });

            var testSubject = CreateTestSubject(httpMessageHandler: httpMessageHandler);
            var requestContent = new { someProp = "some data" };

            var response = await testSubject.PostAsync(TestUri, requestContent, CancellationToken.None);
            response.Should().Be("some response");

            requestContentAsString.Should().Be(JsonConvert.SerializeObject(requestContent, Formatting.Indented));
        }

        [TestMethod]
        public async Task PostAsync_Exception_ExceptionNotCaught()
        {
            var httpMessageHandler = new FakeHttpMessageHandler(message =>
                throw new NotImplementedException());

            var testSubject = CreateTestSubject(httpMessageHandler);

            Func<Task> act = async () => await testSubject.PostAsync(TestUri, null, CancellationToken.None);
            await act.Should().ThrowAsync<NotImplementedException>();
        }

        [TestMethod]
        public async Task GetAsync_ExecutesRequestsOnTheUrl()
        {
            Uri requestUri = null;

            var httpMessageHandler = SetupHttpMessageHandler("some response", message =>
            {
                requestUri = message.RequestUri;
            });

            var testSubject = CreateTestSubject(httpMessageHandler);

            var response = await testSubject.GetAsync(TestUri, CancellationToken.None);

            requestUri.Should().BeEquivalentTo(TestUri);
            response.Should().Be("some response");
        }

        [TestMethod]
        public async Task GetAsync_PassesCancellationToken()
        {
            var validationPassed = false;
            var httpMessageHandler = new Mock<HttpMessageHandler>();
            var originalTokenSource = new CancellationTokenSource();

            httpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("response") })
                .Callback((HttpRequestMessage message, CancellationToken receivedToken) =>
                {
                    // SendAsync uses `CancellationTokenSource.CreateLinkedTokenSource`
                    receivedToken.IsCancellationRequested.Should().BeFalse();
                    originalTokenSource.Cancel();
                    receivedToken.IsCancellationRequested.Should().BeTrue();
                    validationPassed = true;
                })
                .Verifiable();

            var testSubject = CreateTestSubject(httpMessageHandler: httpMessageHandler.Object);

            await testSubject.GetAsync(TestUri, originalTokenSource.Token);

            httpMessageHandler.VerifyAll();
            validationPassed.Should().BeTrue();
        }

        [TestMethod]
        public async Task GetAsync_Exception_ExceptionNotCaught()
        {
            var httpMessageHandler = new FakeHttpMessageHandler(message =>
                throw new NotImplementedException());

            var testSubject = CreateTestSubject(httpMessageHandler);

            Func<Task> act = async () => await testSubject.GetAsync(TestUri, CancellationToken.None);
            await act.Should().ThrowAsync<NotImplementedException>();
        }

        private FakeHttpMessageHandler SetupHttpMessageHandler(string response, Action<HttpRequestMessage> assertReceivedMessage = null)
        {
            var httpMessageHandler = new FakeHttpMessageHandler(message =>
            {
                assertReceivedMessage?.Invoke(message);

                return new HttpResponseMessage { Content = new StringContent(response) };
            });

            return httpMessageHandler;
        }

        private EslintBridgeHttpWrapper CreateTestSubject(HttpMessageHandler httpMessageHandler = null, ILogger logger = null)
        {
            httpMessageHandler ??= SetupHttpMessageHandler("some response");
            logger ??= Mock.Of<ILogger>();

            return new EslintBridgeHttpWrapper(httpMessageHandler, logger);
        }
    }
}

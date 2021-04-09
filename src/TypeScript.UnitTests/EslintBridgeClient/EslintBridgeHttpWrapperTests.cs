/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using HttpResponseMessage = System.Net.Http.HttpResponseMessage;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class EslintBridgeHttpWrapperTests
    {
        private static readonly Uri DefaultUri = new Uri("http://localhost:1234/some-uri");

        [TestMethod]
        public async Task PostAsync_ExecutesRequestsOnTheUrl()
        {
            Uri requestUri = null;

            var httpMessageHandler = SetupHttpMessageHandler("some response", message =>
            {
                requestUri = message.RequestUri;
            });

            var testSubject = CreateTestSubject(httpMessageHandler);

            await testSubject.PostAsync(DefaultUri, null, CancellationToken.None);

            requestUri.Should().BeEquivalentTo(DefaultUri);
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

            await testSubject.PostAsync(DefaultUri, null, originalTokenSource.Token);

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

            var response = await testSubject.PostAsync(DefaultUri, null, CancellationToken.None);
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

            var response = await testSubject.PostAsync(DefaultUri, requestContent, CancellationToken.None);
            response.Should().Be("some response");

            requestContentAsString.Should().Be(JsonConvert.SerializeObject(requestContent, Formatting.Indented));
        }

        [TestMethod]
        public async Task PostAsync_FailsToExecuteRequest_ExceptionCaughtAndLogged()
        {
            var httpMessageHandler = new FakeHttpMessageHandler(message =>
                throw new NotImplementedException("this is a test"));
            var logger = new TestLogger();
            var testSubject = CreateTestSubject(httpMessageHandler: httpMessageHandler, logger: logger);

            var response = await testSubject.PostAsync(DefaultUri, null, CancellationToken.None);
            response.Should().BeNull();
            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public async Task PostAsync_AggregateException_ExceptionCaughtAndLogged()
        {
            var httpMessageHandler = new FakeHttpMessageHandler(message =>
                throw new AggregateException(
                    new List<Exception>
                    {
                        new ArgumentNullException("this is a test1"),
                        new NotImplementedException("this is a test2")
                    }));

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(httpMessageHandler, logger: logger);

            var response = await testSubject.PostAsync(DefaultUri, null, CancellationToken.None);
            response.Should().BeNull();
            logger.AssertPartialOutputStringExists("this is a test1");
            logger.AssertPartialOutputStringExists("this is a test2");
        }

        [TestMethod]
        public async Task PostAsync_CriticalException_ExceptionNotCaught()
        {
            var httpMessageHandler = new FakeHttpMessageHandler(message =>
                throw new StackOverflowException());

            var testSubject = CreateTestSubject(httpMessageHandler);

            Func<Task> act = async () => await testSubject.PostAsync(DefaultUri, null, CancellationToken.None);
            await act.Should().ThrowAsync<StackOverflowException>();
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

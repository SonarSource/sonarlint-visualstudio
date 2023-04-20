/*
 * SonarLint for Visual Studio
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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Api.V9_4;
using SonarQube.Client.Logging;
using SonarQube.Client.Tests.Infra;

namespace SonarQube.Client.Tests.Requests.Api.V9_4
{
    [TestClass]
    public class GetSonarLintEventStreamTests
    {
        [TestMethod]
        public async Task InvokeAsync_ReturnsCorrectStream()
        {
            using var testedStream = new MemoryStream(Encoding.UTF8.GetBytes("hello this is a test"));
            var messageHandler = new Mock<HttpMessageHandler>();
            using var httpClient = new HttpClient(messageHandler.Object) { BaseAddress = new Uri("http://localhost") };

            MocksHelper.SetupHttpRequest(
                messageHandler,
                requestRelativePath: "api/push/sonarlint_events?languages=cs%2Cvbnet%2Ccpp%2Cc%2Cjs%2Cts%2Ccss%2Csecrets&projectKeys=someproj",
                responseMessage: new HttpResponseMessage {Content = new StreamContent(testedStream) },
                headers: MediaTypeHeaderValue.Parse("text/event-stream"));

            var testSubject = new GetSonarLintEventStream {ProjectKey = "someproj", Logger = Mock.Of<ILogger>()};

            using var response = await testSubject.InvokeAsync(httpClient, CancellationToken.None);
            response.Should().NotBeNull();
            messageHandler.VerifyAll();

            using var reader = new StreamReader(response, Encoding.UTF8);
            var responseString = await reader.ReadToEndAsync();
            responseString.Should().Be("hello this is a test");
        }
    }
}

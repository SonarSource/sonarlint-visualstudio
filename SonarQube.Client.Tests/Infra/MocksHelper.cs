/*
 * SonarQube Client
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace SonarQube.Client.Tests.Infra
{
    internal static class MocksHelper
    {
        public const string ValidBaseAddress = "http://localhost";

        public const string EmptyGetIssuesResponse = @"{""total"":0,""p"":1,""ps"":10,""paging"":{""pageIndex"":1,""pageSize"":10,""total"":0},""effortTotal"":0,""debtTotal"":0,""issues"":[],""components"":[],""organizations"":[],""facets"":[]}";

        /// <summary>
        /// Sets up the HTTP message handler mock to respond to any request string 
        /// </summary>
        public static void SetupHttpRequest(Mock<HttpMessageHandler> messageHandlerMock, string response,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            messageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(response)
                }));
        }

        /// <summary>
        /// Sets up the HTTP message handler mock to reply to a specific request string
        /// </summary>
        public static void SetupHttpRequest(Mock<HttpMessageHandler> messageHandlerMock, string requestRelativePath, string response,
            HttpStatusCode statusCode = HttpStatusCode.OK, string basePath = ValidBaseAddress)
        {
            var baseUri = new Uri(basePath);
            messageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(m =>
                        m.RequestUri == new Uri(baseUri, requestRelativePath)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(response)
                }));
        }

        /// <summary>
        /// Returns the actual requests passed to the SendAsync method
        /// </summary>
        public static IEnumerable<HttpRequestMessage> GetSendAsyncRequests(this Mock<HttpMessageHandler> handler) =>
            handler.Invocations.Where(x => x.Method.Name == "SendAsync")
                .Select(x => (HttpRequestMessage)x.Arguments[0])
                .ToArray();
    }
}

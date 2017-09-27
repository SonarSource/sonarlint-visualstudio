/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryClientTests
    {
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> sendFunc;

            public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> sendFunc)
            {
                this.sendFunc = sendFunc;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(sendFunc(request));
            }
        }

        [TestMethod]
        public async Task OptOut_WhenMoreThanThreeFailures_ReturnsFalse()
        {
            // Arrange
            var httpHandler = new FakeHttpMessageHandler(x => { throw new Exception(); });
            var client = new TelemetryClient(httpHandler);

            // Act
            var result = await client.OptOut(new TelemetryPayload());

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task OptOut_WhenSuccess_ReturnsTrue()
        {
            // Arrange
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Created);
            var httpHandler = new FakeHttpMessageHandler(x => { return response; });
            var client = new TelemetryClient(httpHandler);

            // Act
            var result = await client.OptOut(new TelemetryPayload());

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task SendPayload_WhenMoreThanThreeFailures_ReturnsFalse()
        {
            // Arrange
            var httpHandler = new FakeHttpMessageHandler(x => { throw new Exception(); });
            var client = new TelemetryClient(httpHandler);

            // Act
            var result = await client.SendPayload(new TelemetryPayload());

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task SendPayload_WhenSuccess_ReturnsTrue()
        {
            // Arrange
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Created);
            var httpHandler = new FakeHttpMessageHandler(x => { return response; });
            var client = new TelemetryClient(httpHandler);

            // Act
            var result = await client.SendPayload(new TelemetryPayload());

            // Assert
            result.Should().BeTrue();
        }
    }
}

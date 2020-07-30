/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

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
            var httpHandler = new FakeHttpMessageHandler(_ => throw new Exception());
            var client = new TelemetryClient(httpHandler, 3, TimeSpan.FromMilliseconds(1));

            // Act
            var result = await client.OptOutAsync(new TelemetryPayload());

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task OptOut_WhenSuccess_ReturnsTrue()
        {
            // Arrange
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Created);
            var httpHandler = new FakeHttpMessageHandler(_ => response);
            var client = new TelemetryClient(httpHandler);

            // Act
            var result = await client.OptOutAsync(new TelemetryPayload());

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task SendPayload_CheckUrl()
        {
            // Arrange
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Created);
            string uriCalled = null;
            var httpHandler = new FakeHttpMessageHandler(request => { uriCalled = request.RequestUri.AbsoluteUri; return response; });
            var client = new TelemetryClient(httpHandler);

            // Act
            var result = await client.SendPayloadAsync(new TelemetryPayload());

            // Assert
            uriCalled.Should().Be("https://telemetry.sonarsource.com/sonarlint");
        }

        [TestMethod]
        public async Task SendPayload_WhenMoreThanThreeFailures_ReturnsFalse()
        {
            // Arrange
            var httpHandler = new FakeHttpMessageHandler(_ => throw new Exception());
            var client = new TelemetryClient(httpHandler, 3, TimeSpan.FromMilliseconds(1));

            // Act
            var result = await client.SendPayloadAsync(new TelemetryPayload());

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task SendPayload_WhenSuccess_ReturnsTrue()
        {
            // Arrange
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Created);
            var httpHandler = new FakeHttpMessageHandler(_ => response);
            var client = new TelemetryClient(httpHandler);

            // Act
            var result = await client.SendPayloadAsync(new TelemetryPayload());

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task Dates_Are_Serialized_In_Roundtrip_Format()
        {
            string serializedRequestPayload = null;
            // Arrange
            var httpHandlerMock = new Mock<HttpMessageHandler>();
            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((r, _) => serializedRequestPayload = r.Content.ReadAsStringAsync().GetAwaiter().GetResult())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

            var client = new TelemetryClient(httpHandlerMock.Object);

            // Act
            var result = await client.SendPayloadAsync(new TelemetryPayload
            {
                // Adding some ticks to ensure that we send just the milliseconds in the serialized payload
                InstallDate = new DateTimeOffset(2017, 12, 23, 8, 25, 35, 456, TimeSpan.FromHours(1)).AddTicks(123),
                SystemDate = new DateTimeOffset(2018, 3, 15, 18, 55, 10, 123, TimeSpan.FromHours(1)).AddTicks(123),
                IsUsingConnectedMode = true,
                IsUsingLegacyConnectedMode = true,
                IsUsingSonarCloud = true,
                NumberOfDaysOfUse = 200,
                NumberOfDaysSinceInstallation = 230,
                SonarLintProduct = "SonarLint for Visual Studio",
                SonarLintVersion = "1.2.3.4",
                VisualStudioVersion = "15.16",
            });

            // Assert
            result.Should().BeTrue();

            const string expected = @"{
  ""sonarlint_product"": ""SonarLint for Visual Studio"",
  ""sonarlint_version"": ""1.2.3.4"",
  ""ide_version"": ""15.16"",
  ""days_since_installation"": 230,
  ""days_of_use"": 200,
  ""connected_mode_used"": true,
  ""legacy_connected_mode_used"": true,
  ""connected_mode_sonarcloud"": true,
  ""install_time"": ""2017-12-23T08:25:35.456+01:00"",
  ""system_time"": ""2018-03-15T18:55:10.123+01:00"",
  ""analyses"": null
}";

            httpHandlerMock.VerifyAll();
            RemoveLineEndings(serializedRequestPayload).Should().Be(RemoveLineEndings(expected));
        }

        private string RemoveLineEndings(string text)
        {
            return text.Replace("\r\n", string.Empty).Replace("\n", string.Empty);
        }
    }
}

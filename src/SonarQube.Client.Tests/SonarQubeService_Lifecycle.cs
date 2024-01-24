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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.Protected;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_Lifecycle : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task Connect_To_SonarQube_Valid_Credentials()
        {
            // The earliest version that supports authentication
            SetupRequest("api/server/version", "3.3.0.0");
            SetupRequest("api/authentication/validate", "{ \"valid\": true }");

            service.IsConnected.Should().BeFalse();
            service.GetServerInfo().Should().BeNull();

            await service.ConnectAsync(
                new ConnectionInformation(new Uri("http://localhost"), "user", "pass".ToSecureString()),
                CancellationToken.None);

            service.IsConnected.Should().BeTrue();
            service.GetServerInfo().Version.Should().Be(new Version("3.3.0.0"));
        }

        [TestMethod]
        public async Task Connect_To_SonarQube_Invalid_Credentials()
        {
            // The earliest version that supports authentication
            SetupRequest("api/server/version", "3.3.0.0");
            SetupRequest("api/authentication/validate", "{ \"valid\": false }");

            service.IsConnected.Should().BeFalse();
            service.GetServerInfo().Should().BeNull();

            Func<Task> act = () => service.ConnectAsync(
                new ConnectionInformation(new Uri("http://localhost"), "user", "pass".ToSecureString()),
                CancellationToken.None);

            var ex = await act.Should().ThrowAsync<InvalidOperationException>();
            ex.WithMessage("Invalid credentials");

            service.IsConnected.Should().BeFalse();
            service.GetServerInfo().Should().BeNull();
        }

        [TestMethod] // Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/2406
        public async Task Connect_ServerIsNotReachable_IsConnectedIsFalse()
        {
            SetupRequest("api/server/version", "3.3.0.0");

            SetupRequestWithOperation("api/server/version",
                () => throw new ApplicationException("Thrown in test"));

            service.IsConnected.Should().BeFalse();
            service.GetServerInfo().Should().BeNull();

            Func<Task> act = () => service.ConnectAsync(
                new ConnectionInformation(new Uri("http://localhost"), "user", "pass".ToSecureString()),
                CancellationToken.None);

            var ex = await act.Should().ThrowAsync<ApplicationException>();
            ex.WithMessage("Thrown in test");

            service.IsConnected.Should().BeFalse();
            service.GetServerInfo().Should().BeNull();
        }

        [TestMethod]
        [DataRow("http://localhost")]
        [DataRow("https://localhost/")]
        [DataRow("https://localhost:9000")]
        public async Task Connect_SonarQube_IsSonarCloud_SonarQubeUrl_ReturnsFalse(string inputUrl)
        {
            var canonicalUrl = inputUrl.TrimEnd('/');

            // The earliest version that supports authentication
            SetupRequest("api/server/version", "3.3.0.0", serverUrl: canonicalUrl);
            SetupRequest("api/authentication/validate", "{ \"valid\": true }", serverUrl: canonicalUrl);

            await service.ConnectAsync(
                new ConnectionInformation(new Uri(inputUrl), "user", "pass".ToSecureString()),
                CancellationToken.None);

            service.GetServerInfo().ServerType.Should().Be(ServerType.SonarQube);
        }

        [TestMethod]
        [DataRow("http://sonarcloud.io")]
        [DataRow("https://sonarcloud.io")]
        [DataRow("http://SONARCLOUD.IO")]
        [DataRow("http://www.sonarcloud.io")]
        public async Task Connect_SonarQube_IsSonarCloud_SonarCloud_ReturnTrue(string inputUrl)
        {
            const string fixedSonarCloudUrl = "https://sonarcloud.io/";

            // The earliest version that supports authentication
            SetupRequest("api/server/version", "3.3.0.0", serverUrl: fixedSonarCloudUrl);
            SetupRequest("api/authentication/validate", "{ \"valid\": true }", serverUrl: fixedSonarCloudUrl);

            await service.ConnectAsync(
                new ConnectionInformation(new Uri(inputUrl), "user", "pass".ToSecureString()),
                CancellationToken.None);

            service.GetServerInfo().ServerType.Should().Be(ServerType.SonarCloud);
        }

        [TestMethod]
        public async Task Disconnect_Does_Not_Dispose_MessageHandler()
        {
            // Regression test for #689 - LoggingMessageHandler is disposed on disconnect

            // Arrange
            var disposed = false;
            messageHandler.Protected().Setup("Dispose", ItExpr.IsAny<bool>()).Callback(() => disposed = true);

            await ConnectToSonarQube();

            // Act. Disconnect should not throw
            service.Disconnect();

            // Assert
            service.IsConnected.Should().BeFalse();
            service.GetServerInfo().Should().BeNull();
            disposed.Should().BeFalse();
        }

        [TestMethod]
        public async Task Dispose_Does_Dispose_MessageHandler()
        {
            // Arrange
            var disposed = false;
            messageHandler.Protected().Setup("Dispose", ItExpr.IsAny<bool>()).Callback(() => disposed = true);

            await ConnectToSonarQube();

            // Act
            service.Dispose();

            // Assert
            service.IsConnected.Should().BeFalse();
            service.GetServerInfo().Should().BeNull();
            disposed.Should().BeTrue();
        }
    }
}

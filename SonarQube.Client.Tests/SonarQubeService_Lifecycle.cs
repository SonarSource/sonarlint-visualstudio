/*
 * SonarQube Client
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using SonarQube.Client.Helpers;

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
            service.SonarQubeVersion.Should().BeNull();

            await service.ConnectAsync(
                new Models.ConnectionInformation(new Uri("http://localhost"), "user", "pass".ToSecureString()),
                CancellationToken.None);

            service.IsConnected.Should().BeTrue();
            service.SonarQubeVersion.Should().Be(new Version("3.3.0.0"));
        }

        [TestMethod]
        public void Connect_To_SonarQube_Invalid_Credentials()
        {
            // The earliest version that supports authentication
            SetupRequest("api/server/version", "3.3.0.0");
            SetupRequest("api/authentication/validate", "{ \"valid\": false }");

            service.IsConnected.Should().BeFalse();
            service.SonarQubeVersion.Should().BeNull();

            Func<Task> action = async () => await service.ConnectAsync(
                new Models.ConnectionInformation(new Uri("http://localhost"), "user", "pass".ToSecureString()),
                CancellationToken.None);

            action.Should().ThrowExactly<InvalidOperationException>()
                .And.Message.Should().Be("Invalid credentials");

            service.IsConnected.Should().BeFalse();
            service.SonarQubeVersion.Should().BeNull();
        }

        [TestMethod]
        public async Task Disconnect_Does_Not_Dispose_MessageHandler()
        {
            // Regression test for #689 - LoggingMessageHandler is disposed on disconnect

            // Arrange
            messageHandler.Protected().Setup("Dispose", true);
            await ConnectToSonarQube();

            // Act. Disconnect should not throw
            service.Disconnect();

            // Assert
            service.IsConnected.Should().BeFalse();
            service.SonarQubeVersion.Should().BeNull();
            messageHandler.Protected().Verify("Dispose", Times.Never(), true);
        }

        [TestMethod]
        public async Task Dispose_Does_Dispose_MessageHandler()
        {
            // Arrange
            messageHandler.Protected().Setup("Dispose", true);
            await ConnectToSonarQube();

            // Act
            service.Dispose();

            // Assert
            service.IsConnected.Should().BeFalse();
            service.SonarQubeVersion.Should().BeNull();
            messageHandler.Protected().Verify("Dispose", Times.Once(), true);
        }
    }
}

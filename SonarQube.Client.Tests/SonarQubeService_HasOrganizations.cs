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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_HasOrganizations : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task HasOrganizations_NotSonarCloud_ReturnsFalse()
        {
            // Version should be irrelevant
            await CheckDoesNotHaveOrganizations("5.6.0.0", "http://localhost");
            await CheckDoesNotHaveOrganizations("6.0.0.0", "https://localhost/");
            await CheckDoesNotHaveOrganizations("6.1.0.0", "https://localhost:9000");
            await CheckDoesNotHaveOrganizations("6.7.0.0", "https://mysonarqubeserver/");
            await CheckDoesNotHaveOrganizations("7.9.0.0", "https://notsonarcloud.io/");
            await CheckDoesNotHaveOrganizations("7.9.0.0", "https://sonarcloud.ion/");
            await CheckDoesNotHaveOrganizations("7.9.0.0", "http://sonarcloud.ion/");
            await CheckDoesNotHaveOrganizations("99.99.0.0", "https://my.org.sonarqube:123");
        }

        [TestMethod]
        public async Task HasOrganizations_SonarCloud_ReturnsTrue()
        {
            // Version should be irrelevant
            await CheckHasOrganizations("5.6.0.0", "https://sonarcloud.io/");
            await CheckHasOrganizations("6.0.0.0", "https://sonarcloud.io/");
            await CheckHasOrganizations("6.1.0.0", "https://sonarcloud.io/");
            await CheckHasOrganizations("6.7.0.0", "https://sonarcloud.io/");
            await CheckHasOrganizations("7.9.0.0", "https://sonarcloud.io/");
            await CheckHasOrganizations("99.99.0.0", "https://sonarcloud.io/");

            // Casing, final slash
            await CheckHasOrganizations("99.99.0.0", "https://sonarcloud.io");
            await CheckHasOrganizations("99.99.0.0", "HTTPS://SONARCLOUD.IO");
            await CheckHasOrganizations("99.99.0.0", "HTTPS://SONARCLOUD.IO/");
        }

        [TestMethod]
        public void HasOrganizationsFeature_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Action action = () => { var result = service.HasOrganizations(CancellationToken.None).Result; };

            action.Should().ThrowExactly<InvalidOperationException>()
                .WithMessage("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }

        private async Task CheckHasOrganizations(string serverVersion, string serverUrl)
        {
            ResetService();
            await ConnectToSonarQube(serverVersion, serverUrl);

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeTrue();
        }

        private async Task CheckDoesNotHaveOrganizations(string serverVersion, string serverUrl)
        {
            ResetService();
            await ConnectToSonarQube(serverVersion, serverUrl);

            var result = await service.HasOrganizations(CancellationToken.None);
            result.Should().BeFalse();
        }
    }
}

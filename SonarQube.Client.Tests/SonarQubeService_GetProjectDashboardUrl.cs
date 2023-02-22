﻿/*
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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetProjectDashboardUrl : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetProjectDashboardUrl_SonarQube_ReturnsExpectedUrl()
        {
            await ConnectToSonarQube("3.3.0.0", "http://localhost:9000");

            var result = service.GetProjectDashboardUrl("myProject");

            result.Should().BeEquivalentTo(new Uri("http://localhost:9000/dashboard/index/myProject"));
        }

        [TestMethod]
        public async Task GetProjectDashboardUrl_SonarCloud_ReturnsExpectedUrl()
        {
            await ConnectToSonarQube("3.3.0.0", serverUrl: "https://sonarcloud.io");

            var result = service.GetProjectDashboardUrl("myProject");

            result.Should().BeEquivalentTo(new Uri("https://sonarcloud.io/project/overview?id=myProject"));
        }

        [TestMethod]
        public void GetProjectDashboardUrl_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Action action = () => service.GetProjectDashboardUrl("myProject");

            action.Should().ThrowExactly<InvalidOperationException>()
                .WithMessage("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}

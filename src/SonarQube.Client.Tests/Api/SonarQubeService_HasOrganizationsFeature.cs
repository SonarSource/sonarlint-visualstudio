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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Tests.Api
{
    public class SonarQubeService_HasOrganizationsFeature : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task HasOrganizationsFeature_5_60()
        {
            await ConnectToSonarQube();

            service.HasOrganizationsFeature.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizationsFeature_6_00()
        {
            await ConnectToSonarQube("6.0.0.0");

            service.HasOrganizationsFeature.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizationsFeature_6_10()
        {
            await ConnectToSonarQube("6.1.0.0");

            service.HasOrganizationsFeature.Should().BeFalse();
        }

        [TestMethod]
        public async Task HasOrganizationsFeature_6_20()
        {
            await ConnectToSonarQube("6.2.0.0");

            service.HasOrganizationsFeature.Should().BeTrue();
        }

        [TestMethod]
        public async Task HasOrganizationsFeature_6_30()
        {
            await ConnectToSonarQube("6.3.0.0");

            service.HasOrganizationsFeature.Should().BeTrue();
        }

        [TestMethod]
        public async Task HasOrganizationsFeature_99_99()
        {
            await ConnectToSonarQube("99.99.0.0");

            service.HasOrganizationsFeature.Should().BeTrue();
        }

        [TestMethod]
        public void HasOrganizationsFeature_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Action action = () => { var result = service.HasOrganizationsFeature; };

            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}

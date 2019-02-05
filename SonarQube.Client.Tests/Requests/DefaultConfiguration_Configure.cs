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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Requests;
using SonarQube.Client.Api;

namespace SonarQube.Client.Tests.Api
{
    [TestClass]
    public class DefaultConfiguration_Configure
    {
        [TestMethod]
        public void Configure_Writes_Debug_Messages()
        {
            var logger = new TestLogger();

            DefaultConfiguration.Configure(new RequestFactory(logger));

            logger.DebugMessages.Should().ContainInOrder(
                new[]
                {
                    "Registered SonarQube.Client.Api.V2_10.GetPluginsRequest for 2.1",
                    "Registered SonarQube.Client.Api.V2_10.GetProjectsRequest for 2.1",
                    "Registered SonarQube.Client.Api.V2_10.GetVersionRequest for 2.1",
                    "Registered SonarQube.Client.Api.V2_60.GetPropertiesRequest for 2.6",
                    "Registered SonarQube.Client.Api.V3_30.ValidateCredentialsRequest for 3.3",
                    "Registered SonarQube.Client.Api.V5_10.GetIssuesRequest for 5.1",
                    "Registered SonarQube.Client.Api.V5_20.GetQualityProfileChangeLogRequest for 5.2",
                    "Registered SonarQube.Client.Api.V5_20.GetQualityProfilesRequest for 5.2",
                    "Registered SonarQube.Client.Api.V5_20.GetRoslynExportProfileRequest for 5.2",
                    "Registered SonarQube.Client.Api.V5_40.GetModulesRequest for 5.4",
                    "Registered SonarQube.Client.Api.V6_20.GetOrganizationsRequest for 6.2",
                    "Registered SonarQube.Client.Api.V6_20.GetProjectsRequest for 6.2",
                    "Registered SonarQube.Client.Api.V6_30.GetPluginsRequest for 6.3",
                    "Registered SonarQube.Client.Api.V6_30.GetPropertiesRequest for 6.3",
                    "Registered SonarQube.Client.Api.V6_50.GetQualityProfileChangeLogRequest for 6.5",
                    "Registered SonarQube.Client.Api.V6_50.GetQualityProfilesRequest for 6.5",
                    "Registered SonarQube.Client.Api.V6_60.GetNotificationsRequest for 6.6",
                    "Registered SonarQube.Client.Api.V6_60.GetRoslynExportProfileRequest for 6.6",
                    "Registered SonarQube.Client.Api.V7_00.GetOrganizationsRequest for 7.0",
                    "Registered SonarQube.Client.Api.V7_20.GetIssuesRequestWrapper for 7.2",
                });
        }
    }
}

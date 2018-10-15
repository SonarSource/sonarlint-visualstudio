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

namespace SonarQube.Client.Api.Requests
{
    public static class DefaultConfiguration
    {
        public static RequestFactory Configure(RequestFactory requestFactory)
        {
            requestFactory
                .RegisterRequest<IGetPluginsRequest, V2_10.GetPluginsRequest>("2.1")
                .RegisterRequest<IGetProjectsRequest, V2_10.GetProjectsRequest>("2.1")
                .RegisterRequest<IGetVersionRequest, V2_10.GetVersionRequest>("2.1")
                .RegisterRequest<IGetPropertiesRequest, V2_60.GetPropertiesRequest>("2.6")
                .RegisterRequest<IValidateCredentialsRequest, V3_30.ValidateCredentialsRequest>("3.3")
                .RegisterRequest<IGetIssuesRequest, V5_10.GetIssuesRequest>("5.1")
                .RegisterRequest<IGetQualityProfileChangeLogRequest, V5_20.GetQualityProfileChangeLogRequest>("5.2")
                .RegisterRequest<IGetQualityProfilesRequest, V5_20.GetQualityProfilesRequest>("5.2")
                .RegisterRequest<IGetRoslynExportProfileRequest, V5_20.GetRoslynExportProfileRequest>("5.2")
                .RegisterRequest<IGetModulesRequest, V5_40.GetModulesRequest>("5.4")
                .RegisterRequest<IGetOrganizationsRequest, V6_20.GetOrganizationsRequest>("6.2")
                .RegisterRequest<IGetProjectsRequest, V6_20.GetProjectsRequest>("6.2")
                .RegisterRequest<IGetPluginsRequest, V6_30.GetPluginsRequest>("6.3")
                .RegisterRequest<IGetPropertiesRequest, V6_30.GetPropertiesRequest>("6.3")
                .RegisterRequest<IGetQualityProfileChangeLogRequest, V6_50.GetQualityProfileChangeLogRequest>("6.5")
                .RegisterRequest<IGetQualityProfilesRequest, V6_50.GetQualityProfilesRequest>("6.5")
                .RegisterRequest<IGetNotificationsRequest, V6_60.GetNotificationsRequest>("6.6")
                .RegisterRequest<IGetRoslynExportProfileRequest, V6_60.GetRoslynExportProfileRequest>("6.6")
                .RegisterRequest<IGetOrganizationsRequest, V7_00.GetOrganizationsRequest>("7.0")
                .RegisterRequest<IGetIssuesRequest, V7_20.GetIssuesRequest>("7.2")
                ;
            return requestFactory;
        }
    }
}

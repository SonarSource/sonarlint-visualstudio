/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarQube.Client.Api.V10_2;
using SonarQube.Client.Api.V2_10;
using SonarQube.Client.Api.V3_30;
using SonarQube.Client.Api.V5_50;
using SonarQube.Client.Api.V6_30;
using SonarQube.Client.Api.V6_50;
using SonarQube.Client.Api.V6_60;
using SonarQube.Client.Api.V7_20;
using SonarQube.Client.Api.V9_4;
using SonarQube.Client.Api.V9_9;
using SonarQube.Client.Requests;
using GetIssuesRequest = SonarQube.Client.Api.V5_10.GetIssuesRequest;

namespace SonarQube.Client.Api;

internal static class DefaultConfiguration
{
    public static RequestFactory ConfigureSonarQube(RequestFactory requestFactory)
    {
        requestFactory
            .RegisterRequest<IGetVersionRequest, GetVersionRequest>("2.1")
            .RegisterRequest<IGetPropertiesRequest, V2_60.GetPropertiesRequest>("2.6")
            .RegisterRequest<IValidateCredentialsRequest, ValidateCredentialsRequest>("3.3")
            .RegisterRequest<IGetIssuesRequest, GetIssuesRequest>("5.1")
            .RegisterRequest<IGetQualityProfilesRequest, V5_20.GetQualityProfilesRequest>("5.2")
            .RegisterRequest<IGetRulesRequest, GetRulesRequest>("5.5")
            .RegisterRequest<IGetPropertiesRequest, GetPropertiesRequest>("6.3")
            .RegisterRequest<IGetQualityProfilesRequest, GetQualityProfilesRequest>("6.5")
            .RegisterRequest<IGetNotificationsRequest, GetNotificationsRequest>("6.6")
            .RegisterRequest<IGetProjectBranchesRequest, GetProjectBranchesRequest>("6.6")
            .RegisterRequest<IGetIssuesRequest, GetIssuesRequestWrapper<GetIssuesWithComponentSonarQubeRequest>>("7.2")
            .RegisterRequest<IGetExclusionsRequest, GetExclusionsRequest>("7.2")
            .RegisterRequest<IGetSonarLintEventStream, GetSonarLintEventStream>("9.4")
            .RegisterRequest<ISearchHotspotRequest, V9_7.SearchHotspotRequest>("9.7")
            .RegisterRequest<ISearchHotspotRequest, SearchHotspotRequest>("10.2")
            .RegisterRequest<IGetRulesRequest, GetRulesWithCCTRequest>("10.2")
            .RegisterRequest<ISearchFilesByNameRequest, SearchFilesByNameRequest>("9.9");

        return requestFactory;
    }

    public static UnversionedRequestFactory ConfigureSonarCloud(UnversionedRequestFactory requestFactory)
    {
        requestFactory
            .RegisterRequest<IGetVersionRequest, GetVersionRequest>()
            .RegisterRequest<IValidateCredentialsRequest, ValidateCredentialsRequest>()
            .RegisterRequest<IGetRulesRequest, GetRulesWithCCTRequest>()
            .RegisterRequest<IGetPropertiesRequest, GetPropertiesRequest>()
            .RegisterRequest<IGetQualityProfilesRequest, GetQualityProfilesRequest>()
            .RegisterRequest<IGetNotificationsRequest, GetNotificationsRequest>()
            .RegisterRequest<IGetProjectBranchesRequest, GetProjectBranchesRequest>()
            .RegisterRequest<IGetIssuesRequest, GetIssuesRequestWrapper<GetIssuesWithComponentSonarCloudRequest>>()
            .RegisterRequest<IGetExclusionsRequest, GetExclusionsRequest>()
            .RegisterRequest<ISearchHotspotRequest, V9_7.SearchHotspotRequest>()
            .RegisterRequest<ISearchFilesByNameRequest, SearchFilesByNameRequest>();

        return requestFactory;
    }
}

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

using SonarQube.Client.Requests;

namespace SonarQube.Client.Api;

internal static class DefaultConfiguration
{
    public static RequestFactory ConfigureSonarQube(RequestFactory requestFactory)
    {
        requestFactory
            .RegisterRequest<IGetVersionRequest, V2_10.GetVersionRequest>("2.1")
            .RegisterRequest<IGetPropertiesRequest, V2_60.GetPropertiesRequest>("2.6")
            .RegisterRequest<IValidateCredentialsRequest, V3_30.ValidateCredentialsRequest>("3.3")
            .RegisterRequest<IGetQualityProfilesRequest, V5_20.GetQualityProfilesRequest>("5.2")
            .RegisterRequest<IGetRulesRequest, V5_50.GetRulesRequest>("5.5")
            .RegisterRequest<IGetPropertiesRequest, V6_30.GetPropertiesRequest>("6.3")
            .RegisterRequest<IGetQualityProfilesRequest, V6_50.GetQualityProfilesRequest>("6.5")
            .RegisterRequest<IGetNotificationsRequest, V6_60.GetNotificationsRequest>("6.6")
            .RegisterRequest<IGetProjectBranchesRequest, V6_60.GetProjectBranchesRequest>("6.6")
            .RegisterRequest<IGetIssuesRequest, V7_20.GetIssuesRequestWrapper<V7_20.GetIssuesWithComponentSonarQubeRequest>>("7.2")
            .RegisterRequest<IGetExclusionsRequest, V7_20.GetExclusionsRequest>("7.2")
            .RegisterRequest<IGetSonarLintEventStream, V9_4.GetSonarLintEventStream>("9.4")
            .RegisterRequest<IGetRulesRequest, V10_2.GetRulesWithCCTRequest>("10.2")
            .RegisterRequest<ISearchFilesByNameRequest, V9_9.SearchFilesByNameRequest>("9.9");

        return requestFactory;
    }

    public static UnversionedRequestFactory ConfigureSonarCloud(UnversionedRequestFactory requestFactory)
    {
        requestFactory
            .RegisterRequest<IGetVersionRequest, V2_10.GetVersionRequest>()
            .RegisterRequest<IValidateCredentialsRequest, V3_30.ValidateCredentialsRequest>()
            .RegisterRequest<IGetRulesRequest, V10_2.GetRulesWithCCTRequest>()
            .RegisterRequest<IGetPropertiesRequest, V6_30.GetPropertiesRequest>()
            .RegisterRequest<IGetQualityProfilesRequest, V6_50.GetQualityProfilesRequest>()
            .RegisterRequest<IGetNotificationsRequest, V6_60.GetNotificationsRequest>()
            .RegisterRequest<IGetProjectBranchesRequest, V6_60.GetProjectBranchesRequest>()
            .RegisterRequest<IGetIssuesRequest, V7_20.GetIssuesRequestWrapper<V7_20.GetIssuesWithComponentSonarCloudRequest>>()
            .RegisterRequest<IGetExclusionsRequest, V7_20.GetExclusionsRequest>()
            .RegisterRequest<ISearchFilesByNameRequest, V9_9.SearchFilesByNameRequest>();

        return requestFactory;
    }
}

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

namespace SonarQube.Client.Api
{
    internal static class DefaultConfiguration
    {
        public static RequestFactory ConfigureSonarQube(RequestFactory requestFactory)
        {
            requestFactory
                .RegisterRequest<IGetPluginsRequest, V2_10.GetPluginsRequest>("2.1")
                .RegisterRequest<IGetProjectsRequest, V2_10.GetProjectsRequest>("2.1")
                .RegisterRequest<IGetVersionRequest, V2_10.GetVersionRequest>("2.1")
                .RegisterRequest<IGetPropertiesRequest, V2_60.GetPropertiesRequest>("2.6")
                .RegisterRequest<IValidateCredentialsRequest, V3_30.ValidateCredentialsRequest>("3.3")
                .RegisterRequest<IGetSourceCodeRequest, V5_00.GetSourceCodeRequest>("5.0")
                .RegisterRequest<IGetIssuesRequest, V5_10.GetIssuesRequest>("5.1")
                .RegisterRequest<IGetLanguagesRequest, V5_10.GetLanguagesRequest>("5.1")
                .RegisterRequest<IGetQualityProfileChangeLogRequest, V5_20.GetQualityProfileChangeLogRequest>("5.2")
                .RegisterRequest<IGetQualityProfilesRequest, V5_20.GetQualityProfilesRequest>("5.2")
                .RegisterRequest<IGetRoslynExportProfileRequest, V5_20.GetRoslynExportProfileRequest>("5.2")
                .RegisterRequest<IGetModulesRequest, V5_40.GetModulesRequest>("5.4")
                .RegisterRequest<IGetRulesRequest, V5_50.GetRulesRequest>("5.5")
                .RegisterRequest<IDownloadStaticFile, V5_50.DownloadStaticFile>("5.5")
                .RegisterRequest<IGetOrganizationsRequest, V6_20.GetOrganizationsRequest>("6.2")
                .RegisterRequest<IGetProjectsRequest, V6_20.GetProjectsRequest>("6.2")
                .RegisterRequest<IGetPluginsRequest, V6_30.GetPluginsRequest>("6.3")
                .RegisterRequest<IGetPropertiesRequest, V6_30.GetPropertiesRequest>("6.3")
                .RegisterRequest<IGetQualityProfileChangeLogRequest, V6_50.GetQualityProfileChangeLogRequest>("6.5")
                .RegisterRequest<IGetQualityProfilesRequest, V6_50.GetQualityProfilesRequest>("6.5")
                .RegisterRequest<IGetNotificationsRequest, V6_60.GetNotificationsRequest>("6.6")
                .RegisterRequest<IGetRoslynExportProfileRequest, V6_60.GetRoslynExportProfileRequest>("6.6")
                .RegisterRequest<IGetProjectBranchesRequest, V6_60.GetProjectBranchesRequest>("6.6")
                .RegisterRequest<IGetOrganizationsRequest, V7_00.GetOrganizationsRequest>("7.0")
                .RegisterRequest<IGetIssuesRequest, V7_20.GetIssuesRequestWrapper>("7.2")
                .RegisterRequest<IGetHotspotRequest, V8_6.GetHotspotRequest>("8.6")
                .RegisterRequest<IGetTaintVulnerabilitiesRequest, V8_6.GetTaintVulnerabilitiesRequest>("8.6")
                .RegisterRequest<IGetExclusionsRequest, V7_20.GetExclusionsRequest>("7.2")
                .RegisterRequest<IGetSonarLintEventStream, V9_4.GetSonarLintEventStream>("9.4")
                .RegisterRequest<IGetRulesRequest, V9_5.GetRulesWithDescriptionSectionsRequest>("9.5")
                .RegisterRequest<IGetRulesRequest, V9_6.GetRulesWithEducationPrinciplesRequest>("9.6")
                .RegisterRequest<IGetTaintVulnerabilitiesRequest, V9_6.GetTaintVulnerabilitiesWithContextRequest>("9.6")
                .RegisterRequest<ISearchHotspotRequest, V9_7.SearchHotspotRequest>("9.7")
                .RegisterRequest<ISearchHotspotRequest, V10_2.SearchHotspotRequest>("10.2")
                .RegisterRequest<IGetTaintVulnerabilitiesRequest, V10_2.GetTaintVulnerabilitiesWithCCTRequest>("10.2")
                .RegisterRequest<IGetRulesRequest, V10_2.GetRulesWithCCTRequest>("10.2")
                .RegisterRequest<ITransitionIssueRequest, V9_9.TransitionIssueRequestWithWontFix>("9.9")
                .RegisterRequest<ITransitionIssueRequest, V10_4.TransitionIssueRequestWithAccept>("10.4")
                .RegisterRequest<ICommentIssueRequest, V9_9.CommentIssueRequest>("9.9")
                .RegisterRequest<ISearchFilesByNameRequest, V9_9.SearchFilesByNameRequest>("9.9");

            return requestFactory;
        }

        public static UnversionedRequestFactory ConfigureSonarCloud(UnversionedRequestFactory requestFactory)
        {
            requestFactory
                .RegisterRequest<IGetVersionRequest, V2_10.GetVersionRequest>()
                .RegisterRequest<IValidateCredentialsRequest, V3_30.ValidateCredentialsRequest>()
                .RegisterRequest<IGetSourceCodeRequest, V5_00.GetSourceCodeRequest>()
                .RegisterRequest<IGetLanguagesRequest, V5_10.GetLanguagesRequest>()
                .RegisterRequest<IGetModulesRequest, V5_40.GetModulesRequest>()
                .RegisterRequest<IGetRulesRequest, V10_2.GetRulesWithCCTRequest>()
                .RegisterRequest<IDownloadStaticFile, V5_50.DownloadStaticFile>()
                .RegisterRequest<IGetProjectsRequest, V6_20.GetProjectsRequest>()
                .RegisterRequest<IGetPluginsRequest, V6_30.GetPluginsRequest>()
                .RegisterRequest<IGetPropertiesRequest, V6_30.GetPropertiesRequest>()
                .RegisterRequest<IGetQualityProfileChangeLogRequest, V6_50.GetQualityProfileChangeLogRequest>()
                .RegisterRequest<IGetQualityProfilesRequest, V6_50.GetQualityProfilesRequest>()
                .RegisterRequest<IGetNotificationsRequest, V6_60.GetNotificationsRequest>()
                .RegisterRequest<IGetRoslynExportProfileRequest, V6_60.GetRoslynExportProfileRequest>()
                .RegisterRequest<IGetProjectBranchesRequest, V6_60.GetProjectBranchesRequest>()
                .RegisterRequest<IGetOrganizationsRequest, V7_00.GetOrganizationsRequest>()
                .RegisterRequest<IGetIssuesRequest, V7_20.GetIssuesRequestWrapper>()
                .RegisterRequest<IGetHotspotRequest, V8_6.GetHotspotRequest>()
                .RegisterRequest<IGetTaintVulnerabilitiesRequest, V10_2.GetTaintVulnerabilitiesWithCCTRequest>()
                .RegisterRequest<IGetExclusionsRequest, V7_20.GetExclusionsRequest>()
                .RegisterRequest<ISearchHotspotRequest, V9_7.SearchHotspotRequest>()
                .RegisterRequest<ITransitionIssueRequest, V9_9.TransitionIssueRequestWithWontFix>()
                .RegisterRequest<ICommentIssueRequest, V9_9.CommentIssueRequest>()
                .RegisterRequest<ISearchFilesByNameRequest, V9_9.SearchFilesByNameRequest>();

            return requestFactory;
        }
    }
}

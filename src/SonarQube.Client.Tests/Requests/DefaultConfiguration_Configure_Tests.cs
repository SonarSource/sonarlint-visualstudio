﻿/*
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

using SonarQube.Client.Api;
using SonarQube.Client.Requests;
using SonarQube.Client.Tests.Infra;

namespace SonarQube.Client.Tests.Requests;

[TestClass]
public class DefaultConfiguration_Configure_Tests
{
    [TestMethod]
    public void ConfigureSonarQube_Writes_Debug_Messages()
    {
        var logger = new TestLogger();

        var expected = new[]
        {
            "Registered SonarQube.Client.Api.V2_10.GetPluginsRequest for 2.1",
            "Registered SonarQube.Client.Api.V2_10.GetProjectsRequest for 2.1",
            "Registered SonarQube.Client.Api.V2_10.GetVersionRequest for 2.1",
            "Registered SonarQube.Client.Api.V2_60.GetPropertiesRequest for 2.6",
            "Registered SonarQube.Client.Api.V3_30.ValidateCredentialsRequest for 3.3",
            "Registered SonarQube.Client.Api.V5_10.GetIssuesRequest for 5.1",
            "Registered SonarQube.Client.Api.V5_10.GetLanguagesRequest for 5.1",
            "Registered SonarQube.Client.Api.V5_20.GetQualityProfileChangeLogRequest for 5.2",
            "Registered SonarQube.Client.Api.V5_20.GetQualityProfilesRequest for 5.2",
            "Registered SonarQube.Client.Api.V5_20.GetRoslynExportProfileRequest for 5.2",
            "Registered SonarQube.Client.Api.V5_40.GetModulesRequest for 5.4",
            "Registered SonarQube.Client.Api.V5_50.GetRulesRequest for 5.5",
            "Registered SonarQube.Client.Api.V5_50.DownloadStaticFile for 5.5",
            "Registered SonarQube.Client.Api.V6_20.GetOrganizationsRequest for 6.2",
            "Registered SonarQube.Client.Api.V6_20.GetProjectsRequest for 6.2",
            "Registered SonarQube.Client.Api.V6_30.GetPluginsRequest for 6.3",
            "Registered SonarQube.Client.Api.V6_30.GetPropertiesRequest for 6.3",
            "Registered SonarQube.Client.Api.V6_50.GetQualityProfileChangeLogRequest for 6.5",
            "Registered SonarQube.Client.Api.V6_50.GetQualityProfilesRequest for 6.5",
            "Registered SonarQube.Client.Api.V6_60.GetNotificationsRequest for 6.6",
            "Registered SonarQube.Client.Api.V6_60.GetRoslynExportProfileRequest for 6.6",
            "Registered SonarQube.Client.Api.V6_60.GetProjectBranchesRequest for 6.6",
            "Registered SonarQube.Client.Api.V7_00.GetOrganizationsRequest for 7.0",
            "Registered SonarQube.Client.Api.V7_20.GetIssuesRequestWrapper`1[SonarQube.Client.Api.V7_20.GetIssuesWithComponentSonarQubeRequest] for 7.2",
            "Registered SonarQube.Client.Api.V8_6.GetHotspotRequest for 8.6",
            "Registered SonarQube.Client.Api.V7_20.GetExclusionsRequest for 7.2",
            "Registered SonarQube.Client.Api.V9_4.GetSonarLintEventStream for 9.4",
            "Registered SonarQube.Client.Api.V9_7.SearchHotspotRequest for 9.7",
            "Registered SonarQube.Client.Api.V10_2.SearchHotspotRequest for 10.2",
            "Registered SonarQube.Client.Api.V10_2.GetRulesWithCCTRequest for 10.2",
            "Registered SonarQube.Client.Api.V9_9.TransitionIssueRequestWithWontFix for 9.9",
            "Registered SonarQube.Client.Api.V10_4.TransitionIssueRequestWithAccept for 10.4",
            "Registered SonarQube.Client.Api.V9_9.CommentIssueRequest for 9.9",
            "Registered SonarQube.Client.Api.V9_9.SearchFilesByNameRequest for 9.9"
        };

        DefaultConfiguration.ConfigureSonarQube(new RequestFactory(logger));

        DumpDebugMessages(logger);

        logger.DebugMessages.Should().ContainInOrder(expected);
        logger.DebugMessages.Count.Should().Be(expected.Length);
    }

    [TestMethod]
    public void ConfigureSonarCloud_Writes_Debug_Messages()
    {
        var logger = new TestLogger();

        var expected = new string[]
        {
            "Registered SonarQube.Client.Api.V2_10.GetVersionRequest",
            "Registered SonarQube.Client.Api.V3_30.ValidateCredentialsRequest",
            "Registered SonarQube.Client.Api.V5_10.GetLanguagesRequest",
            "Registered SonarQube.Client.Api.V5_40.GetModulesRequest",
            "Registered SonarQube.Client.Api.V10_2.GetRulesWithCCTRequest",
            "Registered SonarQube.Client.Api.V5_50.DownloadStaticFile",
            "Registered SonarQube.Client.Api.V6_20.GetProjectsRequest",
            "Registered SonarQube.Client.Api.V6_30.GetPluginsRequest",
            "Registered SonarQube.Client.Api.V6_30.GetPropertiesRequest",
            "Registered SonarQube.Client.Api.V6_50.GetQualityProfileChangeLogRequest",
            "Registered SonarQube.Client.Api.V6_50.GetQualityProfilesRequest",
            "Registered SonarQube.Client.Api.V6_60.GetNotificationsRequest",
            "Registered SonarQube.Client.Api.V6_60.GetRoslynExportProfileRequest",
            "Registered SonarQube.Client.Api.V6_60.GetProjectBranchesRequest",
            "Registered SonarQube.Client.Api.V7_00.GetOrganizationsRequest",
            "Registered SonarQube.Client.Api.V7_20.GetIssuesRequestWrapper`1[SonarQube.Client.Api.V7_20.GetIssuesWithComponentSonarCloudRequest]",
            "Registered SonarQube.Client.Api.V8_6.GetHotspotRequest",
            "Registered SonarQube.Client.Api.V7_20.GetExclusionsRequest",
            "Registered SonarQube.Client.Api.V9_7.SearchHotspotRequest",
            "Registered SonarQube.Client.Api.V10_4.TransitionIssueRequestWithAccept",
            "Registered SonarQube.Client.Api.V9_9.CommentIssueRequest",
            "Registered SonarQube.Client.Api.V9_9.SearchFilesByNameRequest"
        };

        DefaultConfiguration.ConfigureSonarCloud(new UnversionedRequestFactory(logger));

        DumpDebugMessages(logger);

        logger.DebugMessages.Should().ContainInOrder(expected);
        logger.DebugMessages.Count.Should().Be(expected.Length);
    }

    [TestMethod]
    public void ConfigureSonarQube_CheckAllRequestsImplemented()
    {
        var testSubject = DefaultConfiguration.ConfigureSonarQube(new RequestFactory(new TestLogger()));
        var serverInfo = new ServerInfo(null /* latest */, ServerType.SonarQube);

        testSubject.Create<IDownloadStaticFile>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetHotspotRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetIssuesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetLanguagesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetModulesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetNotificationsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetOrganizationsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetPluginsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetProjectsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetPropertiesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetQualityProfileChangeLogRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetQualityProfilesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetRoslynExportProfileRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetRulesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetVersionRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IValidateCredentialsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetProjectBranchesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetExclusionsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetSonarLintEventStream>(serverInfo).Should().NotBeNull();
        testSubject.Create<ITransitionIssueRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<ICommentIssueRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<ISearchFilesByNameRequest>(serverInfo).Should().NotBeNull();
    }

    [TestMethod]
    public void ConfigureSonarCloud_CheckAllRequestsImplemented()
    {
        var testSubject = DefaultConfiguration.ConfigureSonarCloud(new UnversionedRequestFactory(new TestLogger()));
        var serverInfo = new ServerInfo(null /* latest */, ServerType.SonarQube);

        testSubject.Create<IDownloadStaticFile>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetHotspotRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetIssuesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetLanguagesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetModulesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetNotificationsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetOrganizationsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetPluginsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetProjectsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetPropertiesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetQualityProfileChangeLogRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetQualityProfilesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetRoslynExportProfileRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetRulesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetVersionRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IValidateCredentialsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetProjectBranchesRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<IGetExclusionsRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<ITransitionIssueRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<ICommentIssueRequest>(serverInfo).Should().NotBeNull();
        testSubject.Create<ISearchFilesByNameRequest>(serverInfo).Should().NotBeNull();
    }

    [TestMethod]
    [Description("The following APIs are not implemented on SC (yet). Verify that they are not registered in the factory.")]
    public void ConfigureSonarCloud_CheckUnsupportedRequestsAreNotImplemented()
    {
        var testSubject = DefaultConfiguration.ConfigureSonarCloud(new UnversionedRequestFactory(new TestLogger()));
        var serverInfo = new ServerInfo(null /* latest */, ServerType.SonarQube);

        Action act = () => testSubject.Create<IGetSonarLintEventStream>(serverInfo);
        act.Should().Throw<InvalidOperationException>().And.Message.Should()
            .Be("Could not find factory for 'IGetSonarLintEventStream'.");
    }

    private static void DumpDebugMessages(TestLogger logger)
    {
        Debug.WriteLine("Actual registered requests:");
        foreach (var message in logger.DebugMessages)
        {
            Debug.WriteLine(message);
        }
    }
}

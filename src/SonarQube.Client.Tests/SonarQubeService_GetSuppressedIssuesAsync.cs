﻿/*
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

using System.IO;
using System.Net;
using System.Net.Http;
using Moq;
using SonarQube.Client.Models;
using static SonarQube.Client.Tests.Infra.MocksHelper;

namespace SonarQube.Client.Tests;

[TestClass]
public class SonarQubeService_GetSuppressedIssuesAsync : SonarQubeService_GetIssuesBase
{
    [TestMethod]
    public async Task GetSuppressedIssuesAsync_Old_ExampleFromSonarQube()
    {
        await ConnectToSonarQube();

        using (var reader = new StreamReader(@"TestResources\IssuesProtobufResponse"))
        {
            SetupRequest("batch/issues?key=project1", reader.ReadToEnd());
        }

        var result = await service.GetSuppressedIssuesAsync("project1", null, null, CancellationToken.None);

        // TODO: create a protobuf file with more than one issue with different states
        // the one above does not have suppressed issues, hence the Count==0
        result.Should().BeEmpty();

        httpClientHandler.VerifyAll();
    }

    [TestMethod]
    public async Task GetSuppressedIssuesAsync_Old_NotFound()
    {
        await ConnectToSonarQube();

        SetupRequest("batch/issues?key=project1", "", HttpStatusCode.NotFound);

        Func<Task<IList<SonarQubeIssue>>> func = async () =>
            await service.GetSuppressedIssuesAsync("project1", null, null, CancellationToken.None);

        func.Should().ThrowExactly<HttpRequestException>().And
            .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

        httpClientHandler.VerifyAll();
    }

    [TestMethod]
    public async Task GetSuppressedIssuesAsync_From_7_20()
    {
        await ConnectToSonarQube("7.2.0.0");

        SetupRequest("api/issues/search?projects=shared&statuses=RESOLVED&types=CODE_SMELL&p=1&ps=500", @"
{
  ""total"": 5,
  ""p"": 1,
  ""ps"": 100,
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 100,
    ""total"": 5
  },
  ""issues"": [
    {
      ""key"": ""AWg8bjfdFPFMeKWzHZ_7"",
      ""rule"": ""csharpsquid:S3990"",
      ""severity"": ""MAJOR"",
      ""component"": ""shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082"",
      ""project"": ""shared"",
      ""flows"": [],
      ""resolution"": ""WONTFIX"",
      ""status"": ""RESOLVED"",
      ""message"": ""Mark this assembly with 'System.CLSCompliantAttribute'"",
      ""effort"": ""1min"",
      ""debt"": ""1min"",
      ""author"": """",
      ""tags"": [
        ""api-design""
      ],
      ""creationDate"": ""2019-01-11T11:21:20+0100"",
      ""updateDate"": ""2019-01-11T11:28:22+0100"",
      ""type"": ""CODE_SMELL"",
      ""organization"": ""default-organization""
    }
  ],
  ""components"": [ ]
}
");
        SetupRequest("api/issues/search?projects=shared&statuses=RESOLVED&types=BUG&p=1&ps=500", @"
{
  ""total"": 5,
  ""p"": 1,
  ""ps"": 100,
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 100,
    ""total"": 5
  },
  ""issues"": [
    {
      ""key"": ""AWg8adcV_JurIR2zdSvR"",
      ""rule"": ""csharpsquid:S1118"",
      ""severity"": ""MAJOR"",
      ""component"": ""shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082:Program.cs"",
      ""project"": ""shared"",
      ""subProject"": ""shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082"",
      ""line"": 6,
      ""hash"": ""0afa1b5e62aa3cfaf1cd9a4e63571cb5"",
      ""textRange"": {
        ""startLine"": 6,
        ""endLine"": 6,
        ""startOffset"": 10,
        ""endOffset"": 17
      },
      ""flows"": [],
      ""resolution"": ""WONTFIX"",
      ""status"": ""RESOLVED"",
      ""message"": ""Add a 'protected' constructor or the 'static' keyword to the class declaration."",
      ""effort"": ""10min"",
      ""debt"": ""10min"",
      ""author"": """",
      ""tags"": [
        ""design""
      ],
      ""creationDate"": ""2019-01-11T11:16:30+0100"",
      ""updateDate"": ""2019-01-11T11:26:39+0100"",
      ""type"": ""BUG"",
      ""organization"": ""default-organization""
    }
  ],
  ""components"": [
    {
      ""organization"": ""default-organization"",
      ""key"": ""shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082:Program.cs"",
      ""uuid"": ""AWg8adNk_JurIR2zdSvM"",
      ""enabled"": true,
      ""qualifier"": ""FIL"",
      ""name"": ""Program.cs"",
      ""longName"": ""Program.cs"",
      ""path"": ""Program.cs""
    }
  ]
}
");

        var result = await service.GetSuppressedIssuesAsync("shared", null, null, CancellationToken.None);

        result.Should().HaveCount(2);

        // Module level issues don't have FilePath, hash and line
        result[0].FilePath.Should().Be(string.Empty);
        result[0].Hash.Should().BeNull();
        result[0].TextRange.Should().BeNull();
        result[0].Message.Should().Be("Mark this assembly with 'System.CLSCompliantAttribute'");
        result[0].ModuleKey.Should().Be("shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082");
        result[0].IsResolved.Should().BeTrue();
        result[0].RuleId.Should().Be("csharpsquid:S3990");
        result[0].Severity.Should().Be(SonarQubeIssueSeverity.Major);

        result[1].FilePath.Should().Be("Program.cs");
        result[1].Hash.Should().Be("0afa1b5e62aa3cfaf1cd9a4e63571cb5");
        result[1].TextRange.Should().BeEquivalentTo(new IssueTextRange(6, 6, 10, 17));
        result[1].Message.Should().Be("Add a 'protected' constructor or the 'static' keyword to the class declaration.");
        result[1].ModuleKey.Should().Be("shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082");
        result[1].IsResolved.Should().BeTrue();
        result[1].RuleId.Should().Be("csharpsquid:S1118");
        result[1].Severity.Should().Be(SonarQubeIssueSeverity.Major);

        httpClientHandler.VerifyAll();
    }

    [TestMethod]
    public async Task GetSuppressedIssuesAsync_From_7_20_NotFound()
    {
        await ConnectToSonarQube("7.2.0.0");

        SetupRequest("api/issues/search?projects=project1&statuses=RESOLVED&types=CODE_SMELL&p=1&ps=500", "", HttpStatusCode.NotFound);

        Func<Task<IList<SonarQubeIssue>>> func = async () =>
            await service.GetSuppressedIssuesAsync("project1", null, null, CancellationToken.None);

        func.Should().ThrowExactly<HttpRequestException>().And
            .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

        httpClientHandler.VerifyAll();
    }

    [TestMethod]
    public async Task GetSuppressedIssuesAsync_From_7_20_Paging()
    {
        await ConnectToSonarQube("7.2.0.0");

        SetupPagesOfResponses("simplcom", 1001, "CODE_SMELL");
        SetupPageOfResponses("simplcom", 1, 0, "BUG");

        var result = await service.GetSuppressedIssuesAsync("simplcom", null, null, CancellationToken.None);

        result.Should().HaveCount(1001);
        result.Select(i => i.FilePath).Should().Match(paths => paths.All(p => p == "Program.cs"));

        httpClientHandler.VerifyAll();
    }

    [TestMethod]
    // Note: we're not testing all possible combinations because testing with the
    // max number of items is relatively slow (several seconds per iteration)
    [DataRow(5, 5)] // No issue types with too many issues
    [DataRow(MaxAllowedIssues, 5)] // One issue type with too many issues
    [DataRow(1, MaxAllowedIssues)] // Multiple issue types with too many issues
    public async Task GetSuppressedIssuesAsync_From_7_20_NotifyWhenMaxIssuesReturned(
        int numCodeSmells,
        int numBugs)
    {
        await ConnectToSonarQube("7.2.0.0");

        SetupPagesOfResponses("proj1", numCodeSmells, "CODE_SMELL");
        SetupPagesOfResponses("proj1", numBugs, "BUG");

        var result = await service.GetSuppressedIssuesAsync("proj1", null, null, CancellationToken.None);

        result.Should().HaveCount(
            Math.Min(MaxAllowedIssues, numCodeSmells) +
            Math.Min(MaxAllowedIssues, numBugs));

        DumpWarningsToConsole();

        httpClientHandler.VerifyAll();

        checkForExpectedWarning(numCodeSmells, "code smells");
        checkForExpectedWarning(numBugs, "bugs");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    public async Task GetSuppressedIssuesAsync_From_7_20_BranchIsNotSpecified_BranchIsNotIncludedInQueryString(string emptyBranch)
    {
        await ConnectToSonarQube("7.2.0.0");
        httpClientHandler.Reset();

        SetupHttpRequest(httpClientHandler, EmptyGetIssuesResponse);
        _ = await service.GetSuppressedIssuesAsync("any", emptyBranch, null, CancellationToken.None);

        // Branch is null/empty => should not be passed
        var actualRequests = httpClientHandler.GetSendAsyncRequests();
        actualRequests.Should().HaveCount(2);
        actualRequests.Should().NotContain(x => x.RequestUri.Query.Contains("branch"));
    }

    [TestMethod]
    public async Task GetSuppressedIssuesAsync_From_7_20_BranchIsSpecified_BranchIncludedInQueryString()
    {
        await ConnectToSonarQube("7.2.0.0");
        httpClientHandler.Reset();

        SetupHttpRequest(httpClientHandler, EmptyGetIssuesResponse);
        _ = await service.GetSuppressedIssuesAsync("any", "aBranch", null, CancellationToken.None);

        // The wrapper is expected to make three calls, for code smells, bugs, then vulnerabilities
        var actualRequests = httpClientHandler.GetSendAsyncRequests();
        actualRequests.Should().HaveCount(2);
        actualRequests.Should().OnlyContain(x => x.RequestUri.Query.Contains("&branch=aBranch&"));
    }

    [TestMethod]
    public async Task GetSuppressedIssuesAsync_From_7_20_IssueKeysAreNotSpecified_IssueKeysAreNotIncludedInQueryString()
    {
        await ConnectToSonarQube("7.2.0.0");
        httpClientHandler.Reset();

        SetupHttpRequest(httpClientHandler, EmptyGetIssuesResponse);
        _ = await service.GetSuppressedIssuesAsync("any", null, null, CancellationToken.None);

        // The wrapper is expected to make three calls, for code smells, bugs, then vulnerabilities
        var actualRequests = httpClientHandler.GetSendAsyncRequests();
        actualRequests.Should().HaveCount(2);
        actualRequests.Should().NotContain(x => x.RequestUri.Query.Contains("issues"));
    }

    [TestMethod]
    public async Task GetSuppressedIssuesAsync_From_7_20_IssueKeysAreSpecified_IssueKeysAreIncludedInQueryString()
    {
        await ConnectToSonarQube("7.2.0.0");
        httpClientHandler.Reset();

        SetupHttpRequest(httpClientHandler, EmptyGetIssuesResponse);
        _ = await service.GetSuppressedIssuesAsync("any", null, new[] { "issue1", "issue2" }, CancellationToken.None);

        // The wrapper is expected to make one call with the given issueKeys
        var actualRequests = httpClientHandler.GetSendAsyncRequests();
        actualRequests.Should().ContainSingle();
        actualRequests.Should().OnlyContain(x => x.RequestUri.Query.Contains("issues=issue1%2Cissue2"));
    }

    [TestMethod]
    public void GetSuppressedIssuesAsync_NotConnected()
    {
        // No calls to Connect
        // No need to setup request, the operation should fail

        Func<Task<IList<SonarQubeIssue>>> func = async () =>
            await service.GetSuppressedIssuesAsync("simplcom", null, null, CancellationToken.None);

        func.Should().ThrowExactly<InvalidOperationException>().And
            .Message.Should().Be("This operation expects the service to be connected.");

        logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
    }
}

﻿/*
 * SonarQube Client
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Api;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetSuppressedIssuesAsync : SonarQubeService_TestBase
    {
        private const int MaxAllowedIssues = PagedRequestBase<IGetIssuesRequest>.MaximumItemsCount;
        private const int PageSize = PagedRequestBase<IGetIssuesRequest>.MaximumPageSize;

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_Old_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            ////var issues = ServerIssue.Parser.ParseDelimitedFrom(new FileStream(@"TestResources\IssuesProtobufResponse", FileMode.Open));

            using (var reader = new StreamReader(@"TestResources\IssuesProtobufResponse"))
            {
                SetupRequest("batch/issues?key=project1", reader.ReadToEnd());
            }
            //SetupRequest("batch/issues?key=project1",
            //    new StreamReader(@"TestResources\IssuesProtobufResponse").ReadToEnd());

            var result = await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            // TODO: create a protobuf file with more than one issue with different states
            // the one above does not have suppressed issues, hence the Count==0
            result.Should().BeEmpty();

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_Old_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("batch/issues?key=project1", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeIssue>>> func = async () =>
                await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
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

            SetupRequest("api/issues/search?projects=shared&statuses=RESOLVED&types=VULNERABILITY&p=1&ps=500", @"
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
      ""key"": ""AWg9DV27DpKqrfA7luen"",
      ""rule"": ""csharpsquid:S1451"",
      ""severity"": ""BLOCKER"",
      ""component"": ""shared:SharedProject1/SharedClass1.cs"",
      ""project"": ""shared"",
      ""flows"": [],
      ""resolution"": ""WONTFIX"",
      ""status"": ""RESOLVED"",
      ""message"": ""Add or update the header of this file."",
      ""effort"": ""5min"",
      ""debt"": ""5min"",
      ""author"": """",
      ""tags"": [],
      ""creationDate"": ""2019-01-11T13:18:25+0100"",
      ""updateDate"": ""2019-01-11T14:15:53+0100"",
      ""type"": ""VULNERABILITY"",
      ""organization"": ""default-organization"",
      ""fromHotspot"": false
    },
    {
      ""key"": ""AWg8adc9_JurIR2zdSvT"",
      ""rule"": ""csharpsquid:S3400"",
      ""severity"": ""MINOR"",
      ""component"": ""shared:SharedProject1/SharedClass1.cs"",
      ""project"": ""shared"",
      ""line"": 5,
      ""hash"": ""be411c6cf1ae5ba7d7c5d6da7355afa1"",
      ""textRange"": {
        ""startLine"": 5,
        ""endLine"": 5,
        ""startOffset"": 27,
        ""endOffset"": 30
      },
      ""flows"": [],
      ""resolution"": ""WONTFIX"",
      ""status"": ""RESOLVED"",
      ""message"": ""Remove this method and declare a constant for this value."",
      ""effort"": ""5min"",
      ""debt"": ""5min"",
      ""author"": """",
      ""tags"": [
        ""confusing""
      ],
      ""creationDate"": ""2019-01-11T11:16:30+0100"",
      ""updateDate"": ""2019-01-11T11:26:55+0100"",
      ""type"": ""VULNERABILITY"",
      ""organization"": ""default-organization""
    }
  ],
  ""components"": [
    {
      ""organization"": ""default-organization"",
      ""key"": ""shared:SharedProject1/SharedClass1.cs"",
      ""uuid"": ""AWg8adNl_JurIR2zdSvQ"",
      ""enabled"": true,
      ""qualifier"": ""FIL"",
      ""name"": ""SharedClass1.cs"",
      ""longName"": ""SharedProject1/SharedClass1.cs"",
      ""path"": ""SharedProject1/SharedClass1.cs""
    }
  ]
}
");

            var result = await service.GetSuppressedIssuesAsync("shared", CancellationToken.None);

            result.Should().HaveCount(4);

            // Module level issues don't have FilePath, hash and line
            result[0].FilePath.Should().Be(string.Empty);
            result[0].Hash.Should().BeNull();
            result[0].Line.Should().BeNull();
            result[0].Message.Should().Be("Mark this assembly with 'System.CLSCompliantAttribute'");
            result[0].ModuleKey.Should().Be("shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082");
            result[0].IsResolved.Should().BeTrue();
            result[0].RuleId.Should().Be("S3990");

            result[1].FilePath.Should().Be("Program.cs");
            result[1].Hash.Should().Be("0afa1b5e62aa3cfaf1cd9a4e63571cb5");
            result[1].Line.Should().Be(6);
            result[1].Message.Should().Be("Add a 'protected' constructor or the 'static' keyword to the class declaration.");
            result[1].ModuleKey.Should().Be("shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082");
            result[1].IsResolved.Should().BeTrue();
            result[1].RuleId.Should().Be("S1118");

            // File level issues don't have hash and line
            result[2].FilePath.Should().Be("SharedProject1/SharedClass1.cs");
            result[2].Hash.Should().BeNull();
            result[2].Line.Should().BeNull();
            result[2].Message.Should().Be("Add or update the header of this file.");
            result[2].ModuleKey.Should().Be("shared:SharedProject1/SharedClass1.cs");
            result[2].IsResolved.Should().BeTrue();
            result[2].RuleId.Should().Be("S1451");

            result[3].FilePath.Should().Be("SharedProject1/SharedClass1.cs");
            result[3].Hash.Should().Be("be411c6cf1ae5ba7d7c5d6da7355afa1");
            result[3].Line.Should().Be(5);
            result[3].Message.Should().Be("Remove this method and declare a constant for this value.");
            result[3].ModuleKey.Should().Be("shared:SharedProject1/SharedClass1.cs");
            result[3].IsResolved.Should().BeTrue();
            result[3].RuleId.Should().Be("S3400");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_From_7_20_NotFound()
        {
            await ConnectToSonarQube("7.2.0.0");

            SetupRequest("api/issues/search?projects=project1&statuses=RESOLVED&types=CODE_SMELL&p=1&ps=500", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeIssue>>> func = async () =>
                await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_From_7_20_Paging()
        {
            await ConnectToSonarQube("7.2.0.0");

            SetupPagesOfResponses("simplcom", 1001, "CODE_SMELL");
            SetupPageOfResponses("simplcom", 1, 0, "BUG");
            SetupPageOfResponses("simplcom", 1, 0, "VULNERABILITY");

            var result = await service.GetSuppressedIssuesAsync("simplcom", CancellationToken.None);

            result.Should().HaveCount(1001);
            result.Select(i => i.FilePath).Should().Match(paths => paths.All(p => p == "Program.cs"));

            messageHandler.VerifyAll();
        }

        [TestMethod]
        // Note: we're not testing all possible combinations because testing with the
        // max number of items is relatively slow (several seconds per iteration)
        [DataRow(5, 5, 5)] // No issue types with too many issues
        [DataRow(MaxAllowedIssues, 5, 2)] // One issue type with too many issues
        [DataRow(1, MaxAllowedIssues, MaxAllowedIssues)] // Multiple issue types with too many issues
        public async Task GetSuppressedIssuesAsync_From_7_20_NotifyWhenMaxIssuesReturned(
            int numCodeSmells, int numBugs, int numVulnerabilities)
        {
            await ConnectToSonarQube("7.2.0.0");

            SetupPagesOfResponses("proj1", numCodeSmells, "CODE_SMELL");
            SetupPagesOfResponses("proj1", numBugs, "BUG");
            SetupPagesOfResponses("proj1", numVulnerabilities, "VULNERABILITY");

            var result = await service.GetSuppressedIssuesAsync("proj1", CancellationToken.None);

            result.Should().HaveCount(
                Math.Min(MaxAllowedIssues, numCodeSmells) +
                Math.Min(MaxAllowedIssues, numBugs) +
                Math.Min(MaxAllowedIssues, numVulnerabilities));

            DumpWarningsToConsole();

            messageHandler.VerifyAll();

            checkForExpectedWarning(numCodeSmells, "code smells");
            checkForExpectedWarning(numBugs, "bugs");
            checkForExpectedWarning(numVulnerabilities, "vulnerabilities");

            void checkForExpectedWarning(int itemCount, string partialText)
            {
                // Only expect a warning if the number of items is equal or greater than the max allowed
                var expectedMessageCount = itemCount >= MaxAllowedIssues ? 1 : 0;
                logger.WarningMessages.Count(x => x.Contains(partialText)).Should().Be(expectedMessageCount);
            }
        }

        [TestMethod]
        public void GetSuppressedIssuesAsync_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<IList<SonarQubeIssue>>> func = async () =>
                await service.GetSuppressedIssuesAsync("simplcom", CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }

        private void SetupPageOfResponses(string projectName, int pageNumber, int numberOfIssues, string issueType)
        {
            // Sanity check of the issue types
            issueType.Should().BeOneOf("CODE_SMELL", "BUG", "VULNERABILITY");

            var startItemNumber = (pageNumber - 1) * PageSize + 1;

            var issuesJson = string.Empty;
            var componentJson = string.Empty;
            if (numberOfIssues > 0)
            {
                issuesJson = string.Join(",\n", Enumerable.Range(startItemNumber, numberOfIssues).Select(CreateIssueJson));
                componentJson = CreateComponentJson();
            }

            SetupRequest($"api/issues/search?projects={projectName}&statuses=RESOLVED&types={issueType}&p={pageNumber}&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": {pageNumber},
    ""pageSize"": {PageSize},
    ""total"": 9999
  }},
  ""issues"": [
    {issuesJson}
  ],
  ""components"": [
    {componentJson}
  ]
}}");
        }

        private void SetupPagesOfResponses(string projectName, int numberOfIssues, string issueType)
        {
            var pageNumber = 1;
            var remainingIssues = numberOfIssues;
            while(remainingIssues > 0)
            {
                var issuesOnNewPage = Math.Min(remainingIssues, PageSize);
                SetupPageOfResponses(projectName, pageNumber, issuesOnNewPage, issueType);

                pageNumber++;
                remainingIssues = remainingIssues - issuesOnNewPage;
            }
        }

        private static string CreateIssueJson(int number) =>
            "{ " +
            $"\"key\": \"{number}\", " +
            "\"rule\": \"csharpsquid:S1075\", " +
            "\"component\": \"shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082:Program.cs\", " +
            "\"project\": \"shared\", " +
            "\"subProject\": \"shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082\", " +
            "\"line\": 36, " +
            "\"hash\": \"0dcbf3b077bacc9fdbd898ff3b587085\", " +
            "\"status\": \"RESOLVED\", " +
            "\"message\": \"Refactor your code not to use hardcoded absolute paths or URIs.\" " +
            "}";

        private static string CreateComponentJson() =>
            // "key" should be the same as the "component" field in the issues from CreateIssueJson()
            @"
{
    ""organization"": ""default-organization"",
    ""key"": ""shared:shared:2B470B7D-D47B-4E41-B105-D3938E196082:Program.cs"",
    ""uuid"": ""AWg8adNk_JurIR2zdSvM"",
    ""enabled"": true,
    ""qualifier"": ""FIL"",
    ""name"": ""Program.cs"",
    ""longName"": ""Program.cs"",
    ""path"": ""Program.cs""
}";

        private void DumpWarningsToConsole()
        {
            System.Console.WriteLine("Warnings:");
            foreach (string item in logger.WarningMessages)
            {
                System.Console.WriteLine(item);
            }
            System.Console.WriteLine("");
        }
    }
}

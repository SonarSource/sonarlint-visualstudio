/*
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

using System;
using System.Linq;
using FluentAssertions;
using SonarQube.Client.Api;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Tests
{
    public class SonarQubeService_GetIssuesBase : SonarQubeService_TestBase
    {
        protected const int MaxAllowedIssues = PagedRequestBase<IGetIssuesRequest>.MaximumItemsCount;
        protected const int PageSize = PagedRequestBase<IGetIssuesRequest>.MaximumPageSize;

        protected void SetupPageOfResponses(string projectName, int pageNumber, int numberOfIssues, string issueType)
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

            SetupRequest($"api/issues/search?projects={projectName}&statuses=RESOLVED&types={issueType}&p={pageNumber}&ps={PageSize}", $@"
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

        protected void SetupPageOfResponses(string projectName, string ruleId, string componentKey, string branch, int pageNumber, int numberOfIssues, string issueType)
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

            SetupRequest($"api/issues/search?components={componentKey}&projects={projectName}&rules={ruleId}&branch={branch}&types={issueType}&p={pageNumber}&ps={PageSize}", $@"
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

        protected void SetupPagesOfResponses(string projectName, int numberOfIssues, string issueType)
        {
            var pageNumber = 1;
            var remainingIssues = numberOfIssues;
            while (remainingIssues > 0)
            {
                var issuesOnNewPage = Math.Min(remainingIssues, PageSize);
                SetupPageOfResponses(projectName, pageNumber, issuesOnNewPage, issueType);

                pageNumber++;
                remainingIssues -= issuesOnNewPage;
            }
        }

        protected void SetupPagesOfResponses(string projectName, string ruleId, string componentKey, string branch, int numberOfIssues, string issueType)
        {
            var pageNumber = 1;
            var remainingIssues = numberOfIssues;
            while (remainingIssues > 0)
            {
                var issuesOnNewPage = Math.Min(remainingIssues, PageSize);
                SetupPageOfResponses(projectName, ruleId, componentKey, branch, pageNumber, issuesOnNewPage, issueType);

                pageNumber++;
                remainingIssues -= issuesOnNewPage;
            }
        }

        protected static string CreateIssueJson(int number) =>
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

        protected static string CreateComponentJson() =>
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

        protected void checkForExpectedWarning(int itemCount, string partialText)
        {
            // Only expect a warning if the number of items is equal or greater than the max allowed
            var expectedMessageCount = itemCount >= MaxAllowedIssues ? 1 : 0;
            logger.WarningMessages.Count(x => x.Contains(partialText)).Should().Be(expectedMessageCount);
        }

        protected void DumpWarningsToConsole()
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

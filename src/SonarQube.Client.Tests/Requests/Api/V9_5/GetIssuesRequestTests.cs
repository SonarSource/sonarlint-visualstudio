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

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Api.V9_5;
using SonarQube.Client.Logging;
using SonarQube.Client.Messages.Issues;
using SonarQube.Client.Models;
using SonarQube.Client.Tests.Infra;

namespace SonarQube.Client.Tests.Requests.Api.V9_5
{
    [TestClass]
    public class GetIssuesRequestTests
    {
        [TestMethod]
        public async Task InvokeAsync_HasNoIssues_ReturnsAnEmptyList()
        {
            var issuesPullQueryTimestamp = new IssuesPullQueryTimestamp { QueryTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() };
            var testedStream = CreateStreamRequest(issuesPullQueryTimestamp);

            var messageHandler = new Mock<HttpMessageHandler>();
            using var httpClient = new HttpClient(messageHandler.Object) { BaseAddress = new Uri("http://localhost") };

            MocksHelper.SetupHttpRequest(
                messageHandler,
                requestRelativePath:
                "api/issues/pull?projectKey=someproj&languages=cs%2Cvbnet%2Ccpp%2Cc%2Cjs%2Cts%2Csecrets&branchName=main&resolvedOnly=false",
                responseMessage: new HttpResponseMessage { Content = new StreamContent(testedStream) });

            var testSubject = new GetIssuesRequest { ProjectKey = "someproj", Branch = "main", Logger = Mock.Of<ILogger>() };

            var response = await testSubject.InvokeAsync(httpClient, CancellationToken.None);
            response.Should().BeEmpty();
            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task InvokeAsync_HasClosedIssues_ClosedIssuesAreIgnored()
        {
            var issuesPullQueryTimestamp = new IssuesPullQueryTimestamp { QueryTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() };

            var closedIssue = new IssueLite
            {
                Closed = true,
                Key = "issue3"
            };

            var resolvedIssue = new IssueLite
            {
                Closed = false,
                CreationDate = issuesPullQueryTimestamp.QueryTimestamp,
                Key = "issue1",
                MainLocation = new Location
                {
                    FilePath = "path1",
                    Message = "message1",
                    TextRange = new TextRange
                    {
                        EndLineOffset = 1,
                        EndLine = 2,
                        Hash = "hash1",
                        StartLine = 3,
                        StartLineOffset = 4
                    }
                },
                Resolved = true,
                RuleKey = "rule1",
                Type = RuleType.Vulnerability,
                UserSeverity = Severity.Critical
            };

            var testedStream = CreateStreamRequest(issuesPullQueryTimestamp, closedIssue, resolvedIssue);

            var messageHandler = new Mock<HttpMessageHandler>();
            using var httpClient = new HttpClient(messageHandler.Object) { BaseAddress = new Uri("http://localhost") };

            MocksHelper.SetupHttpRequest(
                messageHandler,
                requestRelativePath: "api/issues/pull?projectKey=someproj&languages=cs%2Cvbnet%2Ccpp%2Cc%2Cjs%2Cts%2Csecrets&branchName=main&resolvedOnly=false",
                responseMessage: new HttpResponseMessage { Content = new StreamContent(testedStream) });

            var testSubject = new GetIssuesRequest { ProjectKey = "someproj", Branch = "main", Logger = Mock.Of<ILogger>() };

            var response = await testSubject.InvokeAsync(httpClient, CancellationToken.None);
            response.Should().NotBeNullOrEmpty();
            messageHandler.VerifyAll();

            response.Length.Should().Be(1);

            response[0].Should().BeEquivalentTo(new SonarQubeIssue(
                issueKey: "issue1",
                filePath: "path1",
                hash: "hash1",
                message: "message1",
                moduleKey: null,
                ruleId: "rule1",
                isResolved: true,
                severity: SonarQubeIssueSeverity.Critical,
                creationTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(issuesPullQueryTimestamp.QueryTimestamp),
                lastUpdateTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(issuesPullQueryTimestamp.QueryTimestamp),
                textRange: new IssueTextRange(startLine: 3, endLine: 2, startOffset: 4, endOffset: 1),
                flows: null
            ));
        }

        [TestMethod]
        public async Task InvokeAsync_HasIssues_ReturnsCorrectIssues()
        {
            var issuesPullQueryTimestamp = new IssuesPullQueryTimestamp { QueryTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() };

            var issue1 = new IssueLite
            {
                Closed = false,
                CreationDate = issuesPullQueryTimestamp.QueryTimestamp,
                Key = "issue1",
                MainLocation = new Location
                {
                    FilePath = "path1",
                    Message = "message1",
                    TextRange = new TextRange
                    {
                        EndLineOffset = 1,
                        EndLine = 2,
                        Hash = "hash1",
                        StartLine = 3,
                        StartLineOffset = 4
                    }
                },
                Resolved = true,
                RuleKey = "rule1",
                Type = RuleType.Vulnerability,
                UserSeverity = Severity.Critical
            };

            var issue2 = new IssueLite
            {
                Closed = false,
                CreationDate = issuesPullQueryTimestamp.QueryTimestamp,
                Key = "issue2",
                MainLocation = new Location
                {
                    FilePath = "path2",
                    Message = "message2",
                    TextRange = new TextRange
                    {
                        EndLineOffset = 5,
                        EndLine = 6,
                        Hash = "hash2",
                        StartLine = 7,
                        StartLineOffset = 8
                    }
                },
                Resolved = false,
                RuleKey = "rule2",
                Type = RuleType.Bug,
                UserSeverity = Severity.Minor
            };

            var testedStream = CreateStreamRequest(issuesPullQueryTimestamp, issue1, issue2);

            var messageHandler = new Mock<HttpMessageHandler>();
            using var httpClient = new HttpClient(messageHandler.Object) { BaseAddress = new Uri("http://localhost") };

            MocksHelper.SetupHttpRequest(
                messageHandler,
                requestRelativePath: "api/issues/pull?projectKey=someproj&languages=cs%2Cvbnet%2Ccpp%2Cc%2Cjs%2Cts%2Csecrets&branchName=main&resolvedOnly=false",
                responseMessage: new HttpResponseMessage { Content = new StreamContent(testedStream) });

            var testSubject = new GetIssuesRequest { ProjectKey = "someproj", Branch = "main", Logger = Mock.Of<ILogger>() };

            var response = await testSubject.InvokeAsync(httpClient, CancellationToken.None);
            response.Should().NotBeNullOrEmpty();
            messageHandler.VerifyAll();

            response.Length.Should().Be(2);

            response[0].Should().BeEquivalentTo(new SonarQubeIssue(
                issueKey: "issue1",
                filePath: "path1",
                hash: "hash1",
                message: "message1",
                moduleKey: null,
                ruleId: "rule1",
                isResolved: true,
                severity: SonarQubeIssueSeverity.Critical,
                creationTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(issuesPullQueryTimestamp.QueryTimestamp),
                lastUpdateTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(issuesPullQueryTimestamp.QueryTimestamp),
                textRange: new IssueTextRange(startLine: 3, endLine: 2, startOffset: 4, endOffset: 1),
                flows: null
            ));

            response[1].Should().BeEquivalentTo(new SonarQubeIssue(
                issueKey: "issue2",
                filePath: "path2",
                hash: "hash2",
                message: "message2",
                moduleKey: null,
                ruleId: "rule2",
                isResolved: false,
                severity: SonarQubeIssueSeverity.Minor,
                creationTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(issuesPullQueryTimestamp.QueryTimestamp),
                lastUpdateTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(issuesPullQueryTimestamp.QueryTimestamp),
                textRange: new IssueTextRange(startLine: 7, endLine: 6, startOffset: 8, endOffset: 5),
                flows: null
            ));
        }

        private static MemoryStream CreateStreamRequest(IssuesPullQueryTimestamp timestamp, params IssueLite[] issues)
        {
            var memoryStream = new MemoryStream();
            timestamp.WriteDelimitedTo(memoryStream);

            foreach (var issue in issues)
            {
                issue.WriteDelimitedTo(memoryStream);
            }

            memoryStream.Flush();
            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}

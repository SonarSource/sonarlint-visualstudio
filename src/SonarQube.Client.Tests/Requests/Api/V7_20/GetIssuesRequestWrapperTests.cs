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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Api.V7_20;
using SonarQube.Client.Tests.Infra;
using static SonarQube.Client.Tests.Infra.MocksHelper;

namespace SonarQube.Client.Tests.Requests.Api.V7_20
{
    [TestClass]
    public class GetIssuesRequestWrapperTests
    {
        [TestMethod]
        public async Task InvokeAsync_NoIssueKeys_ExpectedPropertiesArePassedInMultipleRequests()
        {
            var testSubject = CreateTestSubject("aaaProject", "xStatus", "yBranch", null, "rule1", "project1");

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            SetupHttpRequest(handlerMock, EmptyGetIssuesResponse);

            _ = await testSubject.InvokeAsync(httpClient, CancellationToken.None);

            // The wrapper is expected to make three calls, for code smells, bugs, then vulnerabilities
            handlerMock.Invocations.Count.Should().Be(3);
            CheckExpectedQueryStringsParameters(handlerMock, 0, "aaaProject", "xStatus", "yBranch", "CODE_SMELL", "rule1", "project1");
            CheckExpectedQueryStringsParameters(handlerMock, 1, "aaaProject", "xStatus", "yBranch", "BUG", "rule1", "project1");
            CheckExpectedQueryStringsParameters(handlerMock, 2, "aaaProject", "xStatus", "yBranch", "VULNERABILITY", "rule1", "project1");
        }

        [TestMethod]
        public async Task InvokeAsync_HasIssueKeys_ExpectedPropertiesArePassedInASingleRequest()
        {
            var testSubject = CreateTestSubject("aaaProject", "xStatus", "yBranch", new[] { "issue1", "issue2" }, "rule1", "project1");

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            SetupHttpRequest(handlerMock, EmptyGetIssuesResponse);

            _ = await testSubject.InvokeAsync(httpClient, CancellationToken.None);

            // The wrapper is expected to make one call with the given issueKeys
            handlerMock.Invocations.Count.Should().Be(1);

            var actualQueryString = GetActualQueryStringForInvocation(handlerMock, 0);
            actualQueryString.Contains("?projects=aaaProject").Should().BeTrue();
            actualQueryString.Contains("&statuses=xStatus&").Should().BeTrue();
            actualQueryString.Contains("&branch=yBranch&").Should().BeTrue();
            actualQueryString.Contains("&issues=issue1%2Cissue2&").Should().BeTrue();
            actualQueryString.Contains("&rules=rule1").Should().BeTrue();
            actualQueryString.Contains("&components=project1").Should().BeTrue();
            actualQueryString.Contains("types").Should().BeFalse();
        }

        private static GetIssuesRequestWrapper CreateTestSubject(string projectKey, string statusesToRequest, string branch, string[] issueKeys, string ruleId, string componentKey)
        {
            var testSubject = new GetIssuesRequestWrapper
            {
                Logger = new TestLogger(),
                ProjectKey = projectKey,
                Statuses = statusesToRequest,
                Branch = branch,
                IssueKeys = issueKeys,
                RuleId = ruleId,
                ComponentKey = componentKey
            };

            return testSubject;
        }

        private static void CheckExpectedQueryStringsParameters(Mock<HttpMessageHandler> handlerMock, int invocationIndex,
            string expectedProject, string expectedStatues, string expectedBranch, string expectedTypes, string expectedRule, string expectedComponent)
        {
            var actualQueryString = GetActualQueryStringForInvocation(handlerMock, invocationIndex);

            Console.WriteLine($"Invocation [{invocationIndex}]: {actualQueryString}");
            actualQueryString.Contains($"?projects={expectedProject}").Should().BeTrue();
            actualQueryString.Contains($"&statuses={expectedStatues}&").Should().BeTrue();
            actualQueryString.Contains($"&branch={expectedBranch}&").Should().BeTrue();
            actualQueryString.Contains($"&types={expectedTypes}&").Should().BeTrue();
            actualQueryString.Contains($"&rules={expectedRule}&").Should().BeTrue();
            actualQueryString.Contains($"&components={expectedComponent}&").Should().BeTrue();
        }

        private static string GetActualQueryStringForInvocation(Mock<HttpMessageHandler> handlerMock, int invocationIndex)
        {
            var requestMessage = (HttpRequestMessage)handlerMock.Invocations[invocationIndex].Arguments[0];
            return requestMessage.RequestUri.Query;
        }
    }
}

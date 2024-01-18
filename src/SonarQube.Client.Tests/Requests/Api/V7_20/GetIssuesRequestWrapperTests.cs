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
using SonarQube.Client.Api;
using SonarQube.Client.Api.V7_20;
using SonarQube.Client.Tests.Infra;
using static SonarQube.Client.Tests.Infra.MocksHelper;

namespace SonarQube.Client.Tests.Requests.Api.V7_20
{
    [TestClass]
    public class GetIssuesRequestWrapperTests
    {
        private const string SonarQubeComponentPropertyName = "components";
        private const string SonarCloudComponentPropertyName = "componentKeys";
        
        [DataTestMethod]
        [DataRow(SonarQubeComponentPropertyName, DisplayName = "SonarQube")]
        [DataRow(SonarCloudComponentPropertyName, DisplayName = "SonarCloud")]
        public async Task InvokeAsync_SonarQube_NoIssueKeys_ExpectedPropertiesArePassedInMultipleRequests(string componentPropertyName)
        {
            var testSubject = CreateTestSubject(componentPropertyName, "aaaProject", "xStatus", "yBranch", null, "rule1", "project1");

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            SetupHttpRequest(handlerMock, EmptyGetIssuesResponse);

            _ = await testSubject.InvokeAsync(httpClient, CancellationToken.None);

            // The wrapper is expected to make three calls, for code smells, bugs, then vulnerabilities
            handlerMock.Invocations.Count.Should().Be(3);
            CheckExpectedQueryStringsParameters(componentPropertyName, handlerMock, 0, "aaaProject", "xStatus", "yBranch", "rule1", "project1", expectedTypes: "CODE_SMELL");
            CheckExpectedQueryStringsParameters(componentPropertyName, handlerMock, 1, "aaaProject", "xStatus", "yBranch", "rule1", "project1", expectedTypes: "BUG");
            CheckExpectedQueryStringsParameters(componentPropertyName, handlerMock, 2, "aaaProject", "xStatus", "yBranch", "rule1", "project1", expectedTypes: "VULNERABILITY");
        }

        [DataTestMethod]
        [DataRow(SonarQubeComponentPropertyName, DisplayName = "SonarQube")]
        [DataRow(SonarCloudComponentPropertyName, DisplayName = "SonarCloud")]
        public async Task InvokeAsync_HasIssueKeys_ExpectedPropertiesArePassedInASingleRequest(string componentPropertyName)
        {
            var issueKeys = new[] { "issue1", "issue2" };
            var testSubject = CreateTestSubject(componentPropertyName,"aaaProject", "xStatus", "yBranch", issueKeys, "rule1", "project1");

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            SetupHttpRequest(handlerMock, EmptyGetIssuesResponse);

            _ = await testSubject.InvokeAsync(httpClient, CancellationToken.None);

            // The wrapper is expected to make one call with the given issueKeys
            handlerMock.Invocations.Count.Should().Be(1);

            CheckExpectedQueryStringsParameters(componentPropertyName, handlerMock, 0, "aaaProject", "xStatus", "yBranch", "rule1", "project1", expectedKeys: issueKeys);
        }

        private static IGetIssuesRequest CreateTestSubject(string componentPropertyName, string projectKey, string statusesToRequest, string branch, string[] issueKeys, string ruleId, string componentKey)
        {
            return componentPropertyName switch
            {
                SonarQubeComponentPropertyName => new GetIssuesRequestWrapper<GetIssuesWithComponentSonarQubeRequest>
                {
                    Logger = new TestLogger(),
                    ProjectKey = projectKey,
                    Statuses = statusesToRequest,
                    Branch = branch,
                    IssueKeys = issueKeys,
                    RuleId = ruleId,
                    ComponentKey = componentKey
                },
                SonarCloudComponentPropertyName => new GetIssuesRequestWrapper<GetIssuesWithComponentSonarCloudRequest>
                {
                    Logger = new TestLogger(),
                    ProjectKey = projectKey,
                    Statuses = statusesToRequest,
                    Branch = branch,
                    IssueKeys = issueKeys,
                    RuleId = ruleId,
                    ComponentKey = componentKey
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static void CheckExpectedQueryStringsParameters(string componentKeyName,
            Mock<HttpMessageHandler> handlerMock,
            int invocationIndex,
            string expectedProject,
            string expectedStatues,
            string expectedBranch,
            string expectedRule,
            string expectedComponent,
            string expectedTypes = null,
            string[] expectedKeys = null)
        {
            var actualQueryString = GetActualQueryStringForInvocation(handlerMock, invocationIndex);

            Console.WriteLine($"Invocation [{invocationIndex}]: {actualQueryString}");
            actualQueryString.Contains($"?{componentKeyName}={expectedComponent}").Should().BeTrue();
            actualQueryString.Contains($"&projects={expectedProject}").Should().BeTrue();
            actualQueryString.Contains($"&statuses={expectedStatues}").Should().BeTrue();
            actualQueryString.Contains($"&branch={expectedBranch}").Should().BeTrue();
            actualQueryString.Contains($"&rules={expectedRule}").Should().BeTrue();
            
            if (expectedTypes != null)
            {
                actualQueryString.Contains($"&types={expectedTypes}").Should().BeTrue();
            }
            else
            {
                actualQueryString.Contains("types").Should().BeFalse();
            }

            if (expectedKeys != null)
            {
                var keys = string.Join("%2C", expectedKeys);
                actualQueryString.Contains($"&issues={keys}").Should().BeTrue();
            }
            else
            {
                actualQueryString.Contains("issues").Should().BeFalse();
            }

        }

        private static string GetActualQueryStringForInvocation(Mock<HttpMessageHandler> handlerMock, int invocationIndex)
        {
            var requestMessage = (HttpRequestMessage)handlerMock.Invocations[invocationIndex].Arguments[0];
            return requestMessage.RequestUri.Query;
        }
    }
}

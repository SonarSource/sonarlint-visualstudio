/*
 * SonarQube Client
 * Copyright (C) 2016-2022 SonarSource SA
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
        private const string EmptyResponse = @"{""total"":0,""p"":1,""ps"":10,""paging"":{""pageIndex"":1,""pageSize"":10,""total"":0},""effortTotal"":0,""debtTotal"":0,""issues"":[],""components"":[],""organizations"":[],""facets"":[]}";

        [TestMethod]
        public async Task InvokeAsync_ExpectedPropertiesArePassed()
        {
            var testSubject = CreateTestSubject("aaaProject", "xStatus", "yBranch");

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            SetupHttpRequest(handlerMock, EmptyResponse);

            _ = await testSubject.InvokeAsync(httpClient, CancellationToken.None);

            // The wrapper is expected to make three calls, for code smells, bugs, then vulnerabilities
            handlerMock.Invocations.Count().Should().Be(3);
            CheckExpectedQueryStringsParameters(handlerMock, 0, "aaaProject", "xStatus", "yBranch", "CODE_SMELL");
            CheckExpectedQueryStringsParameters(handlerMock, 1, "aaaProject", "xStatus", "yBranch", "BUG");
            CheckExpectedQueryStringsParameters(handlerMock, 2, "aaaProject", "xStatus", "yBranch", "VULNERABILITY");
        }

        private static GetIssuesRequestWrapper CreateTestSubject(string projectKey, string statusesToRequest, string branch)
        {
            var testSubject = new GetIssuesRequestWrapper();
            testSubject.Logger = new TestLogger();
            testSubject.ProjectKey = projectKey;
            testSubject.Statuses = statusesToRequest;
            testSubject.Branch = branch;

            return testSubject;
        }

        private static void CheckExpectedQueryStringsParameters(Mock<HttpMessageHandler> handlerMock, int invocationIndex,
            string expectedProject, string expectedStatues, string expectedBranch, string expectedTypes)
        {
            var actualQueryString = GetActualQueryStringForInvocation(handlerMock, invocationIndex);

            Console.WriteLine($"Invocation [{invocationIndex}]: {actualQueryString}");
            actualQueryString.Contains($"?projects={expectedProject}").Should().BeTrue();
            actualQueryString.Contains($"&statuses={expectedStatues}&").Should().BeTrue();
            actualQueryString.Contains($"&branch={expectedBranch}&").Should().BeTrue();
            actualQueryString.Contains($"&types={expectedTypes}&").Should().BeTrue();
        }

        private static string GetActualQueryStringForInvocation(Mock<HttpMessageHandler> handlerMock, int invocationIndex)
        {
            var requestMessage = (HttpRequestMessage)handlerMock.Invocations[invocationIndex].Arguments[0];
            return requestMessage.RequestUri.Query;
        }
    }
}

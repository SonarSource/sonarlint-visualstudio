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

namespace SonarQube.Client.Tests.Requests.Api.V7_20;

[TestClass]
public class GetIssuesWithComponentSonarCloudRequestTests
{
    [TestMethod]
    public async Task InvokeAsync_ComponentKeyNotSpecified_ComponentsAreNotIncludedInQueryString()
    {
        var testSubject = CreateTestSubject("any", "any", componentKey: null);

        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(ValidBaseAddress)
        };

        SetupHttpRequest(handlerMock, EmptyGetIssuesResponse);
        _ = await testSubject.InvokeAsync(httpClient, CancellationToken.None);

        var actualQueryString = GetSingleActualQueryString(handlerMock);
        actualQueryString.Should().NotContain("component");
    }

    [TestMethod]
    public async Task InvokeAsync_ComponentKeySpecified_ComponentsAreIncludedInQueryString()
    {
        var testSubject = CreateTestSubject("any", "any", componentKey: "project1");

        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(ValidBaseAddress)
        };

        SetupHttpRequest(handlerMock, EmptyGetIssuesResponse);
        _ = await testSubject.InvokeAsync(httpClient, CancellationToken.None);

        var actualQueryString = GetSingleActualQueryString(handlerMock);
        actualQueryString.Should().Contain("componentKeys=project1");
    }

    private static GetIssuesWithComponentSonarCloudRequest CreateTestSubject(string projectKey, string statusesToRequest, string componentKey = null)
    {
        var testSubject = new GetIssuesWithComponentSonarCloudRequest
        {
            Logger = new TestLogger(),
            ProjectKey = projectKey,
            Statuses = statusesToRequest,
            ComponentKey = componentKey
        };

        return testSubject;
    }

    private static string GetSingleActualQueryString(Mock<HttpMessageHandler> handlerMock)
    {
        handlerMock.Invocations.Count.Should().Be(1);
        var requestMessage = (HttpRequestMessage)handlerMock.Invocations[0].Arguments[0];
        return requestMessage.RequestUri.Query;
    }
}

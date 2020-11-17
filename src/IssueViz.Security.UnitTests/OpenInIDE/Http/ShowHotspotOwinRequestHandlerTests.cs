/*
 * SonarLint for Visual Studio
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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Owin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Http
{
    [TestClass]
    public class ShowHotspotOwinRequestHandlerTests
    {
        const string ORIGIN = "http://origin";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var apiRequestHandler = MefTestHelpers.CreateExport<IOpenInIDERequestHandler>(Mock.Of<IOpenInIDERequestHandler>());
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>());

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<ShowHotspotOwinRequestHandler, IOwinPathRequestHandler>(null, new[] { apiRequestHandler, loggerExport });
        }

        [TestMethod]
        public void ApiPath_ReturnsExpectedPath()
        {
            var testSubject = new ShowHotspotOwinRequestHandler(Mock.Of<IOpenInIDERequestHandler>(), Mock.Of<ILogger>());

            testSubject.ApiPath.Should().Be("/hotspots/show");
        }

        [TestMethod]
        [DataRow("", "server;project;hotspot")]
        [DataRow("server=http://s&hotspot=h&organization=o", "project")]
        [DataRow("project=p&hotspot=h&organization=o", "server")]
        [DataRow("project=p&server=http://s&organization=o", "hotspot")]
        [DataRow("organization=o", "hotspot;server;project")] // multiple missing
        [DataRow("projectXXX=p&server=http://s&organization=o", "project")] // partial match => missing
        [DataRow("PROJECT=p&SERVER=http://s", "hotspot")] // lookup is not case sensitive -> only hotspot is missing
        public async Task ProcessRequest_MissingParameter_Returns400StatusCode(string wholeQueryString, string missingParamList)
        {
            var testLogger = new TestLogger(logToConsole: true);
            var apiHandlerMock = new Mock<IOpenInIDERequestHandler>();

            var context = CreateContext(wholeQueryString);

            var testSubject = new ShowHotspotOwinRequestHandler(apiHandlerMock.Object, testLogger);

            // Act
            await testSubject.ProcessRequest(context);

            context.Response.StatusCode.Should().Be(400);

            // Note: passing a variable number of items to a test is messy. Here, it's
            // simpler to pass a string and split it.
            var missingParamNames = missingParamList.Split(';');
            foreach(var paramName in missingParamNames)
            {
                testLogger.AssertPartialOutputStringExists(paramName);
            }

            CheckApiHandlerNotCalled(apiHandlerMock);
        }

        [TestMethod]
        public async Task ProcessRequest_InvalidServerParameter_Returns400StatusCode()
        {
            const string invalidUrl = "NOT_A_URL";
            var testLogger = new TestLogger(logToConsole: true);
            var apiHandlerMock = new Mock<IOpenInIDERequestHandler>();

            var context = CreateContext($"server={invalidUrl}&project=any&hotspot=&any");

            var testSubject = new ShowHotspotOwinRequestHandler(apiHandlerMock.Object, testLogger);

            // Act
            await testSubject.ProcessRequest(context);

            context.Response.StatusCode.Should().Be(400);
            testLogger.AssertPartialOutputStringExists(invalidUrl);
            CheckApiHandlerNotCalled(apiHandlerMock);
        }

        [TestMethod]
        [DataRow("project=p&server=http://s&hotspot=h", "http://s", "p", "h", null)] // organization is optional
        [DataRow("project=p&server=http://s&hotspot=h&organization=o", "http://s", "p", "h", "o")]
        [DataRow("organization=O&hotspot=H&server=HTTP://S&project=P", "HTTP://S", "P", "H", "O")] // order is not important and value case is preserved
        [DataRow("PROJECT=pppp&SERVER=https://sss&hotSPOT=hhh", "https://sss", "pppp", "hhh", null)] // lookup is not case sensitive
        [DataRow("project=p&server=http://s&hotspot=h&unknown=oXXX", "http://s", "p", "h", null)]  // unknown parameters are ignored
        public async Task ProcessRequest_ValidRequest_HandlerCalledAndReturns200StatusCode(string wholeQueryString, string expectedServer, string expectedProject,
            string expectedHotspot, string expectedOrganization)
        {
            var apiHandlerMock = new Mock<IOpenInIDERequestHandler>();

            var context = CreateContext(wholeQueryString);

            var testSubject = new ShowHotspotOwinRequestHandler(apiHandlerMock.Object, new TestLogger(logToConsole: true));

            // Act
            await testSubject.ProcessRequest(context);

            context.Response.StatusCode.Should().Be(200);
            context.Response.Headers["Access-Control-Allow-Origin"].Should().Be(ORIGIN);
            CheckApiHandlerCalledWithExpectedValues(apiHandlerMock, expectedServer, expectedProject, expectedHotspot, expectedOrganization);
        }

        private static IOwinContext CreateContext(string wholeQueryString = null)
        {
            var context = new OwinContext();
            context.Request.QueryString = new QueryString(wholeQueryString);
            context.Request.Headers["Origin"] = ORIGIN;
            return context;
        }

        private static void CheckApiHandlerNotCalled(Mock<IOpenInIDERequestHandler> apiHandlerMock) =>
            apiHandlerMock.Invocations.Should().BeEmpty();

        private static void CheckApiHandlerCalledWithExpectedValues(Mock<IOpenInIDERequestHandler> apiHandlerMock,
            string server, string project, string hotspot, string organization)
        {
            apiHandlerMock.Verify(x => x.ShowHotspotAsync(It.Is<IShowHotspotRequest>(
                x => x.ServerUrl == new Uri(server) &&
                     x.ProjectKey == project &&
                     x.HotspotKey == hotspot &&
                     x.OrganizationKey == organization)));
        }
    }
}

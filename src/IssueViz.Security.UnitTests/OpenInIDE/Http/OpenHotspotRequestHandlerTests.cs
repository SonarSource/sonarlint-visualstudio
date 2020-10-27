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
    public class OpenHotspotRequestHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var apiRequestHandler = MefTestHelpers.CreateExport<IOpenInIDERequestHandler>(Mock.Of<IOpenInIDERequestHandler>());
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>());

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<OpenHotspotRequestHandler, IOwinPathRequestHandler>(null, new[] { apiRequestHandler, loggerExport });
        }

        [TestMethod]
        [DataRow("server=s&hotspot=h&organization=o", "project")]
        [DataRow("project=p&hotspot=h&organization=o", "server")]
        [DataRow("project=p&server=s&organization=o", "hotspot")]
        [DataRow("organization=o", "hotspot;server;project")] // multiple missing
        [DataRow("projectXXX=p&server=s&organization=o", "project")] // partial match => missing
        [DataRow("PROJECT=p&SERVER=s", "hotspot")] // lookup is not case sensitive -> only hotspot is missing
        public void ProcessRequest_MissingParameter_Returns400StatusCode(string wholeQueryString, string missingParamList)
        {
            var testLogger = new TestLogger(logToConsole: true);
            var apiHandlerMock = new Mock<IOpenInIDERequestHandler>();

            var context = CreateContext(wholeQueryString);

            var testSubject = new OpenHotspotRequestHandler(apiHandlerMock.Object, testLogger);

            // Act
            testSubject.ProcessRequest(context);

            context.Response.StatusCode.Should().Be(400);

            var missingParamNames = missingParamList.Split(';');
            foreach(var paramName in missingParamNames)
            {
                testLogger.AssertPartialOutputStringExists(paramName);
            }

            CheckApiHandlerNotCalled(apiHandlerMock);
        }

        [TestMethod]
        [DataRow("project=p&server=s&hotspot=h", "s", "p", "h", null)] // organization is optional
        [DataRow("project=p&server=s&hotspot=h&organization=o", "s", "p", "h", "o")]
        [DataRow("organization=O&hotspot=H&server=S&project=P", "S", "P", "H", "O")] // order is not important and value case is preserved
        [DataRow("PROJECT=pppp&SERVER=sss&hotSPOT=hhh", "sss", "pppp", "hhh", null)] // lookup is not case sensitive
        public void ProcessRequest_ValidRequest_HandlerCalledAndReturns200StatusCode(string wholeQueryString, string expectedServer, string expectedProject,
            string expectedHotspot, string expectedOrganization)
        {
            var apiHandlerMock = new Mock<IOpenInIDERequestHandler>();

            var context = CreateContext(wholeQueryString);

            var testSubject = new OpenHotspotRequestHandler(apiHandlerMock.Object, new TestLogger(logToConsole: true));

            // Act
            testSubject.ProcessRequest(context);

            context.Response.StatusCode.Should().Be(200);
            CheckApiHandlerCalledWithExpectedValues(apiHandlerMock, expectedServer, expectedProject, expectedHotspot, expectedOrganization);
        }

        private static IOwinContext CreateContext(string wholeQueryString = null)
        {
            var context = new OwinContext();
            context.Request.QueryString = new QueryString(wholeQueryString);
            return context;
        }

        private static void CheckApiHandlerNotCalled(Mock<IOpenInIDERequestHandler> apiHandlerMock) =>
            apiHandlerMock.Invocations.Should().BeEmpty();

        private static void CheckApiHandlerCalledWithExpectedValues(Mock<IOpenInIDERequestHandler> apiHandlerMock,
            string server, string project, string hotspot, string organization)
        {
            apiHandlerMock.Verify(x => x.ShowHotspot(It.Is<IShowHotspotRequest>(
                x => x.ServerUrl == server &&
                     x.ProjectKey == project &&
                     x.HotspotKey == hotspot &&
                     x.OrganizationKey == organization)));
        }
    }
}

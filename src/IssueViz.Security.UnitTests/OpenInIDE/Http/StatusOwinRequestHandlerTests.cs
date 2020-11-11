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

using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Owin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Http
{
    [TestClass]
    public class StatusOwinRequestHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var apiRequestHandler = MefTestHelpers.CreateExport<IOpenInIDERequestHandler>(Mock.Of<IOpenInIDERequestHandler>());
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>());

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<StatusOwinRequestHandler, IOwinPathRequestHandler>(null, new[] { apiRequestHandler, loggerExport });
        }

        [TestMethod]
        public void ApiPath_ReturnsExpectedPath()
        {
            var testSubject = new StatusOwinRequestHandler(Mock.Of<IOpenInIDERequestHandler>(), Mock.Of<ILogger>());

            testSubject.ApiPath.Should().Be("/status");
        }

        [TestMethod]
        public async Task ProcessRequest_WritesExpectedResponse()
        {
            const string expectedIdeName = "Visual Studio";
            const string expectedDescription = "myproject.csproj";

            var apiResponse = new StatusResponse(expectedIdeName, expectedDescription);

            var testLogger = new TestLogger(logToConsole: true);
            var apiHandlerMock = new Mock<IOpenInIDERequestHandler>();
            apiHandlerMock.Setup(x => x.GetStatusAsync()).Returns(Task.FromResult<IStatusResponse>(apiResponse));

            var responseStream = new MemoryStream();
            var context = new OwinContext();
            context.Response.Body = responseStream;

            var testSubject = new StatusOwinRequestHandler(apiHandlerMock.Object, testLogger);

            // Act
            await testSubject.ProcessRequest(context)
                .ConfigureAwait(false);

            context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var responseData = Deserialize(responseStream);
            responseData["ideName"].Type.Should().Be(JTokenType.String);
            responseData["ideName"].ToString().Should().Be(expectedIdeName);

            responseData["description"].Type.Should().Be(JTokenType.String);
            responseData["description"].ToString().Should().Be(expectedDescription);
        }

        private static JObject Deserialize(MemoryStream stream)
        {
            stream.Position = 0;
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();
            var data = serializer.Deserialize(jsonReader);
            return data as JObject;
        }
    }
}

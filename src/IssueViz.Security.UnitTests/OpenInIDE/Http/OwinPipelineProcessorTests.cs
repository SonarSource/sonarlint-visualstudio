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
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Owin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Http
{
    [TestClass]
    public class OwinPipelineProcessorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var requestHandlersExport = MefTestHelpers.CreateExport<IEnumerable<IOwinPathRequestHandler>>(Array.Empty<IOwinPathRequestHandler>());
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>());

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<OwinPipelineProcessor, IOwinPipelineProcessor>(null, new[] { requestHandlersExport, loggerExport });
        }

        [TestMethod]
        public void Ctor_HandlersAreRegistered()
        {
            var handler1 = CreateHandler("path1/");
            var handler2 = CreateHandler("path2/");

            var testSubject = new OwinPipelineProcessor(new[] { handler1, handler2 }, new TestLogger());

            testSubject.PathToHandlerMap.Count.Should().Be(2);
            testSubject.PathToHandlerMap["path1/"].Should().BeSameAs(handler1);
            testSubject.PathToHandlerMap["path2/"].Should().BeSameAs(handler2);
        }

        [TestMethod]
        [DataRow("/unknown")]
        [DataRow("/sonarlint/api/unknown")]
        [DataRow("/sonarlint")]
        [DataRow("/sonarlint/api")]
        [DataRow("/handled")]
        [DataRow("/api/handled")]
        [DataRow("/SONARLINT/API/HANDLED")]
        public async Task ProcessRequest_UnrecognisedPath_Returns404(string requestedPath)
        {
            var testLogger = new TestLogger(logToConsole: true);
            var context = CreateOwinContext(requestedPath);

            var handler = CreateHandler("/sonarlint/api/handled");
            var testSubject = new OwinPipelineProcessor(new[] { handler }, testLogger);

            await testSubject.ProcessRequest(context.Environment).ConfigureAwait(false);

            context.Response.StatusCode.Should().Be(404);
            testLogger.AssertPartialOutputStringExists(requestedPath);
        }

        [TestMethod]
        public async Task ProcessRequest_RecognisedPath_ReturnsStatusSetByHandler()
        {
            var testLogger = new TestLogger(logToConsole: true);
            const int expectedStatusCode = 12345;
            const string handledRequestPath = "/sonarlint/api/handled";
            var context = CreateOwinContext(handledRequestPath);

            var handler = CreateHandler(handledRequestPath, expectedStatusCode);
            var testSubject = new OwinPipelineProcessor(new[] { handler }, testLogger);

            await testSubject.ProcessRequest(context.Environment).ConfigureAwait(false);

            context.Response.StatusCode.Should().Be(expectedStatusCode);
            testLogger.AssertPartialOutputStringExists(handledRequestPath);
        }

        [TestMethod]
        public void ProcessRequest_ExceptionInHandler_IsLoggedAndRethrown()
        {
            var testLogger = new TestLogger(logToConsole: true);
            const string handledRequestPath = "/path";
            var context = CreateOwinContext(handledRequestPath);

            const string expectedErrorMessage = "exception thrown by test";
            var handler = CreateHandler(handledRequestPath, processOp: () => throw new IndexOutOfRangeException(expectedErrorMessage));
            var testSubject = new OwinPipelineProcessor(new[] { handler }, testLogger);

            Func<Task> act = () => testSubject.ProcessRequest(context.Environment);

            act.Should().ThrowExactly<IndexOutOfRangeException>().And.Message.Should().Be(expectedErrorMessage);
            testLogger.AssertPartialOutputStringExists(expectedErrorMessage);
        }

        private static IOwinPathRequestHandler CreateHandler(string path, int statusCodeToReturn = (int)HttpStatusCode.OK, Action processOp = null)
        {
            var handlerMock = new Mock<IOwinPathRequestHandler>();
            handlerMock.Setup(x => x.ApiPath).Returns(path);

            handlerMock.Setup(x => x.ProcessRequest(It.IsAny<IOwinContext>()))
                .Callback<IOwinContext>( x =>
                     {
                         x.Response.StatusCode = statusCodeToReturn;
                         processOp?.Invoke();
                     });

            return handlerMock.Object;
        }

        private static IOwinContext CreateOwinContext(string requestedPath)
        {
            var context = new OwinContext();
            context.Request.Path = new PathString(requestedPath);

            return context;
        }
    }
}

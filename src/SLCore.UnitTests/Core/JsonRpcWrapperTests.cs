/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using NSubstitute;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Monitoring;
using StreamJsonRpc.Protocol;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Core;

[TestClass]
public class JsonRpcWrapperTests
{
    [TestMethod]
    public void CreateErrorDetails_InternalError_ReportsException_WithMethodName()
    {
        var monitoringService = Substitute.For<IMonitoringService>();
        using var sending = new MemoryStream();
        using var receiving = new MemoryStream();
        var testSubject = new TestableJsonRpcWrapper(sending, receiving, monitoringService);
        var request = new JsonRpcRequest { Method = "getFileExclusions" };
        var expected = new InvalidOperationException("boom");

        var result = testSubject.InvokeCreateErrorDetails(request, expected);

        result.Code.Should().Be(JsonRpcErrorCode.InternalError);
        monitoringService.Received(1).ReportException(expected, "JsonRpcWrapper.CreateErrorDetails:getFileExclusions");
    }

    [TestMethod]
    public void CreateErrorDetails_InternalError_NullMethod_ReportsException_WithUnknownMethod()
    {
        var monitoringService = Substitute.For<IMonitoringService>();
        using var sending = new MemoryStream();
        using var receiving = new MemoryStream();
        var testSubject = new TestableJsonRpcWrapper(sending, receiving, monitoringService);
        var request = new JsonRpcRequest();
        var expected = new InvalidOperationException("boom");

        var result = testSubject.InvokeCreateErrorDetails(request, expected);

        result.Code.Should().Be(JsonRpcErrorCode.InternalError);
        monitoringService.Received(1).ReportException(expected, "JsonRpcWrapper.CreateErrorDetails:unknown");
    }

    [TestMethod]
    public void CreateErrorDetails_NonInternalError_DoesNotReportException()
    {
        var monitoringService = Substitute.For<IMonitoringService>();
        using var sending = new MemoryStream();
        using var receiving = new MemoryStream();
        var testSubject = new TestableJsonRpcWrapper(sending, receiving, monitoringService);
        var request = new JsonRpcRequest { Method = "someMethod" };
        var operationCanceled = new OperationCanceledException("cancelled");

        var result = testSubject.InvokeCreateErrorDetails(request, operationCanceled);

        result.Code.Should().Be(JsonRpcErrorCode.RequestCanceled);
        monitoringService.DidNotReceive().ReportException(Arg.Any<Exception>(), Arg.Any<string>());
    }

    private sealed class TestableJsonRpcWrapper : JsonRpcWrapper
    {
        public TestableJsonRpcWrapper(Stream sendingStream, Stream receivingStream, IMonitoringService monitoringService)
            : base(sendingStream, receivingStream, monitoringService)
        {
        }

        public JsonRpcError.ErrorDetail InvokeCreateErrorDetails(JsonRpcRequest request, Exception exception)
            => base.CreateErrorDetails(request, exception);
    }
}

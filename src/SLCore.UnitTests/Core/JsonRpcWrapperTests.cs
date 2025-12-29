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
    public void CreateErrorDetails_ReportsException_WithMethodName()
    {
        var monitoringService = Substitute.For<IMonitoringService>();
        using var sending = new MemoryStream();
        using var receiving = new MemoryStream();
        var testSubject = new TestableJsonRpcWrapper(sending, receiving, monitoringService);

        var request = new JsonRpcRequest
        {
            Method = "getFileExclusions"
        };
        var expected = new InvalidOperationException("boom");

        _ = testSubject.InvokeCreateErrorDetails(request, expected);

        monitoringService.Received(1).ReportException(expected, "JsonRpcWrapper.CreateErrorDetails:getFileExclusions");
    }

    [TestMethod]
    public void CreateErrorDetails_NullMethod_ReportsException_WithUnknownMethod()
    {
        var monitoringService = Substitute.For<IMonitoringService>();
        using var sending = new MemoryStream();
        using var receiving = new MemoryStream();
        var testSubject = new TestableJsonRpcWrapper(sending, receiving, monitoringService);

        var request = new JsonRpcRequest();
        var expected = new InvalidOperationException("boom");

        _ = testSubject.InvokeCreateErrorDetails(request, expected);

        monitoringService.Received(1).ReportException(expected, "JsonRpcWrapper.CreateErrorDetails:unknown");
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

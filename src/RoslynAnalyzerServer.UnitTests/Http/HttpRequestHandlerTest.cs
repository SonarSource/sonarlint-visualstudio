/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using System.Text;
using SonarLint.VisualStudio.Integration.TestInfrastructure;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Http;

[TestClass]
public class HttpRequestHandlerTest
{
    private IHttpListenerContext context = null!;
    private IHttpListenerRequest request = null!;
    private IHttpListenerResponse response = null!;
    private Stream stream = null!;
    private HttpRequestHandler testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        request = Substitute.For<IHttpListenerRequest>();
        response = Substitute.For<IHttpListenerResponse>();
        context = Substitute.For<IHttpListenerContext>();
        context.Request.Returns(request);
        context.Response.Returns(response);
        stream = Substitute.For<Stream>();
        stream.CanWrite.Returns(true);
        context.Response.OutputStream.Returns(stream);
        testSubject = new HttpRequestHandler();
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.RequestEntityTooLarge)]
    [DataRow(HttpStatusCode.RequestTimeout)]
    [DataRow(HttpStatusCode.LengthRequired)]
    public void CloseRequest_SetsStatusCodeAndCloses_ReturnsVoid(HttpStatusCode statusCode)
    {
        testSubject.CloseRequest(context, statusCode);

        response.Received().StatusCode = (int)statusCode;
        response.Received().Close();
    }

    [TestMethod]
    public void CloseRequest_ConnectionBroken_DoesNotWriteResponse()
    {
        stream.CanWrite.Returns(false);

        testSubject.CloseRequest(context, HttpStatusCode.BadRequest);

        response.DidNotReceiveWithAnyArgs().StatusCode = default;
        response.DidNotReceiveWithAnyArgs().Close();
    }

    [TestMethod]
    public async Task SendResponse_WritesCorrectlySerializedDiagnostics()
    {
        response.OutputStream.Returns(new MemoryStream());
        var expectedString = "{\"Diagnostics\":[{\"Id\":\"id1\"}]}";

        await testSubject.SendResponseAsync(context, expectedString);

        response.Received().ContentLength64 = Encoding.UTF8.GetBytes(expectedString).Length;
        response.Received().StatusCode = (int)HttpStatusCode.OK;
    }

    [TestMethod]
    public async Task SendResponse_ClosesOutputStream()
    {
        var outputStream = new MemoryStream();
        response.OutputStream.Returns(outputStream);

        await testSubject.SendResponseAsync(context, "{\"Diagnostics\":[}");

        outputStream.CanRead.Should().BeFalse();
    }

    [TestMethod]
    public async Task SendResponse_ConnectionBroken_DoesNotWriteResponse()
    {
        stream.CanWrite.Returns(false);

        await testSubject.SendResponseAsync(context, "{\"Diagnostics\":[}");

        stream.DidNotReceiveWithAnyArgs().WriteAsync(default, default, default, default).IgnoreAwaitForAssert();
        stream.DidNotReceiveWithAnyArgs().Write(default, default, default);
        response.DidNotReceiveWithAnyArgs().StatusCode = default;
        response.DidNotReceiveWithAnyArgs().Close();
    }
}

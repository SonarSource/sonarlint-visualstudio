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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Http;

[TestClass]
public class AnalysisRequestHandlerTest
{
    private const string ValidToken = "token";
    private const string InvalidToken = "wrong";
    private const string AnalyzeUrl = "http://localhost/analyze";
    private const int DefaultPort = 1234;
    private const int MaxRequestBodyBytes = 100;
    private const string AuthTokenHeader = "X-Auth-Token";
    private const string HttpMethodPost = "POST";
    private const string DiagnosticId = "S100";
    private static readonly FileUri FileUri = new("C:\\File.cs");
    private IHttpServerConfiguration configuration = null!;
    private IHttpListenerContext context = null!;

    private ILogger logger = null!;

    // Substitute fields for test doubles
    private IHttpListenerRequest request = null!;
    private IHttpListenerResponse response = null!;
    private AnalysisRequestHandler testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        configuration = Substitute.For<IHttpServerConfiguration>();
        configuration.Token.Returns(ValidToken.ToSecureString());
        configuration.MaxRequestBodyBytes.Returns(MaxRequestBodyBytes);
        testSubject = new AnalysisRequestHandler(logger, configuration);
        request = Substitute.For<IHttpListenerRequest>();
        response = Substitute.For<IHttpListenerResponse>();
        context = Substitute.For<IHttpListenerContext>();
        context.Request.Returns(request);
        context.Response.Returns(response);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<AnalysisRequestHandler, IAnalysisRequestHandler>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IHttpServerConfiguration>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<AnalysisRequestHandler>();

    [TestMethod]
    public void Ctor_LoggerSetsContext() => logger.Received(1).ForContext(Resources.HttpServerLogContext).ForContext(nameof(RoslynAnalysisHttpServer));

    [TestMethod]
    public void IsValidRequest_RemoteEndpointNull_ReturnsFalse()
    {
        request.RemoteEndPoint.Returns((IPEndPoint?)null);

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeFalse();
        response.Received().StatusCode = (int)HttpStatusCode.Forbidden;
    }

    [TestMethod]
    public void IsValidRequest_NotLocalRequest_ReturnsFalse()
    {
        request.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Parse("8.8.8.8"), DefaultPort));

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeFalse();
        response.Received().StatusCode = (int)HttpStatusCode.Forbidden;
    }

    [TestMethod]
    public void IsValidRequest_Loopback_ReturnsTrue()
    {
        MockValidRequest();
        request.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, DefaultPort));

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeTrue();
    }

    [TestMethod]
    public void IsValidRequest_IPv6Loopback_ReturnsTrue()
    {
        MockValidRequest();
        request.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.IPv6Loopback, DefaultPort));

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeTrue();
    }

    [TestMethod]
    public void IsValidRequest_TokenInvalid_ReturnsFalse()
    {
        MockValidRequest();
        request.Headers.Returns(new WebHeaderCollection { [AuthTokenHeader] = InvalidToken });

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeFalse();
        response.Received().StatusCode = (int)HttpStatusCode.Unauthorized;
    }

    [TestMethod]
    [DataRow("Bearer")]
    [DataRow("X-Authorization")]
    [DataRow("X-Api-Key")]
    public void IsValidRequest_TokenInInvalidHeader_ReturnsFalse(string wrongAuthenticationHeader)
    {
        MockValidRequest();
        request.Headers.Returns(new WebHeaderCollection { [wrongAuthenticationHeader] = InvalidToken });

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeFalse();
        response.Received().StatusCode = (int)HttpStatusCode.Unauthorized;
    }

    [TestMethod]
    public void IsValidRequest_ValidInValidHeader_ReturnsTrue()
    {
        MockValidRequest();
        request.Headers.Returns(new WebHeaderCollection { [AuthTokenHeader] = ValidToken });

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("http://localhost/invalid")]
    [DataRow("http://localhost/analyze/abc")]
    public void IsValidRequest_UrlInvalid_ReturnsFalse(string invalidUrl)
    {
        MockValidRequest();
        request.Url.Returns(new Uri(invalidUrl));

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeFalse();
        response.Received().StatusCode = (int)HttpStatusCode.NotFound;
    }

    [TestMethod]
    [DataRow("GET")]
    [DataRow("PUT")]
    [DataRow("PATCH")]
    [DataRow("DELETE")]
    public void IsValidRequest_MethodInvalid_ReturnsFalse(string httpMethod)
    {
        MockValidRequest();
        request.HttpMethod.Returns(httpMethod);

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeFalse();
        response.Received().StatusCode = (int)HttpStatusCode.NotFound;
    }

    [TestMethod]
    public void IsValidRequest_ContentLengthExceeded_ReturnsFalse()
    {
        MockValidRequest();
        request.ContentLength64.Returns(MaxRequestBodyBytes + 1);

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeFalse();
        response.Received().StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
        logger.Received().LogVerbose(Resources.BodyLengthExceeded, context.Request.ContentLength64, configuration.MaxRequestBodyBytes);
    }

    [TestMethod]
    public void IsValidRequest_ContentLengthNotExceeded_ReturnsTrue()
    {
        MockValidRequest();
        request.ContentLength64.Returns(MaxRequestBodyBytes);

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeTrue();
    }

    [TestMethod]
    public void IsValidRequest_AllValid_ReturnsTrue()
    {
        MockValidRequest();

        var isValid = testSubject.IsValidRequest(context);

        isValid.Should().BeTrue();
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
    public async Task ParseAnalysisRequestBody_DeserializationFails_ReturnsNull()
    {
        var unexpectedBodyContent = @"
{
  ""$type"": ""System.Windows.Data.ObjectDataProvider, PresentationFramework"",
  ""MethodName"": ""Start"",
  ""ObjectInstance"": {
    ""$type"": ""System.Diagnostics.Process, System"",
    ""StartInfo"": {
      ""$type"": ""System.Diagnostics.ProcessStartInfo, System"",
      ""FileName"": ""malicious.exe""
    }
  }
}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(unexpectedBodyContent));
        request.InputStream.Returns(stream);
        request.ContentEncoding.Returns(Encoding.UTF8);

        var result = await testSubject.ParseAnalysisRequestBody(context);

        result.Should().BeNull();
        response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ParseAnalysisRequestBody_FileNamesMissing_ReturnsNull()
    {
        var stream = new MemoryStream("{\"ActiveRules\":[]}"u8.ToArray());
        request.InputStream.Returns(stream);
        request.ContentEncoding.Returns(Encoding.UTF8);

        var result = await testSubject.ParseAnalysisRequestBody(context);

        result.Should().BeNull();
        response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ParseAnalysisRequestBody_FileNamesEmpty_ReturnsNull()
    {
        var stream = new MemoryStream("{\"FileNames\":[],\"ActiveRules\":[]}"u8.ToArray());
        request.InputStream.Returns(stream);
        request.ContentEncoding.Returns(Encoding.UTF8);

        var result = await testSubject.ParseAnalysisRequestBody(context);

        result.Should().BeNull();
        response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ParseAnalysisRequestBody_RequestBodyValid_ReturnsExpectedModel()
    {
        var validRequestJson = $"{{\"FileNames\":[\"{FileUri}\"],\"ActiveRules\":[{{\"RuleKey\":\"{DiagnosticId}\"}}]}}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(validRequestJson));
        request.InputStream.Returns(stream);
        request.ContentEncoding.Returns(Encoding.UTF8);

        var result = await testSubject.ParseAnalysisRequestBody(context);

        result.Should().NotBeNull();
        result!.FileNames.Should().HaveCount(1);
        result.FileNames[0].Should().Be(FileUri);
        result.ActiveRules.Should().HaveCount(1);
        result.ActiveRules[0].RuleKey.Should().Be(DiagnosticId);
    }

    [TestMethod]
    public async Task SendResponse_WritesCorrectlySerializedDiagnostics()
    {
        response.OutputStream.Returns(new MemoryStream());
        var expectedString = "{\"Diagnostics\":[{\"Id\":\"id1\"}]}";

        await testSubject.SendResponse(context, [new DiagnosticDto("id1")]);

        response.Received().ContentLength64 = Encoding.UTF8.GetBytes(expectedString).Length;
        response.Received().StatusCode = (int)HttpStatusCode.OK;
    }

    [TestMethod]
    public async Task SendResponse_ClosesOutputStream()
    {
        var outputStream = new MemoryStream();
        response.OutputStream.Returns(outputStream);
        var diagnostics = new List<DiagnosticDto> { new(DiagnosticId) };

        await testSubject.SendResponse(context, diagnostics);

        outputStream.CanRead.Should().BeFalse();
    }

    private void MockValidRequest()
    {
        request.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, DefaultPort));
        request.Headers.Returns(new WebHeaderCollection { [AuthTokenHeader] = ValidToken });
        request.HttpMethod.Returns(HttpMethodPost);
        request.Url.Returns(new Uri(AnalyzeUrl));
        request.ContentLength64.Returns(MaxRequestBodyBytes);
    }
}

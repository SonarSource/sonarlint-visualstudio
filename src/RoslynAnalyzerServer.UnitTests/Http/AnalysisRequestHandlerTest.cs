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
    private const string CancelUrl = "http://localhost/cancel";
    private const string UnknownUrl = "http://localhost/SOMERANDOMURL";
    private const int DefaultPort = 1234;
    private const int MaxRequestBodyBytes = 100;
    private const string AuthTokenHeader = "X-Auth-Token";
    private const string HttpMethodPost = "POST";
    private const string DiagnosticId = "S100";
    private static readonly Guid AnalysisId = Guid.NewGuid();
    private static readonly FileUri FileUri = new("C:\\File.cs");
    private IHttpServerSettings settings = null!;
    private IHttpServerConfigurationProvider configurationProvider = null!;
    private IHttpListenerContext context = null!;
    private ILogger logger = null!;
    private IHttpListenerRequest request = null!;
    private IHttpListenerResponse response = null!;
    private AnalysisRequestHandler testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        settings = Substitute.For<IHttpServerSettings>();
        settings.MaxRequestBodyBytes.Returns(MaxRequestBodyBytes);
        MockConfigurationProvider();
        testSubject = new AnalysisRequestHandler(logger, settings, configurationProvider);
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
            MefTestHelpers.CreateExport<IHttpServerSettings>(),
            MefTestHelpers.CreateExport<IHttpServerConfigurationProvider>()
        );

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<AnalysisRequestHandler>();

    [TestMethod]
    public void Ctor_LoggerSetsContext() => logger.Received(1).ForContext(Resources.HttpServerLogContext).ForContext(nameof(RoslynAnalysisHttpServer));

    [TestMethod]
    public void ValidateRequest_RemoteEndpointNull_ReturnsForbidden()
    {
        request.RemoteEndPoint.Returns((IPEndPoint?)null);

        var result = testSubject.ValidateRequest(context.Request, out var errorCode, out _);

        result.Should().BeFalse();
        errorCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public void ValidateRequest_NotLocalRequest_ReturnsForbidden()
    {
        request.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Parse("8.8.8.8"), DefaultPort));

        var result = testSubject.ValidateRequest(context.Request, out var errorCode, out _);

        result.Should().BeFalse();
        errorCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public void ValidateRequest_Loopback_ReturnsOK()
    {
        MockValidRequest();
        request.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, DefaultPort));

        var result = testSubject.ValidateRequest(context.Request, out _, out _);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void ValidateRequest_IPv6Loopback_ReturnsOK()
    {
        MockValidRequest();
        request.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.IPv6Loopback, DefaultPort));

        var result = testSubject.ValidateRequest(context.Request, out _, out _);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void ValidateRequest_TokenInvalid_ReturnsUnauthorized()
    {
        MockValidRequest();
        request.Headers.Returns(new WebHeaderCollection { [AuthTokenHeader] = InvalidToken });

        var result = testSubject.ValidateRequest(context.Request, out var errorCode, out _);

        result.Should().BeFalse();
        errorCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    [DataRow("Bearer")]
    [DataRow("X-Authorization")]
    [DataRow("X-Api-Key")]
    public void ValidateRequest_TokenInInvalidHeader_ReturnsUnauthorized(string wrongAuthenticationHeader)
    {
        MockValidRequest();
        request.Headers.Returns(new WebHeaderCollection { [wrongAuthenticationHeader] = InvalidToken });

        var result = testSubject.ValidateRequest(context.Request, out var errorCode, out _);

        result.Should().BeFalse();
        errorCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public void ValidateRequest_ValidInValidHeader_ReturnsOK()
    {
        MockValidRequest();
        request.Headers.Returns(new WebHeaderCollection { [AuthTokenHeader] = ValidToken });

        var result = testSubject.ValidateRequest(context.Request, out _, out _);

        result.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("http://localhost/invalid")]
    [DataRow("http://localhost/analyze/abc")]
    public void ValidateRequest_UrlInvalid_ReturnsNotFound(string invalidUrl)
    {
        MockValidRequest();
        request.Url.Returns(new Uri(invalidUrl));

        var result = testSubject.ValidateRequest(context.Request, out var errorCode, out _);

        result.Should().BeFalse();
        errorCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    [DataRow("GET")]
    [DataRow("PUT")]
    [DataRow("PATCH")]
    [DataRow("DELETE")]
    public void ValidateRequest_MethodInvalid_ReturnsNotFound(string httpMethod)
    {
        MockValidRequest();
        request.HttpMethod.Returns(httpMethod);

        var result = testSubject.ValidateRequest(context.Request, out var errorCode, out _);

        result.Should().BeFalse();
        errorCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public void ValidateRequest_ContentLengthExceeded_ReturnsRequestEntityTooLarge()
    {
        MockValidRequest();
        request.ContentLength64.Returns(MaxRequestBodyBytes + 1);

        var result = testSubject.ValidateRequest(context.Request, out var errorCode, out _);

        result.Should().BeFalse();
        errorCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        logger.Received().LogVerbose(Resources.BodyLengthExceeded, context.Request.ContentLength64, settings.MaxRequestBodyBytes);
    }

    [TestMethod]
    public void ValidateRequest_ContentLengthNotExceeded_ReturnsOK()
    {
        MockValidRequest();
        request.ContentLength64.Returns(MaxRequestBodyBytes);

        var result = testSubject.ValidateRequest(context.Request, out _, out _);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void ValidateRequest_AllValid_ReturnsOK()
    {
        MockValidRequest();

        var result = testSubject.ValidateRequest(context.Request, out _, out _);

        result.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(AnalyzeUrl, RequestType.Analyze)]
    [DataRow(CancelUrl, RequestType.Cancel)]
    [DataRow(UnknownUrl, RequestType.Unknown)]
    public void ValidateRequest_ValidRequestType_SetsCorrectRequestType(string url, RequestType expectedRequestType)
    {
        MockValidRequest();
        request.Url.Returns(new Uri(url));

        var result = testSubject.ValidateRequest(context.Request, out _, out var requestType);

        result.Should().Be(expectedRequestType != RequestType.Unknown);
        requestType.Should().Be(expectedRequestType);
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

        var result = await AnalysisRequestHandler.ParseAnalysisRequestBodyAsync<AnalysisRequest>(context.Request);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task ParseAnalysisRequestBody_FileNamesMissing_ReturnsNull()
    {
        var stream = new MemoryStream("""{"ActiveRules":[]}"""u8.ToArray());
        request.InputStream.Returns(stream);
        request.ContentEncoding.Returns(Encoding.UTF8);

        var result = await AnalysisRequestHandler.ParseAnalysisRequestBodyAsync<AnalysisRequest>(context.Request);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task ParseAnalysisRequestBody_FileNamesEmpty_ReturnsNull()
    {
        var stream = new MemoryStream("""{"FileNames":[],"ActiveRules":[]}"""u8.ToArray());
        request.InputStream.Returns(stream);
        request.ContentEncoding.Returns(Encoding.UTF8);

        var result = await AnalysisRequestHandler.ParseAnalysisRequestBodyAsync<AnalysisRequest>(context.Request);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task ParseAnalysisRequestBody_RequestBodyValid_ReturnsExpectedModel()
    {
        var validRequestJson = $$"""{"FileUris":["{{FileUri}}"],"ActiveRules":[{"RuleId":"{{DiagnosticId}}"}], "AnalysisId":"{{AnalysisId}}"}""";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(validRequestJson));
        request.InputStream.Returns(stream);
        request.ContentEncoding.Returns(Encoding.UTF8);

        var result = await AnalysisRequestHandler.ParseAnalysisRequestBodyAsync<AnalysisRequest>(context.Request);

        result.Should().NotBeNull();
        result!.FileUris.Should().HaveCount(1);
        result.FileUris[0].Should().Be(FileUri);
        result.ActiveRules.Should().HaveCount(1);
        result.ActiveRules[0].RuleId.Should().Be(DiagnosticId);
        result.AnalysisId.Should().Be(AnalysisId);
    }

    [TestMethod]
    public async Task ParseCancellationRequestBody_DeserializationFails_ReturnsNull()
    {
        var invalidBodyContent = "{}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidBodyContent));
        request.InputStream.Returns(stream);
        request.ContentEncoding.Returns(Encoding.UTF8);

        var result = await testSubject.ParseCancellationRequestBodyAsync(context.Request);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task ParseCancellationRequestBody_RequestBodyValid_ReturnsExpectedModel()
    {
        var validRequestJson = $$"""{"AnalysisId":"{{AnalysisId}}"}""";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(validRequestJson));
        request.InputStream.Returns(stream);
        request.ContentEncoding.Returns(Encoding.UTF8);

        var result = await testSubject.ParseCancellationRequestBodyAsync(context.Request);

        result.Should().NotBeNull();
        result!.AnalysisId.Should().Be(AnalysisId);
    }

    private void MockValidRequest()
    {
        request.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, DefaultPort));
        request.Headers.Returns(new WebHeaderCollection { [AuthTokenHeader] = ValidToken });
        request.HttpMethod.Returns(HttpMethodPost);
        request.Url.Returns(new Uri(AnalyzeUrl));
        request.ContentLength64.Returns(MaxRequestBodyBytes);
    }

    private void MockConfigurationProvider()
    {
        var configuration = Substitute.For<IHttpServerConfiguration>();
        configurationProvider = Substitute.For<IHttpServerConfigurationProvider>();
        configurationProvider.CurrentConfiguration.Returns(configuration);
        configuration.Token.Returns(ValidToken.ToSecureString());
    }
}

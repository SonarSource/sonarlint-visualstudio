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
using System.Net.Http;
using System.Security;
using System.Text;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Http.Helper;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Http;

[TestClass]
public class RoslynAnalysisHttpServerTest
{
    private static readonly HttpServerStarter ServerStarter = new();
    private static readonly HttpRequester HttpRequester = new();
    private const string CsharpFileName = "C:\\MyFile.cs";

    [ClassInitialize]
    public static void ClassInitialize(TestContext context) => ServerStarter.StartListeningOnBackgroundThread();

    [ClassCleanup]
    public static void ClassCleanup()
    {
        ServerStarter.Dispose();
        HttpRequester.Dispose();
    }

    [TestMethod]
    public async Task StartListenAsync_CallsMultipleTimes_DoesNotStartIfAlreadyStarted()
    {
        var port = ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port;
        await VerifyServerReachable(CreateClientRequestConfig());

        await ServerStarter.RoslynAnalysisHttpServer.StartListenAsync();
        await ServerStarter.RoslynAnalysisHttpServer.StartListenAsync();

        ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port.Should().Be(port);
        ServerStarter.MockedLogger.Received(1).LogVerbose(Resources.HttpServerStarted);
        await VerifyServerReachable(CreateClientRequestConfig());
    }

    [TestMethod]
    public async Task StartListenAsync_StartsAfterDisposed_DoesNotStart()
    {
        using var serverStarter = new HttpServerStarter();
        serverStarter.StartListeningOnBackgroundThread();

        serverStarter.RoslynAnalysisHttpServer.Dispose();
        await serverStarter.RoslynAnalysisHttpServer.StartListenAsync();

        await VerifyServerNotReachable<TaskCanceledException, AnalysisRequest>(CreateClientRequestConfig(serverStarter)); // the timeout of the request should be reached
        serverStarter.MockedLogger.Received(1).LogVerbose(Resources.HttpServerStarted);
        serverStarter.MockedLogger.Received(1).LogVerbose(Resources.HttpServerDisposed);
    }

    [TestMethod]
    public async Task StartListenAsync_PortAlreadyInUse_TriesAgain()
    {
        using var serverStarter2 = new HttpServerStarter(useMockedServerConfiguration: true);
        var busyPort = ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port;
        MockServerConfiguration(serverStarter2.HttpServerConfigurationProvider, busyPort);

        serverStarter2.StartListeningOnBackgroundThread();

        serverStarter2.MockedLogger.Received(1).LogVerbose(Resources.HttpServerAttemptFailed, 1, busyPort, Arg.Any<string>());
        await VerifyServerReachable(CreateClientRequestConfig(serverStarter2));
    }

    [TestMethod]
    public void StartListenAsync_PortAlreadyInUse_TriesMaxAttempts()
    {
        using var serverStarter2 = new HttpServerStarter(useMockedServerConfiguration: true);
        var busyPort = ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port;
        MockServerConfiguration(serverStarter2.HttpServerConfigurationProvider, busyPort);
        var maxAttempts = serverStarter2.ServerSettings.MaxStartAttempts;

        serverStarter2.StartListeningOnBackgroundThread();

        serverStarter2.MockedLogger.Received(maxAttempts)
            .LogVerbose(Resources.HttpServerAttemptFailed, Arg.Is<int>(x => x >= 1 && x <= maxAttempts), busyPort, Arg.Is<string>(x => x.Contains("Failed to listen on prefix")));
        serverStarter2.MockedLogger.Received(1).LogVerbose(Resources.HttpServerFailedToStartAttempts, maxAttempts);
    }

    [TestMethod]
    public async Task StartListenAsync_AnalysisRequestTakesLongerThanTimeout_ClosesRequestAfterTimeout()
    {
        var millisecondTimeout = 5;
        using var serverStarter2 = new HttpServerStarter(useMockedServerSettings: true);
        MockServerSettings(serverStarter2.ServerSettings, requestTimeout: millisecondTimeout);
        SimulateAnalysisRunsOutOfTime(serverStarter2.MockedRoslynAnalysisService);
        serverStarter2.StartListeningOnBackgroundThread();

        var response = await HttpRequester.SendRequest(CreateClientRequestConfig(serverStarter2));

        response.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
        serverStarter2.MockedLogger.Received(1).LogVerbose(Arg.Any<MessageLevelContext>(), Resources.HttpRequestTimedOut, Arg.Any<int>());
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Cancel_ReturnsExpectedResult(bool isCanceled)
    {
        var analysisId = Guid.NewGuid();
        ServerStarter.MockedRoslynAnalysisService.Cancel(Arg.Is<AnalysisCancellationRequest>(x => x.AnalysisId == analysisId)).Returns(isCanceled);
        ServerStarter.StartListeningOnBackgroundThread();

        var response = await HttpRequester.SendRequest(CreateCancellationRequestConfig(ServerStarter, analysisId));

        response.StatusCode.Should().Be(isCanceled ? HttpStatusCode.OK : HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task StartListenAsync_ExceededBodyLength_Fails()
    {
        var bodyLength = 1;
        var largeBody = "{}";
        using var serverStarter2 = new HttpServerStarter(useMockedServerSettings: true);
        MockServerSettings(serverStarter2.ServerSettings, maxBodyLength: bodyLength);
        serverStarter2.StartListeningOnBackgroundThread();

        var response = await HttpRequester.SendRequest(serverStarter2.HttpServerConfigurationProvider.CurrentConfiguration.Token.ToUnsecureString(),
            GetRequestUrl(serverStarter2.HttpServerConfigurationProvider.CurrentConfiguration.Port),
            largeBody);

        serverStarter2.MockedLogger.Received(1).LogVerbose(Resources.BodyLengthExceeded, (long)Encoding.UTF8.GetByteCount(largeBody), (long)bodyLength);
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [TestMethod]
    public async Task StartListenAsync_ProvidesWrongBody_Fails()
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

        var response = await HttpRequester.SendRequest(ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Token.ToUnsecureString(),
            GetRequestUrl(ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port), unexpectedBodyContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task StartListenAsync_ValidRequest_ReturnsEmptyDiagnostics()
    {
        var response = await HttpRequester.SendRequest(CreateClientRequestConfig());

        await VerifyRequestSucceeded(response);
    }

    [TestMethod]
    public async Task StartListenAsync_InvalidToken_ReturnsUnauthorized()
    {
        var response = await HttpRequester.SendRequest(CreateClientRequestConfig(token: "wrongToken".ToSecureString()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task StartListenAsync_InvalidRequestUri_ReturnsNotFound()
    {
        var invalidRequestUrl = GetRequestUrl(ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port, "wrongPath");

        var response = await HttpRequester.SendRequest(CreateClientRequestConfig(requestUri: invalidRequestUrl));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task StartListenAsync_NoFilesToAnalyze_ReturnsBadRequest()
    {
        var response = await HttpRequester.SendRequest(CreateClientRequestConfig(fileNames: []));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task StartListenAsync_AnalysisThrowsException_ReturnsInternalServerError()
    {
        var exceptionMessage = "Simulated exception";
        using var serverStarter2 = new HttpServerStarter();
        serverStarter2.MockedRoslynAnalysisService
            .When(x => x.AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException(exceptionMessage));
        serverStarter2.StartListeningOnBackgroundThread();

        var response = await HttpRequester.SendRequest(CreateClientRequestConfig(serverStarter2));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        serverStarter2.MockedLogger.Received(1).LogVerbose(Arg.Any<MessageLevelContext>(), Resources.HttpRequestFailed, Arg.Is<string>(x => x.Contains(exceptionMessage)));
    }

    [TestMethod]
    public async Task Dispose_StopsServer()
    {
        var testServerStarter = new HttpServerStarter();
        testServerStarter.StartListeningOnBackgroundThread();

        testServerStarter.RoslynAnalysisHttpServer.Dispose();

        await VerifyServerNotReachable<TaskCanceledException, AnalysisRequest>(CreateClientRequestConfig(testServerStarter)); // the timeout of the request should be reached
        testServerStarter.MockedLogger.Received(1).LogVerbose(Resources.HttpServerDisposed);
    }

    [TestMethod]
    public void Dispose_CallsMultipleTimes_DisposesOnce()
    {
        var testServerStarter = new HttpServerStarter();
        testServerStarter.StartListeningOnBackgroundThread();

        testServerStarter.RoslynAnalysisHttpServer.Dispose();
        testServerStarter.RoslynAnalysisHttpServer.Dispose();
        testServerStarter.RoslynAnalysisHttpServer.Dispose();

        testServerStarter.MockedLogger.Received(1).LogVerbose(Resources.HttpServerDisposed);
    }

    private static AnalysisRequestConfig<AnalysisRequest> CreateClientRequestConfig(HttpServerStarter httpServerStarter, Guid? analysisId = null) =>
        CreateClientRequestConfig(
            [CsharpFileName],
            GetRequestUrl(httpServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port),
            httpServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Token,
            analysisId);

    private static AnalysisRequestConfig<AnalysisCancellationRequest> CreateCancellationRequestConfig(HttpServerStarter httpServerStarter, Guid analysisId) =>
        new(httpServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Token,
            GetRequestUrl(httpServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port, requestPath: "cancel"),
            new AnalysisCancellationRequest { AnalysisId = analysisId });

    private static AnalysisRequestConfig<AnalysisRequest> CreateClientRequestConfig(SecureString? token = null, string? requestUri = null) =>
        CreateClientRequestConfig([CsharpFileName], requestUri, token);

    private static AnalysisRequestConfig<AnalysisRequest> CreateClientRequestConfig(
        string[] fileNames,
        string? requestUri = null,
        SecureString? token = null,
        Guid? analysisId = null)
    {
        token ??= ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Token;
        requestUri ??= GetRequestUrl(ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port);
        var fileUris = fileNames.Select(x => new FileUri(x));
        var analysisRequest = new AnalysisRequest { FileNames = [.. fileUris], ActiveRules = [new ActiveRuleDto("id", [])], AnalysisId = analysisId ?? Guid.NewGuid() };
        return new AnalysisRequestConfig<AnalysisRequest>(token, requestUri, analysisRequest);
    }

    private static string GetRequestUrl(int port, string requestPath = "analyze") => $"http://127.0.0.1:{port}/{requestPath}";

    private static async Task VerifyServerReachable<T>(AnalysisRequestConfig<T> requestConfig)
    {
        var response = await HttpRequester.SendRequest(requestConfig);
        await VerifyRequestSucceeded(response);
    }

    private static async Task VerifyRequestSucceeded(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var analysisResponse = await GetAnalysisResponse(response);
        analysisResponse.Should().NotBeNull();
        analysisResponse!.RoslynIssues.Should().BeEmpty();
    }

    private static async Task VerifyServerNotReachable<TException, TRequest>(AnalysisRequestConfig<TRequest> analysisRequestConfig) where TException : Exception
    {
        var act = async () => await HttpRequester.SendRequest(analysisRequestConfig);
        await act.Should().ThrowAsync<TException>();
    }

    private static async Task<AnalysisResponse?> GetAnalysisResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var analysisResponse = JsonConvert.DeserializeObject<AnalysisResponse>(content);
        return analysisResponse;
    }

    private static void MockServerSettings(
        IHttpServerSettings serverConfiguration,
        int requestTimeout = 50,
        int maxBodyLength = 1024)
    {
        serverConfiguration.MaxStartAttempts.Returns(3);
        serverConfiguration.RequestMillisecondsTimeout.Returns(requestTimeout);
        serverConfiguration.MaxRequestBodyBytes.Returns(maxBodyLength);
    }

    private static void MockServerConfiguration(IHttpServerConfigurationProvider serverConfigurationProvider, int port) => serverConfigurationProvider.CurrentConfiguration.Port.Returns(port);

    private static void SimulateAnalysisRunsOutOfTime(IRoslynAnalysisService roslynAnalysisService) =>
        roslynAnalysisService
            .When(x => x.AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>()))
            .Throw(new OperationCanceledException());

    private static void SimulateAnalysisWithCallback(IRoslynAnalysisService roslynAnalysisService, Action callback) =>
        roslynAnalysisService.AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callback();
                return Task.FromResult(Enumerable.Empty<RoslynIssue>());
            });
}

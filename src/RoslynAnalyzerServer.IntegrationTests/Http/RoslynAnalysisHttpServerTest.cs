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

        await VerifyServerNotReachable<TaskCanceledException>(CreateClientRequestConfig(serverStarter)); // the timeout of the request should be reached
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
        SimulateLongAnalysis(serverStarter2.MockedRoslynAnalysisService, millisecondTimeout * 2);
        serverStarter2.StartListeningOnBackgroundThread();

        var response = await HttpRequester.SendRequest(CreateClientRequestConfig(serverStarter2));

        serverStarter2.MockedLogger.Received(1).LogVerbose(Resources.HttpRequestTimedOut, Arg.Any<int>());
        response.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
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

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task StartListenAsync_NoFilesToAnalyze_ReturnsBadRequest()
    {
        var response = await HttpRequester.SendRequest(CreateClientRequestConfig(fileNames: []));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Dispose_StopsServer()
    {
        var testServerStarter = new HttpServerStarter();
        testServerStarter.StartListeningOnBackgroundThread();

        testServerStarter.RoslynAnalysisHttpServer.Dispose();

        await VerifyServerNotReachable<TaskCanceledException>(CreateClientRequestConfig(testServerStarter)); // the timeout of the request should be reached
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

    private static AnalysisRequestConfig CreateClientRequestConfig(HttpServerStarter httpServerStarter) =>
        CreateClientRequestConfig([CsharpFileName], GetRequestUrl(httpServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port),
            httpServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Token);

    private static AnalysisRequestConfig CreateClientRequestConfig(SecureString? token = null, string? requestUri = null) => CreateClientRequestConfig([CsharpFileName], requestUri, token);

    private static AnalysisRequestConfig CreateClientRequestConfig(string[] fileNames, string? requestUri = null, SecureString? token = null)
    {
        token ??= ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Token;
        requestUri ??= GetRequestUrl(ServerStarter.HttpServerConfigurationProvider.CurrentConfiguration.Port);
        return new AnalysisRequestConfig(token, requestUri, fileNames);
    }

    private static string GetRequestUrl(int port, string requestPath = "analyze") => $"http://127.0.0.1:{port}/{requestPath}";

    private static async Task VerifyServerReachable(AnalysisRequestConfig requestConfig)
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

    private static async Task VerifyServerNotReachable<T>(AnalysisRequestConfig analysisRequestConfig) where T : Exception
    {
        var act = async () => await HttpRequester.SendRequest(analysisRequestConfig);
        await act.Should().ThrowAsync<T>();
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
        int maxBodyLength = 100)
    {
        serverConfiguration.MaxStartAttempts.Returns(3);
        serverConfiguration.RequestMillisecondsTimeout.Returns(requestTimeout);
        serverConfiguration.MaxRequestBodyBytes.Returns(maxBodyLength);
    }

    private static void MockServerConfiguration(IHttpServerConfigurationProvider serverConfigurationProvider, int port) => serverConfigurationProvider.CurrentConfiguration.Port.Returns(port);

    private static void SimulateLongAnalysis(IRoslynAnalysisService roslynAnalysisService, int milliseconds) =>
        roslynAnalysisService
            .When(x => x.AnalyzeAsync(Arg.Any<List<FileUri>>(), Arg.Any<List<ActiveRuleDto>>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>()))
            .Do(_ => Task.Delay(milliseconds).GetAwaiter().GetResult());
}

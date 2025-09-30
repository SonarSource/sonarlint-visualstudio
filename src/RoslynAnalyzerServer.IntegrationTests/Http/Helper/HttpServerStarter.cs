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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Http.Helper;

internal sealed class HttpServerStarter : IDisposable
{
    private const int WaitForServerMsTimeout = 1000;
    internal readonly IHttpServerSettings ServerSettings;
    internal IHttpServerConfigurationProvider HttpServerConfigurationProvider { get; }
    internal RoslynAnalysisHttpServer RoslynAnalysisHttpServer { get; }
    internal ILogger MockedLogger { get; } = CreateMockedLogger();
    internal IRoslynAnalysisService MockedRoslynAnalysisService { get; } = CreateMockedAnalysisEngine();

    public HttpServerStarter(bool useMockedServerSettings = false, int maxConcurrentRequests = 5, bool useMockedServerConfiguration = false)
    {
        ServerSettings = useMockedServerSettings ? CreateMockedServerConfiguration(maxConcurrentRequests) : new HttpServerSettings();
        var httpServerConfigurationProvider = new HttpServerConfigurationProvider();
        HttpServerConfigurationProvider = useMockedServerConfiguration ? CreateMockedServerConfigurationProvider() : httpServerConfigurationProvider;
        var httpServerConfigurationFactory = useMockedServerConfiguration ? CreateHttpServerConfigurationFactory() : httpServerConfigurationProvider;
        var analysisRequestHandler = new AnalysisRequestHandler(MockedLogger, ServerSettings, HttpServerConfigurationProvider);
        RoslynAnalysisHttpServer = new RoslynAnalysisHttpServer(MockedLogger, ServerSettings, analysisRequestHandler, new HttpRequestHandler(),
            new HttpListenerFactory(), httpServerConfigurationFactory, MockedRoslynAnalysisService);
    }

    public void StartListeningOnBackgroundThread()
    {
        var serverListeningEvent = new ManualResetEvent(false);
        MockedLogger.When(x => x.LogVerbose(Arg.Is<string>(x => x == Resources.HttpServerStarted || x == Resources.HttpServerNotStarted))).Do(_ =>
        {
            serverListeningEvent.Set();
        });
        MockedLogger.When(x => x.LogVerbose(Resources.HttpServerFailedToStartAttempts, ServerSettings.MaxStartAttempts)).Do(_ =>
        {
            serverListeningEvent.Set();
        });
        var thread = new Thread(() => StartRoslynAnalysisHttpServer(RoslynAnalysisHttpServer).ConfigureAwait(false)) { IsBackground = true };
        thread.Start();
        serverListeningEvent.WaitOne(WaitForServerMsTimeout);
    }

    public void Dispose() => RoslynAnalysisHttpServer.Dispose();

    private static async Task StartRoslynAnalysisHttpServer(RoslynAnalysisHttpServer httpServer) => await httpServer.StartListenAsync();

    private static ILogger CreateMockedLogger()
    {
        var logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string>()).Returns(logger);
        return logger;
    }

    private static IRoslynAnalysisService CreateMockedAnalysisEngine()
    {
        var analysisEngine = Substitute.For<IRoslynAnalysisService>();
        analysisEngine.AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Enumerable.Empty<RoslynIssue>()));
        return analysisEngine;
    }

    private static IHttpServerSettings CreateMockedServerConfiguration(int maxConcurrentRequests)
    {
        var config = Substitute.For<IHttpServerSettings>();
        config.MaxConcurrentRequests.Returns(maxConcurrentRequests);
        config.MaxStartAttempts.Returns(3);
        config.RequestMillisecondsTimeout.Returns(3000);
        config.MaxRequestBodyBytes.Returns(1024);
        return config;
    }

    private static IHttpServerConfigurationProvider CreateMockedServerConfigurationProvider()
    {
        var config = Substitute.For<IHttpServerConfigurationProvider>();
        config.CurrentConfiguration.Returns(Substitute.For<IHttpServerConfiguration>());
        return config;
    }

    private IHttpServerConfigurationFactory CreateHttpServerConfigurationFactory()
    {
        var config = Substitute.For<IHttpServerConfigurationFactory>();
        var configuration = Substitute.For<IHttpServerConfiguration>();
        config.SetNewConfiguration().Returns(configuration);
        HttpServerConfigurationProvider.CurrentConfiguration.Returns(configuration);

        return config;
    }
}

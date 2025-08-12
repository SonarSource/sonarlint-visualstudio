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

using System.ComponentModel.Composition;
using System.Net;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

[Export(typeof(IRoslynAnalysisHttpServer))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class RoslynAnalysisHttpServer(
    ILogger logger,
    IHttpServerConfiguration configuration,
    IAnalysisRequestHandler analysisRequestHandler,
    IHttpListenerFactory httpListenerFactory,
    IAnalysisEngine analysisEngine) : IRoslynAnalysisHttpServer
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly ILogger logger = logger.ForContext(Resources.HttpServerLogContext).ForContext(nameof(RoslynAnalysisHttpServer));
    private readonly SemaphoreSlim requestSemaphore = new(configuration.MaxConcurrentRequests);
    private HttpListener? httpListener;
    private bool isDisposed;

    public async Task StartListenAsync()
    {
        try
        {
            if (httpListener is { IsListening: true } || isDisposed)
            {
                logger.LogVerbose(Resources.HttpServerNotStarted);
                return;
            }

            logger.LogVerbose(Resources.HttpServerStarting);
            for (var attempt = 1; attempt <= configuration.MaxStartAttempts; attempt++)
            {
                httpListener = httpListenerFactory.Create(configuration.Port);
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                await StartListenAsync(attempt);
            }
            logger.LogVerbose(Resources.HttpServerFailedToStartAttempts, configuration.MaxStartAttempts);
        }
        catch (Exception ex)
        {
            logger.LogVerbose(Resources.HttpServerFailure, ex);
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        httpListener?.Close();
        requestSemaphore.Dispose();
        isDisposed = true;
        logger.LogVerbose(Resources.HttpServerDisposed);
    }

    private async Task StartListenAsync(int attempt)
    {
        try
        {
            httpListener!.Start();
            logger.LogVerbose(Resources.HttpServerStarted);
            await WaitForRequests(httpListener, cancellationTokenSource.Token);
        }
        catch (HttpListenerException ex)
        {
            logger.LogVerbose(Resources.HttpServerAttemptFailed, attempt, configuration.Port, ex.Message);
            configuration.GenerateNewPort();
        }
    }

    private async Task WaitForRequests(HttpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IHttpListenerContext? context = null;
            try
            {
                var getRequestTask = listener.GetContextAsync();
                var completedTask = await Task.WhenAny(getRequestTask, Task.Delay(-1, cancellationToken));
                if (completedTask != getRequestTask)
                {
                    break;
                }
                context = new HttpListenerContextAdapter(await getRequestTask);
                if (!await requestSemaphore.WaitAsync(0, cancellationToken))
                {
                    analysisRequestHandler.CloseRequest(context, HttpStatusCode.ServiceUnavailable);
                    logger.LogVerbose(Resources.ConcurrentRequestsExceeded, configuration.MaxConcurrentRequests);
                    continue;
                }

                _ = HandleRequestWithTimeout(context, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.WriteLine(Resources.HttpServerAttemptFailed, ex);
                if (context != null)
                {
                    analysisRequestHandler.CloseRequest(context, HttpStatusCode.InternalServerError);
                }
            }
        }
    }

    private async Task HandleRequestWithTimeout(IHttpListenerContext context, CancellationToken serverCancellationToken)
    {
        using var requestCancellationToken = new CancellationTokenSource(configuration.RequestMillisecondsTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken, requestCancellationToken.Token);
        try
        {
            await Task.Run(() => HandleRequest(context, linkedCts.Token), linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogVerbose(Resources.HttpRequestTimedOut, configuration.RequestMillisecondsTimeout);
            analysisRequestHandler.CloseRequest(context, HttpStatusCode.RequestTimeout);
        }
        finally
        {
            requestSemaphore.Release();
        }
    }

    private async Task HandleRequest(IHttpListenerContext context, CancellationToken cancellationToken)
    {
        if (!analysisRequestHandler.IsValidRequest(context) || await analysisRequestHandler.GetAnalysisRequest(context) is not { } analysisRequest)
        {
            return;
        }
        var diagnostics = await analysisEngine.AnalyzeAsync(analysisRequest.FileNames, analysisRequest.ActiveRules, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await analysisRequestHandler.SendResponse(context, diagnostics);
    }
}

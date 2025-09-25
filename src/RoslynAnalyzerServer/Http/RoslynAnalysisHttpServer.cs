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
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

[Export(typeof(IRoslynAnalysisHttpServer))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class RoslynAnalysisHttpServer(
    ILogger logger,
    IHttpServerSettings settings,
    IAnalysisRequestHandler analysisRequestHandler,
    IHttpRequestHandler httpRequestHandler,
    IHttpListenerFactory httpListenerFactory,
    IHttpServerConfigurationFactory httpServerConfigurationFactory,
    IRoslynAnalysisService roslynAnalysisService) : IRoslynAnalysisHttpServer
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly ILogger logger = logger.ForContext(Resources.HttpServerLogContext).ForContext(nameof(RoslynAnalysisHttpServer));
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
            for (var attempt = 1; attempt <= settings.MaxStartAttempts; attempt++)
            {
                var currentConfiguration = httpServerConfigurationFactory.SetNewConfiguration();
                httpListener = httpListenerFactory.Create(currentConfiguration.Port);
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                await StartListenAsync(attempt, currentConfiguration.Port);
            }
            logger.LogVerbose(Resources.HttpServerFailedToStartAttempts, settings.MaxStartAttempts);
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
        isDisposed = true;
        logger.LogVerbose(Resources.HttpServerDisposed);
    }

    private async Task StartListenAsync(int attempt, int port)
    {
        try
        {
            httpListener!.Start();
            logger.LogVerbose(Resources.HttpServerStarted);
            await WaitForRequestsAsync(httpListener, cancellationTokenSource.Token);
        }
        catch (HttpListenerException ex)
        {
            logger.LogVerbose(Resources.HttpServerAttemptFailed, attempt, port, ex.Message);
        }
    }

    private async Task WaitForRequestsAsync(HttpListener listener, CancellationToken cancellationToken)
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
                _ = HandleRequestWithTimeoutAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.WriteLine(Resources.HttpServerAttemptFailed, ex);
                if (context != null)
                {
                    httpRequestHandler.CloseRequest(context, HttpStatusCode.InternalServerError);
                }
            }
        }
    }

    private async Task HandleRequestWithTimeoutAsync(IHttpListenerContext context, CancellationToken serverCancellationToken)
    {
        using var requestCancellationToken = new CancellationTokenSource(settings.RequestMillisecondsTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken, requestCancellationToken.Token);
        try
        {
            await Task.Run(() => HandleRequestAsync(context, linkedCts.Token), linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogVerbose(Resources.HttpRequestTimedOut, settings.RequestMillisecondsTimeout);
            httpRequestHandler.CloseRequest(context, HttpStatusCode.RequestTimeout);
        }
        catch (Exception exception)
        {
            logger.LogVerbose(Resources.HttpRequestFailed, exception.Message + exception.StackTrace);
            httpRequestHandler.CloseRequest(context, HttpStatusCode.InternalServerError);
        }
    }

    private async Task HandleRequestAsync(IHttpListenerContext context, CancellationToken cancellationToken)
    {
        if (!analysisRequestHandler.ValidateRequest(context, out var validationStatusCode, out var requestType))
        {
            httpRequestHandler.CloseRequest(context, validationStatusCode);
            return;
        }

        if (requestType == RequestType.Analyze && await analysisRequestHandler.ParseAnalysisRequestBodyAsync(context) is { } analysisRequest)
        {
            var issues = await roslynAnalysisService.AnalyzeAsync(analysisRequest, cancellationToken);
            await httpRequestHandler.SendResponseAsync(context, analysisRequestHandler.SerializeAnalysisRequestResponse(issues.ToList()));
        }
        else if (requestType == RequestType.Cancel && await analysisRequestHandler.ParseCancellationRequestBodyAsync(context) is { } cancellationRequest)
        {
            var status = roslynAnalysisService.Cancel(cancellationRequest);
            httpRequestHandler.CloseRequest(context, status ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        }
        else
        {
            httpRequestHandler.CloseRequest(context, HttpStatusCode.BadRequest);
        }
    }
}

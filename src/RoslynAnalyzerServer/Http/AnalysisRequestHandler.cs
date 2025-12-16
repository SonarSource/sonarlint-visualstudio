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

using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

public enum RequestType
{
    Unknown,
    Analyze,
    Cancel
}

public interface IAnalysisRequestHandler
{
    Task<AnalysisRequest?> ParseAnalysisRequestBodyAsync(IHttpListenerRequest request);

    Task<AnalysisCancellationRequest?> ParseCancellationRequestBodyAsync(IHttpListenerRequest request);

    string SerializeAnalysisRequestResponse(List<RoslynIssue> diagnostics);

    bool ValidateRequest(IHttpListenerRequest request, out HttpStatusCode errorCode, out RequestType requestType);
}

[Export(typeof(IAnalysisRequestHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class AnalysisRequestHandler(ILogger logger, IHttpServerSettings serverSettings, IHttpServerConfigurationProvider serverConfigurationProvider) : IAnalysisRequestHandler
{
    private const string XAuthTokenHeader = "X-Auth-Token";
    private const string AnalyzeRequestUrl = "/analyze";
    private const string CancelAnalysisRequestUrl = "/cancel";
    private readonly ILogger logger = logger.ForContext(Resources.HttpServerLogContext).ForContext(nameof(AnalysisRequestHandler));

    public string SerializeAnalysisRequestResponse(List<RoslynIssue> diagnostics)
    {
        var responseObj = new AnalysisResponse { RoslynIssues = diagnostics };
        var responseString = JsonConvert.SerializeObject(responseObj);
        return responseString;
    }

    public bool ValidateRequest(IHttpListenerRequest request, out HttpStatusCode errorCode, out RequestType requestType)
    {
        requestType = RequestType.Unknown;
        errorCode = default;
        return VerifyLocalRequest(request, out errorCode)
               && VerifyToken(request, out errorCode)
               && VerifyMethod(request, out errorCode, out requestType)
               && VerifyContentLength(request, out errorCode);
    }

    public async Task<AnalysisRequest?> ParseAnalysisRequestBodyAsync(IHttpListenerRequest request)
    {
        var analysisRequestBodyAsync = await ParseAnalysisRequestBodyAsync<AnalysisRequest>(request);
        return analysisRequestBodyAsync is { FileUris.Count: > 0, ActiveRules.Count: > 0 } ? analysisRequestBodyAsync : null;
    }

    public Task<AnalysisCancellationRequest?> ParseCancellationRequestBodyAsync(IHttpListenerRequest request) =>
        ParseAnalysisRequestBodyAsync<AnalysisCancellationRequest>(request);

    internal static async Task<T?> ParseAnalysisRequestBodyAsync<T>(IHttpListenerRequest request) where T : class
    {
        var body = await ReadBodyAsync(request);
        return GetAnalysisRequestFromBody<T>(body);
    }

    private static async Task<string> ReadBodyAsync(IHttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        return await reader.ReadToEndAsync();
    }

    private static T? GetAnalysisRequestFromBody<T>(string body) where T : class
    {
        T? requestDto;
        try
        {
            requestDto = JsonConvert.DeserializeObject<T>(body);
        }
        catch (Exception)
        {
            return null;
        }
        return requestDto;
    }

    private static bool VerifyLocalRequest(IHttpListenerRequest request, out HttpStatusCode errorCode)
    {
        if (!IsLocalRequest(request))
        {
            errorCode = HttpStatusCode.Forbidden;
            return false;
        }
        errorCode = default;
        return true;
    }

    private bool VerifyToken(IHttpListenerRequest request, out HttpStatusCode errorCode)
    {
        var token = request.Headers[XAuthTokenHeader];
        if (token != serverConfigurationProvider.CurrentConfiguration.Token.ToUnsecureString())
        {
            errorCode = HttpStatusCode.Unauthorized;
            return false;
        }
        errorCode = default;
        return true;
    }

    private static bool VerifyMethod(IHttpListenerRequest request, out HttpStatusCode errorCode, out RequestType requestType)
    {
        errorCode = default;
        if (request.HttpMethod == HttpMethod.Post.Method && request.Url.AbsolutePath == AnalyzeRequestUrl)
        {
            requestType = RequestType.Analyze;
            return true;
        }

        if (request.HttpMethod == HttpMethod.Post.Method && request.Url.AbsolutePath == CancelAnalysisRequestUrl)
        {
            requestType = RequestType.Cancel;
            return true;
        }

        requestType = RequestType.Unknown;
        errorCode = HttpStatusCode.BadRequest;
        return false;
    }

    private bool VerifyContentLength(IHttpListenerRequest request, out HttpStatusCode errorCode)
    {
        if (request.ContentLength64 > serverSettings.MaxRequestBodyBytes)
        {
            logger.LogVerbose(Resources.BodyLengthExceeded, request.ContentLength64, serverSettings.MaxRequestBodyBytes);
            errorCode = HttpStatusCode.RequestEntityTooLarge;
            return false;
        }
        errorCode = default;
        return true;
    }

    private static bool IsLocalRequest(IHttpListenerRequest request)
    {
        var remoteAddress = request.RemoteEndPoint?.Address;
        return remoteAddress != null
               && (remoteAddress.Equals(IPAddress.Loopback)
                   || remoteAddress.Equals(IPAddress.IPv6Loopback));
    }
}

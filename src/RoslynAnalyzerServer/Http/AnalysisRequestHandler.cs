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
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

public interface IAnalysisRequestHandler
{
    Task<AnalysisRequest?> ParseAnalysisRequestBodyAsync(IHttpListenerContext context);

    string ParseAnalysisRequestResponse(List<RoslynIssue> diagnostics);

    HttpStatusCode ValidateRequest(IHttpListenerContext context);
}

[Export(typeof(IAnalysisRequestHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class AnalysisRequestHandler(ILogger logger, IHttpServerSettings serverSettings, IHttpServerConfigurationProvider serverConfigurationProvider) : IAnalysisRequestHandler
{
    private const string XAuthTokenHeader = "X-Auth-Token";
    private const string AnalyzeRequestUrl = "/analyze";
    private readonly ILogger logger = logger.ForContext(Resources.HttpServerLogContext).ForContext(nameof(AnalysisRequestHandler));

    public HttpStatusCode ValidateRequest(IHttpListenerContext context)
    {
        if (VerifyLocalRequest(context) is var localRequestStatusCode && localRequestStatusCode != HttpStatusCode.OK)
        {
            return localRequestStatusCode;
        }
        if (VerifyToken(context) is var tokenStatusCode && tokenStatusCode != HttpStatusCode.OK)
        {
            return tokenStatusCode;
        }
        if (VerifyMethod(context) is var methodStatusCode && methodStatusCode != HttpStatusCode.OK)
        {
            return methodStatusCode;
        }
        if (VerifyContentLength(context) is var contentLengthStatusCode && contentLengthStatusCode != HttpStatusCode.OK)
        {
            return contentLengthStatusCode;
        }
        return HttpStatusCode.OK;
    }

    public async Task<AnalysisRequest?> ParseAnalysisRequestBodyAsync(IHttpListenerContext context)
    {
        var body = await ReadBodyAsync(context);
        var requestDto = GetAnalysisRequestFromBody(body);
        if (requestDto != null && requestDto.FileNames.Count != 0)
        {
            return requestDto;
        }

        return null;
    }

    public string ParseAnalysisRequestResponse(List<RoslynIssue> diagnostics)
    {
        var responseObj = new AnalysisResponse { RoslynIssues = diagnostics };
        var responseString = JsonConvert.SerializeObject(responseObj);
        return responseString;
    }

    private static async Task<string> ReadBodyAsync(IHttpListenerContext context)
    {
        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        return await reader.ReadToEndAsync();
    }

    private static AnalysisRequest? GetAnalysisRequestFromBody(string body)
    {
        AnalysisRequest? requestDto;
        try
        {
            requestDto = JsonConvert.DeserializeObject<AnalysisRequest>(body);
        }
        catch (Exception)
        {
            return null;
        }
        return requestDto;
    }

    private static HttpStatusCode VerifyLocalRequest(IHttpListenerContext context) => IsLocalRequest(context.Request) ? HttpStatusCode.OK : HttpStatusCode.Forbidden;

    private static bool IsLocalRequest(IHttpListenerRequest request)
    {
        var remote = request.RemoteEndPoint;
        return remote != null && (remote.Address.Equals(IPAddress.Loopback) || remote.Address.Equals(IPAddress.IPv6Loopback));
    }

    private HttpStatusCode VerifyToken(IHttpListenerContext context)
    {
        var token = context.Request.Headers[XAuthTokenHeader];

        return token == serverConfigurationProvider.CurrentConfiguration.Token.ToUnsecureString() ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;
    }

    private static HttpStatusCode VerifyMethod(IHttpListenerContext context)
    {
        if (context.Request.HttpMethod == HttpMethod.Post.Method && context.Request.Url.AbsolutePath == AnalyzeRequestUrl)
        {
            return HttpStatusCode.OK;
        }
        return HttpStatusCode.NotFound;
    }

    private HttpStatusCode VerifyContentLength(IHttpListenerContext context)
    {
        if (context.Request.ContentLength64 <= serverSettings.MaxRequestBodyBytes)
        {
            return HttpStatusCode.OK;
        }
        logger.LogVerbose(Resources.BodyLengthExceeded, context.Request.ContentLength64, serverSettings.MaxRequestBodyBytes);
        return HttpStatusCode.RequestEntityTooLarge;
    }
}

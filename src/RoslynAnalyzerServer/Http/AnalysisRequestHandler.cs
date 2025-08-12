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
using System.Text;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

public interface IAnalysisRequestHandler : IHttpRequestHandler
{
    Task<AnalysisRequest?> GetAnalysisRequest(IHttpListenerContext context);

    Task SendResponse(IHttpListenerContext context, List<DiagnosticDto> diagnostics);
}

public interface IHttpRequestHandler
{
    bool IsValidRequest(IHttpListenerContext context);

    void CloseRequest(IHttpListenerContext context, HttpStatusCode statusCode);
}

[Export(typeof(IAnalysisRequestHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class AnalysisRequestHandler(ILogger logger, IHttpServerConfiguration configuration) : IAnalysisRequestHandler
{
    private const string XAuthTokenHeader = "X-Auth-Token";
    private const string AnalyzeRequestUrl = "/analyze";
    private readonly ILogger logger = logger.ForContext(Resources.HttpServerLogContext).ForContext(nameof(AnalysisRequestHandler));

    public void CloseRequest(IHttpListenerContext context, HttpStatusCode statusCode)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.Close();
    }

    public async Task SendResponse(IHttpListenerContext context, List<DiagnosticDto> diagnostics)
    {
        var responseString = CreateResponse(diagnostics);
        await WriteResponse(responseString, context);
    }

    public bool IsValidRequest(IHttpListenerContext context) => VerifyLocalRequest(context) && VerifyToken(context) && VerifyMethod(context) && VerifyContentLength(context);

    public async Task<AnalysisRequest?> GetAnalysisRequest(IHttpListenerContext context)
    {
        var body = await ReadBody(context);
        var requestDto = GetAnalysisRequestFromBody(body);
        if (requestDto != null && requestDto.FileNames.Count != 0)
        {
            return requestDto;
        }

        CloseRequest(context, HttpStatusCode.BadRequest);
        return null;
    }

    private static async Task<string> ReadBody(IHttpListenerContext context)
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

    private static string CreateResponse(List<DiagnosticDto> diagnostics)
    {
        var responseObj = new AnalysisResponse { Diagnostics = diagnostics };
        var responseString = JsonConvert.SerializeObject(responseObj);
        return responseString;
    }

    private static async Task WriteResponse(string responseString, IHttpListenerContext context)
    {
        var buffer = Encoding.UTF8.GetBytes(responseString);
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }

    private bool VerifyLocalRequest(IHttpListenerContext context)
    {
        if (IsLocalRequest(context.Request))
        {
            return true;
        }
        CloseRequest(context, HttpStatusCode.Forbidden);
        return false;
    }

    private static bool IsLocalRequest(IHttpListenerRequest request)
    {
        var remote = request.RemoteEndPoint;
        return remote != null && (remote.Address.Equals(IPAddress.Loopback) || remote.Address.Equals(IPAddress.IPv6Loopback));
    }

    private bool VerifyToken(IHttpListenerContext context)
    {
        var token = context.Request.Headers[XAuthTokenHeader];
        if (token == configuration.Token.ToUnsecureString())
        {
            return true;
        }
        CloseRequest(context, HttpStatusCode.Unauthorized);
        return false;
    }

    private bool VerifyMethod(IHttpListenerContext context)
    {
        if (context.Request.HttpMethod == HttpMethod.Post.Method && context.Request.Url.AbsolutePath == AnalyzeRequestUrl)
        {
            return true;
        }
        CloseRequest(context, HttpStatusCode.NotFound);
        return false;
    }

    private bool VerifyContentLength(IHttpListenerContext context)
    {
        if (context.Request.ContentLength64 <= configuration.MaxRequestBodyBytes)
        {
            return true;
        }
        logger.LogVerbose(Resources.BodyLengthExceeded, context.Request.ContentLength64, configuration.MaxRequestBodyBytes);
        CloseRequest(context, HttpStatusCode.RequestEntityTooLarge);
        return false;
    }
}

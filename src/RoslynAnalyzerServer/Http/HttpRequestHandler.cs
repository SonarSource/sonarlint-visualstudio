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
using System.Text;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

public interface IHttpRequestHandler
{
    Task SendResponse(IHttpListenerContext context, string responseString);

    void CloseRequest(IHttpListenerContext context, HttpStatusCode statusCode);
}

[Export(typeof(IHttpRequestHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class HttpRequestHandler() : IHttpRequestHandler
{
    public void CloseRequest(IHttpListenerContext context, HttpStatusCode statusCode)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.Close();
    }

    public async Task SendResponse(IHttpListenerContext context, string responseString) => await WriteResponse(responseString, context, HttpStatusCode.OK);

    private static async Task WriteResponse(string responseString, IHttpListenerContext context, HttpStatusCode statusCode)
    {
        var buffer = Encoding.UTF8.GetBytes(responseString);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.StatusCode = (int)statusCode;
        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }
}

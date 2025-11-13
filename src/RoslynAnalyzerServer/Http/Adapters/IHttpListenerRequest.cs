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

using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;

public interface IHttpListenerRequest
{
    IPEndPoint? RemoteEndPoint { get; }
    NameValueCollection Headers { get; }
    string HttpMethod { get; }
    Uri Url { get; }
    long ContentLength64 { get; }
    Stream InputStream { get; }
    Encoding ContentEncoding { get; }
}

public class HttpListenerRequestAdapter(HttpListenerRequest request) : IHttpListenerRequest
{
    public IPEndPoint? RemoteEndPoint => request.RemoteEndPoint;
    public NameValueCollection Headers => request.Headers;
    public string HttpMethod => request.HttpMethod;
    public Uri Url => request.Url;
    public long ContentLength64 => request.ContentLength64;
    public Stream InputStream => request.InputStream;
    public Encoding ContentEncoding => request.ContentEncoding;
}

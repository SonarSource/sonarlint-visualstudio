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

using System.IO;
using System.Net;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Adapters;

public interface IHttpListenerResponse
{
    int StatusCode { get; set; }
    long ContentLength64 { get; set; }
    Stream OutputStream { get; }

    void Close();
}

public class HttpListenerResponseAdapter(HttpListenerResponse response) : IHttpListenerResponse
{
    public int StatusCode { get => response.StatusCode; set => response.StatusCode = value; }

    public void Close() => response.Close();

    public long ContentLength64 { get => response.ContentLength64; set => response.ContentLength64 = value; }
    public Stream OutputStream => response.OutputStream;
}

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
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

public interface IHttpServerConfiguration
{
    int Port { get; }
    SecureString Token { get; }
    int MaxStartAttempts { get; }
    int RequestMillisecondsTimeout { get; }
    long MaxRequestBodyBytes { get; }
    int MaxConcurrentRequests { get; }

    void GenerateNewPort();
}

[Export(typeof(IHttpServerConfiguration))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class HttpServerConfiguration() : IHttpServerConfiguration
{
    private const int TokenByteLength = 32;
    private const long OneMb = 1024 * 1024;
    private const int ThirtySeconds = 30000;
    private readonly Lazy<SecureString> lazyToken = new(GenerateSecureToken);
    private Lazy<int> lazyPort = new(GetAvailablePort);

    public int Port => lazyPort.Value;
    public SecureString Token => lazyToken.Value;
    public int MaxStartAttempts => 10;
    public int RequestMillisecondsTimeout => ThirtySeconds;
    public long MaxRequestBodyBytes => OneMb;
    public int MaxConcurrentRequests => 20;

    public void GenerateNewPort() => lazyPort = new Lazy<int>(GetAvailablePort);

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static SecureString GenerateSecureToken()
    {
        var bytes = new byte[TokenByteLength];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).ToSecureString();
    }
}

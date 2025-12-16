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
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

public interface IHttpServerConfigurationProvider
{
    IHttpServerConfiguration CurrentConfiguration { get; }
}

internal interface IHttpServerConfigurationFactory
{
    IHttpServerConfiguration SetNewConfiguration();
}

[Export(typeof(IHttpServerConfigurationProvider))]
[Export(typeof(IHttpServerConfigurationFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class HttpServerConfigurationProvider : IHttpServerConfigurationProvider, IHttpServerConfigurationFactory
{
    private readonly object lockObj = new();
    private IHttpServerConfiguration currentConfiguration = null!;

    [ImportingConstructor]
    public HttpServerConfigurationProvider() => SetNewConfiguration();

    public IHttpServerConfiguration SetNewConfiguration()
    {
        lock (lockObj)
        {
            currentConfiguration = new HttpServerConfiguration();
            return currentConfiguration;
        }
    }

    public IHttpServerConfiguration CurrentConfiguration
    {
        get
        {
            lock (lockObj)
            {
                return currentConfiguration;
            }
        }
    }

    private sealed class HttpServerConfiguration : IHttpServerConfiguration
    {
        private const int TokenByteLength = 32;
        private const string PortAnalysisPropertyKey = "sonar.sqvsRoslynPlugin.internal.serverPort";
        private const string TokenAnalysisPropertyKey = "sonar.sqvsRoslynPlugin.internal.serverToken";

        public int Port { get; } = GetAvailablePort();
        public SecureString Token { get; } = GenerateSecureToken();

        public Dictionary<string, string> MapToInferredProperties() => new() { { PortAnalysisPropertyKey, Port.ToString() }, { TokenAnalysisPropertyKey, Token.ToUnsecureString() } };

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
}

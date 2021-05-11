/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal interface IEslintBridgeHttpWrapper : IDisposable
    {
        Task<string> PostAsync(string serverEndpoint, object request, CancellationToken cancellationToken);
    }

    internal sealed class EslintBridgeHttpWrapper : IEslintBridgeHttpWrapper
    {
        private readonly ILogger logger;
        private readonly HttpClient httpClient;
        private readonly IEslintBridgeProcess eslintBridgeProcess;

        public EslintBridgeHttpWrapper(IEslintBridgeProcessFactory eslintBridgeProcessFactory, ILogger logger)
            : this(eslintBridgeProcessFactory, new HttpClientHandler(), logger)
        {
        }

        internal EslintBridgeHttpWrapper(IEslintBridgeProcessFactory eslintBridgeProcessFactory,
            HttpMessageHandler httpHandler,
            ILogger logger)
        {
            this.logger = logger;
            httpClient = new HttpClient(httpHandler);

            eslintBridgeProcess = eslintBridgeProcessFactory.Create();
        }

        public async Task<string> PostAsync(string serverEndpoint, object request, CancellationToken cancellationToken)
        {
            var port = await eslintBridgeProcess.Start();
            var fullServerUrl = BuildServerUri(port.Port, serverEndpoint);

            var serializedRequest = request == null
                ? string.Empty
                : JsonConvert.SerializeObject(request, Formatting.Indented);
            logger.LogDebug(Resources.INFO_RequestDetails, fullServerUrl, serializedRequest);

            var content = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(fullServerUrl, content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync();

            logger.LogDebug(Resources.INFO_ResponseDetails, fullServerUrl, responseString);

            return responseString;
        }

        public void Dispose()
        {
            eslintBridgeProcess?.Dispose();
            httpClient?.Dispose();
        }

        private Uri BuildServerUri(int port, string endpoint) => new Uri($"http://localhost:{port}/{endpoint}");
    }
}

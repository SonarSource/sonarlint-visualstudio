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
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal interface IEslintBridgeHttpWrapper : IDisposable
    {
        Task<string> PostAsync(string serverEndpoint, object request, CancellationToken cancellationToken);
    }

    [Export(typeof(IEslintBridgeHttpWrapper))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class EslintBridgeHttpWrapper : IEslintBridgeHttpWrapper
    {
        private readonly IEslintBridgeProcess eslintBridgeProcess;
        private readonly ILogger logger;
        private readonly HttpClient httpClient;

        [ImportingConstructor]
        public EslintBridgeHttpWrapper(IEslintBridgeProcess eslintBridgeProcess, ILogger logger)
            : this(eslintBridgeProcess, new HttpClientHandler(), logger)
        {
        }

        internal EslintBridgeHttpWrapper(IEslintBridgeProcess eslintBridgeProcess, HttpMessageHandler httpHandler, ILogger logger)
        {
            this.eslintBridgeProcess = eslintBridgeProcess;
            this.logger = logger;
            httpClient = new HttpClient(httpHandler);
        }

        public async Task<string> PostAsync(string serverEndpoint, object request, CancellationToken cancellationToken)
        {
            try
            {
                var serializedRequest = request == null ? string.Empty : JsonConvert.SerializeObject(request, Formatting.Indented);
                logger.LogDebug(Resources.INFO_RequestDetails, serverEndpoint, serializedRequest);

                var port = await eslintBridgeProcess.Start();
                var content = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"http://localhost:{port}/{serverEndpoint}", content, cancellationToken);
                var responseString = await response.Content.ReadAsStringAsync();

                logger.LogDebug(Resources.INFO_ResponseDetails, serverEndpoint, responseString);

                return responseString;
            }
            catch (AggregateException ex) 
            {
                var exceptions = string.Join(Environment.NewLine, ex.InnerExceptions.Select(x=> x.Message));
                logger.WriteLine(Resources.ERR_RequestFailure, serverEndpoint, exceptions);
                return null;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ERR_RequestFailure, serverEndpoint, ex.Message);
                return null;
            }
        }

        public void Dispose()
        {
            eslintBridgeProcess?.Dispose();
            httpClient?.Dispose();
        }
    }
}

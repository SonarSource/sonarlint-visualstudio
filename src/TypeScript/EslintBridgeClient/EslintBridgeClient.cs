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
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal interface IEslintBridgeClient : IDisposable
    {
        Task<AnalysisResponse> AnalyzeJs(string filePath);
    }

    [Export(typeof(IEslintBridgeClient))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class EslintBridgeClient : IEslintBridgeClient
    {
        private readonly IEslintBridgeHttpWrapper httpWrapper;
        private readonly ILogger logger;

        [ImportingConstructor]
        public EslintBridgeClient(IEslintBridgeHttpWrapper httpWrapper, ILogger logger)
        {
            this.httpWrapper = httpWrapper;
            this.logger = logger;
        }

        public async Task<AnalysisResponse> AnalyzeJs(string filePath)
        {
            try
            {
                var analysisRequest = new AnalysisRequest
                {
                    FilePath = filePath,
                    IgnoreHeaderComments = true,
                    TSConfigFilePaths = Array.Empty<string>() // eslint-bridge generates a default tsconfig for JS analysis
                };

                var responseString = await httpWrapper.PostAsync("analyze-js", analysisRequest);

                return responseString == null ? null : JsonConvert.DeserializeObject<AnalysisResponse>(responseString);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ERR_AnalyzeJsFailure, filePath, ex);
                return null;
            }
        }

        private Task Close()
        {
            return httpWrapper.PostAsync("close");
        }

        public async void Dispose()
        {
            await Close();
            httpWrapper.Dispose();
        }
    }
}

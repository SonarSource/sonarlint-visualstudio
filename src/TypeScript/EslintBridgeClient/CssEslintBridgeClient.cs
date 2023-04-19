/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    /// <summary>
    /// Eslint bridge client for /analyze-css endpoint
    /// </summary>
    internal interface ICssEslintBridgeClient : IEslintBridgeClient
    {
    }

    [Export(typeof(ICssEslintBridgeClient))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CssEslintBridgeClient : EslintBridgeClientBase, ICssEslintBridgeClient
    {
        private Rule[] rulesConfiguration;

        [ImportingConstructor]
        public CssEslintBridgeClient(IEslintBridgeProcessFactory eslintBridgeProcessFactory, ILogger logger)
            : this(eslintBridgeProcessFactory.Create(), logger)
        {
        }

        private CssEslintBridgeClient(IEslintBridgeProcess eslintBridgeProcess, ILogger logger)
            : this(eslintBridgeProcess, new EslintBridgeHttpWrapper(logger), new EslintBridgeKeepAlive(eslintBridgeProcess, logger))
        {
        }

        internal /* for testing */ CssEslintBridgeClient(IEslintBridgeProcess eslintBridgeProcess,
            IEslintBridgeHttpWrapper httpWrapper,
            IEslintBridgeKeepAlive eslintBridgeKeepAlive)
            : base("analyze-css", eslintBridgeProcess, httpWrapper, eslintBridgeKeepAlive)
        {
        }

        public Task InitLinter(IEnumerable<Rule> rules, CancellationToken cancellationToken)
        {
            rulesConfiguration = rules.ToArray();
            return Task.CompletedTask;
        }

        public async Task<AnalysisResponse> Analyze(string filePath, string tsConfigFilePath, CancellationToken cancellationToken)
        {
            var analysisRequest = new CssAnalysisRequest
            {
                FilePath = filePath,
                Rules = rulesConfiguration
            };

            var responseString = await EslintBridgeHttpHelper.MakeCallAsync(eslintBridgeProcess,
                httpWrapper,
                analyzeEndpoint,
                analysisRequest,
                cancellationToken);

            if (string.IsNullOrEmpty(responseString))
            {
                throw new InvalidOperationException(string.Format(Resources.ERR_InvalidResponse, responseString));
            }

            return JsonConvert.DeserializeObject<AnalysisResponse>(responseString);
        }
    }
}

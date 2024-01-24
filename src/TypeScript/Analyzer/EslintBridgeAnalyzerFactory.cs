/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    internal interface IEslintBridgeAnalyzerFactory
    {
        IEslintBridgeAnalyzer Create(IRulesProvider rulesProvider, IEslintBridgeClient eslintBridgeClient);
    }

    [Export(typeof(IEslintBridgeAnalyzerFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class EslintBridgeAnalyzerFactory : IEslintBridgeAnalyzerFactory
    {
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly IAnalysisConfigMonitor analysisConfigMonitor;
        private readonly ILogger logger;

        [ImportingConstructor]
        public EslintBridgeAnalyzerFactory(IActiveSolutionTracker activeSolutionTracker,
            IAnalysisConfigMonitor analysisConfigMonitor,
            ILogger logger)
        {
            this.activeSolutionTracker = activeSolutionTracker;
            this.analysisConfigMonitor = analysisConfigMonitor;
            this.logger = logger;
        }

        public IEslintBridgeAnalyzer Create(IRulesProvider rulesProvider, IEslintBridgeClient eslintBridgeClient)
        {
            return new EslintBridgeAnalyzer(rulesProvider,
                eslintBridgeClient,
                activeSolutionTracker,
                analysisConfigMonitor,
                new EslintBridgeIssueConverter(rulesProvider, logger),
                logger);
        }
    }
}

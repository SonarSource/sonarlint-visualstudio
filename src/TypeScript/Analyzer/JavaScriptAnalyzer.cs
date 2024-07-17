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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    // [Export(typeof(IAnalyzer))] disabled, to be removed
    internal sealed class JavaScriptAnalyzer : AnalyzerBase, IAnalyzer
    {
        [ImportingConstructor]
        public JavaScriptAnalyzer(IJavaScriptEslintBridgeClient eslintBridgeClient,
            IRulesProviderFactory rulesProviderFactory,
            ITelemetryManager telemetryManager,
            IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
            IEslintBridgeAnalyzerFactory eslintBridgeAnalyzerFactory,
            IThreadHandling threadHandling) :
            base(telemetryManager,
                analysisStatusNotifierFactory,
                eslintBridgeAnalyzerFactory,
                rulesProviderFactory,
                eslintBridgeClient,
                threadHandling,
                "javascript",
                Language.Js,
                "js",
                nameof(JavaScriptAnalyzer))
        {
        }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            return languages.Contains(AnalysisLanguage.Javascript);
        }

        public void ExecuteAnalysis(string path,
            Guid analysisId,
            string charset,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            IAnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(IsAnalysisSupported(detectedLanguages));

            ExecuteAsync(path, consumer, cancellationToken).Forget(); // fire and forget
        }
    }
}

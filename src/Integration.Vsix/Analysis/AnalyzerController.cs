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

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    [Export(typeof(IAnalyzerController))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class AnalyzerController : IAnalyzerController, IDisposable
    {
        private readonly ILogger logger;
        private readonly IEnumerable<IAnalyzer> analyzers;

        // The analyzer controller does not use the config monitor. However, something needs to MEF-import
        // the config monitor so that it is created, and the lifetimes of the analyzer controller and
        // config monitor should be the same so it is convenient to create it here.
        private readonly IAnalysisConfigMonitor analysisConfigMonitor;
        private readonly IAnalyzableFileIndicator analyzableFileIndicator;

        [ImportingConstructor]
        public AnalyzerController(ILogger logger,
            [ImportMany] IEnumerable<IAnalyzer> analyzers,
            IAnalysisConfigMonitor analysisConfigMonitor,
            IAnalyzableFileIndicator analyzableFileIndicator)
        {
            this.logger = logger;
            this.analyzers = analyzers;
            this.analysisConfigMonitor = analysisConfigMonitor;
            this.analyzableFileIndicator = analyzableFileIndicator;
        }

        #region IAnalyzerController implementation

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            bool isSupported = analyzers.Any(a => a.IsAnalysisSupported(languages));
            return isSupported;
        }

        public void ExecuteAnalysis(string path, Guid analysisId, string charset, IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer, IAnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
        {
            var supportedAnalyzers = analyzers.Where(x => x.IsAnalysisSupported(detectedLanguages)).ToList();
            var handled = false;

            if (supportedAnalyzers.Any() && analyzableFileIndicator.ShouldAnalyze(path))
            {
                handled = true;

                foreach (var analyzer in supportedAnalyzers)
                {
                    analyzer.ExecuteAnalysis(path, analysisId, charset, detectedLanguages, consumer, analyzerOptions, cancellationToken);
                }
            }

            if (!handled)
            {
                logger.LogVerbose($"[AnalyzerController] No analyzer supported analysis of {path}");
            }
        }

        #endregion IAnalyzerController implementation

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    (analysisConfigMonitor as IDisposable)?.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}

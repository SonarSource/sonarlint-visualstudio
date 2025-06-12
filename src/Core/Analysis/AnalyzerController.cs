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

namespace SonarLint.VisualStudio.Core.Analysis
{
    [Export(typeof(IAnalyzerController))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class AnalyzerController : IAnalyzerController, IDisposable
    {
        private readonly IAnalyzer analyzer;

        // The analyzer controller does not use the config monitor. However, something needs to MEF-import
        // the config monitor so that it is created, and the lifetimes of the analyzer controller and
        // config monitor should be the same so it is convenient to create it here.
        private readonly IAnalysisConfigMonitor analysisConfigMonitor;

        [ImportingConstructor]
        public AnalyzerController(
            IAnalysisConfigMonitor analysisConfigMonitor,
            IAnalyzer analyzer,
            ILogger logger)
        {
            this.analysisConfigMonitor = analysisConfigMonitor;
            this.analyzer = analyzer;
        }

        #region IAnalyzerController implementation

        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2047 Class will be completely dropped
        public void ExecuteAnalysis(
            string path,
            Guid analysisId,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IAnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken) =>
            analyzer.ExecuteAnalysis([path]);

        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2047 Class will be completely dropped
        public Task<Guid?> ExecuteAnalysis(List<string> paths) => throw new NotImplementedException();

        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2047 Class will be completely dropped
        public Task<Guid?> ExecuteAnalysisForOpenedFiles() => throw new NotImplementedException();

        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2047 Class will be completely dropped
        public void CancelAnalysis(Guid analysisId) => throw new NotImplementedException();

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

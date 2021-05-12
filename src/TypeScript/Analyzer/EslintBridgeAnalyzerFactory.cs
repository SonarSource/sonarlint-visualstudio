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

        [ImportingConstructor]
        public EslintBridgeAnalyzerFactory(IActiveSolutionTracker activeSolutionTracker, IAnalysisConfigMonitor analysisConfigMonitor)
        {
            this.activeSolutionTracker = activeSolutionTracker;
            this.analysisConfigMonitor = analysisConfigMonitor;
        }

        public IEslintBridgeAnalyzer Create(IRulesProvider rulesProvider, IEslintBridgeClient eslintBridgeClient)
        {
            return new EslintBridgeAnalyzer(rulesProvider, eslintBridgeClient, activeSolutionTracker, analysisConfigMonitor);
        }
    }
}

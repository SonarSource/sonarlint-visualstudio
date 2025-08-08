using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

internal interface ISonarRoslynProjectWrapper
{
    string Name { get; }

    AnalyzerOptions RoslynAnalyzerOptions { get; }

    bool ContainsDocument(
        string filePath,
        out string analysisFilePath);

    Task<ISonarRoslynCompilationWrapper> GetCompilationAsync(CancellationToken token);
}

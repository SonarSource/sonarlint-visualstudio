using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

internal interface ISonarRoslynProjectWrapper
{
    string Name { get; }

    bool SupportsCompilation { get; }

    AnalyzerOptions RoslynAnalyzerOptions { get; }

    bool ContainsDocument(
        string filePath,
        [NotNullWhen(true)]out string? analysisFilePath);

    Task<ISonarRoslynCompilationWrapper> GetCompilationAsync(CancellationToken token);
}

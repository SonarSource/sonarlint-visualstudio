using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

internal interface ISonarRoslynCompilationWrapper
{
    CompilationOptions RoslynCompilationOptions { get; }
    Language Language { get; }

    ISonarRoslynCompilationWrapper WithOptions(CompilationOptions withSpecificDiagnosticOptions);

    ISonarRoslynCompilationWithAnalyzersWrapper WithAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions compilationWithAnalyzersOptions);
}

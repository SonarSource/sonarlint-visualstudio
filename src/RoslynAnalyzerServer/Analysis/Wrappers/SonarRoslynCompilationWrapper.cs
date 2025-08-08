using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

internal class SonarRoslynCompilationWrapper(Compilation roslynCompilation) : ISonarRoslynCompilationWrapper
{
    public CompilationOptions RoslynCompilationOptions  => roslynCompilation.Options;
    public Language Language { get; } = roslynCompilation.Language switch
    {
        LanguageNames.CSharp => Language.CSharp,
        LanguageNames.VisualBasic => Language.VBNET,
        _ => throw new ArgumentOutOfRangeException(nameof(roslynCompilation.Language)),
    };

    public ISonarRoslynCompilationWrapper WithOptions(CompilationOptions withSpecificDiagnosticOptions) =>
        new SonarRoslynCompilationWrapper(roslynCompilation.WithOptions(withSpecificDiagnosticOptions));

    public CompilationWithAnalyzers WithAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions compilationWithAnalyzersOptions) =>
        roslynCompilation.WithAnalyzers(analyzers, compilationWithAnalyzersOptions);
}

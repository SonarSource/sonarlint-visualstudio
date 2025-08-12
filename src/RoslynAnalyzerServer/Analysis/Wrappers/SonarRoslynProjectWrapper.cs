using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

[ExcludeFromCodeCoverage] // todo SLVS-2466 add roslyn 'integration' tests using AdHocWorkspace
internal class SonarRoslynProjectWrapper(Project project) : ISonarRoslynProjectWrapper
{
    public string Name => project.Name;
    public bool SupportsCompilation => project.SupportsCompilation;
    public AnalyzerOptions RoslynAnalyzerOptions  => project.AnalyzerOptions;

    public async Task<ISonarRoslynCompilationWrapper> GetCompilationAsync(CancellationToken token) =>
        new SonarRoslynCompilationWrapper((await project.GetCompilationAsync(token))!);

    public bool ContainsDocument(
        string filePath,
        [NotNullWhen(true)]out string? analysisFilePath)
    {
        analysisFilePath = project.Documents
            .Select(document => document.FilePath)
            .Where(path => path != null)
            .FirstOrDefault(path =>
                path!.Equals(filePath)
                || (path.StartsWith(filePath)
                    && path.EndsWith(".g.cs")));

        return analysisFilePath != null;
    }

}

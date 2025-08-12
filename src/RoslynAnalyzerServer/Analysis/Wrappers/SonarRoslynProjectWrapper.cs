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
        analysisFilePath = null;
        foreach (var document in project.Documents)
        {
            if (document.FilePath is null)
            {
                continue;
            }

            if (document.FilePath.Equals(filePath)
                || (document.FilePath.StartsWith(filePath) && document.FilePath.EndsWith(".g.cs")))
            {
                analysisFilePath = document.FilePath;
                return true;
            }
        }
        return false;
    }

}

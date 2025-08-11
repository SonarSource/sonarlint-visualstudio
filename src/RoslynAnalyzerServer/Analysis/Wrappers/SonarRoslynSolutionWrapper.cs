using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

[ExcludeFromCodeCoverage] // todo add roslyn 'integration' tests using AdHocWorkspace
internal class SonarRoslynSolutionWrapper(Solution workspaceCurrentSolution) : ISonarRoslynSolutionWrapper
{
    public IEnumerable<ISonarRoslynProjectWrapper> Projects { get; } = workspaceCurrentSolution.Projects.Select(x => new SonarRoslynProjectWrapper(x));
}

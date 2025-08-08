using Microsoft.CodeAnalysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

internal class SonarRoslynSolutionWrapper(Solution workspaceCurrentSolution) : ISonarRoslynSolutionWrapper
{
    public IEnumerable<ISonarRoslynProjectWrapper> Projects { get; } = workspaceCurrentSolution.Projects.Select(x => new SonarRoslynProjectWrapper(x));
}

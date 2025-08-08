namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

internal interface ISonarRoslynWorkspaceWrapper
{
    ISonarRoslynSolutionWrapper CurrentSolution { get; }
}
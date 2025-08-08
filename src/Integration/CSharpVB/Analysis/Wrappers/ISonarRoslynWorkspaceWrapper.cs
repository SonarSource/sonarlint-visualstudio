namespace SonarLint.VisualStudio.Integration.CSharpVB.Analysis.Wrappers;

internal interface ISonarRoslynWorkspaceWrapper
{
    ISonarRoslynSolutionWrapper CurrentSolution { get; }
}
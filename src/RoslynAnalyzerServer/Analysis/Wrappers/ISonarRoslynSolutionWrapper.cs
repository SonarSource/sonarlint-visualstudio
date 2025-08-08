namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

internal interface ISonarRoslynSolutionWrapper
{
    public IEnumerable<ISonarRoslynProjectWrapper> Projects { get; }
}
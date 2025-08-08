namespace SonarLint.VisualStudio.Integration.CSharpVB.Analysis.Wrappers;

internal interface ISonarRoslynSolutionWrapper
{
    public IEnumerable<ISonarRoslynProjectWrapper> Projects { get; }
}
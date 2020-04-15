using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    public class CFamilyAnalyzerOptions : IAnalyzerOptions
    {
        public bool RunReproducer { get; set; }
    }
}

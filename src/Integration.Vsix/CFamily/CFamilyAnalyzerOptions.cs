using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    public class CFamilyAnalyzerOptions : IAnalyzerOptions
    {
        public bool RunReproducer { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    public interface ISuppressionExecutionContext
    {
        bool IsInConnectedMode { get; }
        string SonarProjectKey { get; }
    }
    internal class SuppressionExecutionContext : ISuppressionExecutionContext
    {
        private const string Exp = @"\\.sonarlint\\(?<sonarkey>[^\\/]+)\\(CSharp|VB)\\SonarLint.xml$";
        private static readonly Regex SonarLintFileRegEx = new Regex(Exp, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private readonly AnalyzerOptions analyzerOptions;


        public SuppressionExecutionContext(AnalyzerOptions analyzerOptions)
        {
            this.analyzerOptions = analyzerOptions;

            GetProjectKey();
        }

        private void GetProjectKey()
        {
            foreach (var item in analyzerOptions.AdditionalFiles)
            {
                var match = SonarLintFileRegEx.Match(item.Path);
                if (match.Success)
                {
                    SonarProjectKey = match.Groups["sonarkey"].Value;
                    return;
                }
            }
        }

        public bool IsInConnectedMode => SonarProjectKey != null;

        public string SonarProjectKey { get; private set; } = null;
    }
}

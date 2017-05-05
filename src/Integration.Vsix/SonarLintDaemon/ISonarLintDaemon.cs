using System;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    interface ISonarLintDaemon : IDisposable
    {
        bool IsInstalled { get; }

        void Install();
        bool IsRunning();
        void Start();
        void RequestAnalysis(string path, string charset);
    }
}

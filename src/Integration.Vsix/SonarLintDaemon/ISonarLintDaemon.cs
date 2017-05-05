using System;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    interface ISonarLintDaemon : IDisposable
    {
        bool IsInstalled { get; }
        bool IsRunning { get; }

        void Install();
        void Start();
        void RequestAnalysis(string path, string charset);
    }
}

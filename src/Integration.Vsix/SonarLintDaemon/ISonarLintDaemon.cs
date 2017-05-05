using System;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    interface ISonarLintDaemon : IDisposable
    {
        bool IsInstalled { get; }

        void Install();
        void Start();
    }
}

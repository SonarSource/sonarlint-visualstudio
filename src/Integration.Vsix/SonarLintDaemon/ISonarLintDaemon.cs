using System;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    interface ISonarLintDaemon : IDisposable
    {
        bool IsInstalled();
        void Install();
        void Start();
    }
}

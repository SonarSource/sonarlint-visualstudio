using Sonarlint;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    interface ISonarLintDaemon : IDisposable
    {
        bool IsInstalled { get; }
        bool IsRunning { get; }

        void Install();
        void Start();
        void RequestAnalysis(string path, string charset, IIssueConsumer consumer);
    }

    interface IIssueConsumer
    {
        void Accept(string path, IEnumerable<Issue> issue);
    }
}

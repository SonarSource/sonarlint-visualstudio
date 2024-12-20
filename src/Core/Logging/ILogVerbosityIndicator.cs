namespace SonarLint.VisualStudio.Core.Logging;

public interface ILogVerbosityIndicator
{
    bool IsVerboseEnabled { get; }
    bool IsThreadIdEnabled { get; }
}
namespace SonarLint.VisualStudio.Core.Logging;

public interface ILogWriter
{
    void WriteLine(string message);
}
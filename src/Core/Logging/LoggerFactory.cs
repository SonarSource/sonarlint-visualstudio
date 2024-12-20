using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Core.Logging;

[Export(typeof(ILoggerFactory))]
[PartCreationPolicy(CreationPolicy.NonShared)]
[method: ImportingConstructor]
public class LoggerFactory(ILogContextManager logContextManager) : ILoggerFactory
{
    public static ILoggerFactory Default { get; } = new LoggerFactory(new LogContextManager());
    public ILogger Create(ILogWriter logWriter, ILogVerbosityIndicator verbosityIndicator) =>
        new LoggerBase(logContextManager, logWriter, verbosityIndicator);
}
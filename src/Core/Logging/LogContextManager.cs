using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Core.Logging;

[Export(typeof(ILogContextManager))]
[PartCreationPolicy(CreationPolicy.NonShared)]
public class LogContextManager : ILogContextManager
{
    private readonly ImmutableList<string> contexts;
    private readonly ImmutableList<string> verboseContexts;
    private readonly Lazy<string> formatedContext;
    private readonly Lazy<string> formatedVerboseContext;

    [ImportingConstructor]
    public LogContextManager() : this(ImmutableList<string>.Empty, ImmutableList<string>.Empty) { }

    private LogContextManager(ImmutableList<string> contexts, ImmutableList<string> verboseContexts)
    {
        this.contexts = contexts;
        this.verboseContexts = verboseContexts;
        formatedContext = new Lazy<string>(() => MergeContextsIntoSingleProperty(contexts), LazyThreadSafetyMode.PublicationOnly);
        formatedVerboseContext = new Lazy<string>(() => MergeContextsIntoSingleProperty(verboseContexts), LazyThreadSafetyMode.PublicationOnly);
    }

    public string FormatedContext => formatedContext.Value;
    public string FormatedVerboseContext => formatedVerboseContext.Value;

    public ILogContextManager CreateAugmentedContext(IEnumerable<string> additionalContexts) => new LogContextManager(contexts.AddRange(additionalContexts), verboseContexts);

    public ILogContextManager CreateAugmentedVerboseContext(IEnumerable<string> additionalVerboseContexts) => new LogContextManager(contexts, verboseContexts.AddRange(additionalVerboseContexts));

    private static string MergeContextsIntoSingleProperty(ImmutableList<string> contexts) => contexts.Count > 0 ? string.Join(" > ", contexts) : null;
}

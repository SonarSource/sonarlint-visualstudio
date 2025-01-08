/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */


using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Core.Logging;

[Export(typeof(ILoggerContextManager))]
[PartCreationPolicy(CreationPolicy.NonShared)]
internal class LoggerContextManager : ILoggerContextManager
{
    private const string Separator = " > ";
    private readonly ImmutableList<string> contexts;
    private readonly ImmutableList<string> verboseContexts;
    private readonly Lazy<string> formatedContext;
    private readonly Lazy<string> formatedVerboseContext;

    [ImportingConstructor]
    public LoggerContextManager() : this(ImmutableList<string>.Empty, ImmutableList<string>.Empty) { }

    private LoggerContextManager(ImmutableList<string> contexts, ImmutableList<string> verboseContexts)
    {
        this.contexts = contexts;
        this.verboseContexts = verboseContexts;
        formatedContext = new Lazy<string>(() => MergeContextsIntoSingleProperty(contexts), LazyThreadSafetyMode.PublicationOnly);
        formatedVerboseContext = new Lazy<string>(() => MergeContextsIntoSingleProperty(verboseContexts), LazyThreadSafetyMode.PublicationOnly);
    }

    public ILoggerContextManager CreateAugmentedContext(IEnumerable<string> additionalContexts) => new LoggerContextManager(contexts.AddRange(FilterContexts(additionalContexts)), verboseContexts);

    public ILoggerContextManager CreateAugmentedVerboseContext(IEnumerable<string> additionalVerboseContexts) => new LoggerContextManager(contexts, verboseContexts.AddRange(FilterContexts(additionalVerboseContexts)));

    public string GetFormattedContextOrNull(MessageLevelContext messageLevelContext) =>
        GetContextInternal(formatedContext.Value, messageLevelContext.Context);
    public string GetFormattedVerboseContextOrNull(MessageLevelContext messageLevelContext) =>
        GetContextInternal(formatedVerboseContext.Value, messageLevelContext.VerboseContext);

    private static IEnumerable<string> FilterContexts(IEnumerable<string> contexts) => contexts.Where(context => !string.IsNullOrEmpty(context));

    private static string GetContextInternal(string baseContext, IReadOnlyCollection<string> messageLevelContext)
    {
        if (messageLevelContext is not { Count: > 0 })
        {
            return baseContext;
        }

        IEnumerable<string> resultingContext = FilterContexts(messageLevelContext);
        if (baseContext != null)
        {
            resultingContext = resultingContext.Prepend(baseContext);
        }

        return MergeContextsIntoSingleProperty(resultingContext);
    }

    private static string MergeContextsIntoSingleProperty(IEnumerable<string> contexts)
    {
        var joinResult = string.Join(Separator, contexts);
        return string.IsNullOrEmpty(joinResult) ? null : joinResult;
    }
}

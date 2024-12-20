/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

namespace SonarLint.VisualStudio.Core;

public class LogContext : ILogContext
{
    private readonly ImmutableList<string> contexts;
    private readonly Lazy<string> formatedContext;
    public string FormatedContext => formatedContext.Value;
    private readonly ImmutableList<string> verboseContexts;
    private readonly Lazy<string> formatedVerboseContext;
    public string FormatedVerboseContext => formatedVerboseContext.Value;

    public ILogContext CreateAugmentedContext(IEnumerable<string> newContexts) => new LogContext(contexts.AddRange(newContexts), verboseContexts);

    public ILogContext CreateAugmentedVerboseContext(IEnumerable<string> newVerboseContexts) => new LogContext(contexts, this.verboseContexts.AddRange(newVerboseContexts));

    public LogContext() : this(ImmutableList<string>.Empty, ImmutableList<string>.Empty) { }

    private LogContext(ImmutableList<string> contexts, ImmutableList<string> verboseContexts)
    {
        this.contexts = contexts;
        this.verboseContexts = verboseContexts;
        formatedContext = new Lazy<string>(() => MergeContextsIntoSingleProperty(contexts), LazyThreadSafetyMode.PublicationOnly);
        formatedVerboseContext = new Lazy<string>(() => MergeContextsIntoSingleProperty(verboseContexts), LazyThreadSafetyMode.PublicationOnly);
    }

    private static string MergeContextsIntoSingleProperty(ImmutableList<string> contexts) => contexts.Count > 0 ? string.Join(" > ", contexts) : null;
}

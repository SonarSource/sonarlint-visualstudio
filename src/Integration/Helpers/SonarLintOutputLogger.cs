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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Text;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Helpers;

[Export(typeof(ILogger))]
[Export(typeof(IContextualLogger))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class SonarLintOutputLogger : IContextualLogger
{
    private readonly IServiceProvider serviceProvider;
    private readonly ISonarLintSettings sonarLintSettings;
    private readonly ImmutableList<string> contexts;
    private readonly ImmutableList<string> verboseContexts;
    private string contextsProperty;
    private string verboseContextsProperty;

    private bool VerboseLogsEnabled => sonarLintSettings.DaemonLogLevel == DaemonLogLevel.Verbose;

    [ImportingConstructor]
    public SonarLintOutputLogger(
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        ISonarLintSettings sonarLintSettings)
        : this(serviceProvider, sonarLintSettings, ImmutableList<string>.Empty, ImmutableList<string>.Empty)
    {
    }

    private SonarLintOutputLogger(
        IServiceProvider serviceProvider,
        ISonarLintSettings sonarLintSettings,
        ImmutableList<string> contexts,
        ImmutableList<string> verboseContexts)
    {
        this.serviceProvider = serviceProvider;
        this.sonarLintSettings = sonarLintSettings;
        this.contexts = contexts;
        this.verboseContexts = verboseContexts;
        contextsProperty = MergeContextsIntoSingleProperty(contexts);
        verboseContextsProperty = MergeContextsIntoSingleProperty(verboseContexts);
    }
    private static string MergeContextsIntoSingleProperty(ImmutableList<string> contexts) => contexts.Count > 0 ? string.Join(" > ", contexts) : null;

    public IContextualLogger ForContext(params string[] context) =>
        new SonarLintOutputLogger(serviceProvider, sonarLintSettings, contexts.AddRange(context.Where(x => !string.IsNullOrEmpty(x))), verboseContexts);

    public IContextualLogger ForVerboseContext(params string[] context) =>
        new SonarLintOutputLogger(serviceProvider, sonarLintSettings, contexts, verboseContexts.AddRange(context.Where(x => !string.IsNullOrEmpty(x))));

    public void WriteLine(string message) =>
        WriteToOutputPane(CreateStandardLogPrefix().Append(message).ToString());

    public void WriteLine(string messageFormat, params object[] args) =>
        WriteToOutputPane(CreateStandardLogPrefix().AppendFormat(CultureInfo.CurrentCulture, messageFormat, args).ToString());

    private StringBuilder CreateStandardLogPrefix() => AddStandardProperties(new StringBuilder());

    public void LogVerbose(string messageFormat, params object[] args)
    {
        if (VerboseLogsEnabled)
        {
            var debugLogPrefix = CreateDebugLogPrefix();
            var logLine = args.Length > 0
                ? debugLogPrefix.AppendFormat(CultureInfo.CurrentCulture, messageFormat, args)
                : debugLogPrefix.Append(messageFormat);
            WriteToOutputPane(logLine.ToString());
        }
    }

    private StringBuilder CreateDebugLogPrefix() => AppendProperty(AddStandardProperties(new StringBuilder()), "DEBUG");

    private StringBuilder AddStandardProperties(StringBuilder builder)
    {
        if (VerboseLogsEnabled)
        {
            AppendPropertyFormat(builder, "ThreadId {0, 3}", Thread.CurrentThread.ManagedThreadId);
        }

        if (contextsProperty != null)
        {
            AppendProperty(builder, contextsProperty);
        }

        if (VerboseLogsEnabled && verboseContextsProperty != null)
        {
            AppendProperty(builder, verboseContextsProperty);
        }

        return builder;
    }

    private static StringBuilder AppendProperty(StringBuilder builder, string property) => builder.Append('[').Append(property).Append(']').Append(' ');

    private static StringBuilder AppendPropertyFormat(StringBuilder builder, string property, params object[] args) => builder.Append('[').AppendFormat(property, args).Append(']').Append(' ');

    private void WriteToOutputPane(string message) => VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, message);
}

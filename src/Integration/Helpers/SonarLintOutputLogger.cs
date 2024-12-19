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
    private readonly string contextPropertyValue;

    private bool DebugLogsEnabled => sonarLintSettings.DaemonLogLevel == DaemonLogLevel.Verbose;

    [ImportingConstructor]
    public SonarLintOutputLogger(
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        ISonarLintSettings sonarLintSettings)
        : this(serviceProvider, sonarLintSettings, ImmutableList<string>.Empty)
    {
    }

    private SonarLintOutputLogger(
        IServiceProvider serviceProvider,
        ISonarLintSettings sonarLintSettings,
        ImmutableList<string> contexts)
    {
        this.serviceProvider = serviceProvider;
        this.sonarLintSettings = sonarLintSettings;
        this.contexts = contexts;
        contextPropertyValue = contexts.Count > 0 ? string.Join(" > ", contexts) : null;;
    }

    public IContextualLogger ForContext(params string[] context) =>
        new SonarLintOutputLogger(serviceProvider, sonarLintSettings, contexts.AddRange(context));

    public void WriteLine(string message) =>
        WriteToOutputPane(CreateStandardLogPrefix().Append(message).ToString());

    public void WriteLine(string messageFormat, params object[] args) =>
        WriteToOutputPane(CreateStandardLogPrefix().AppendFormat(CultureInfo.CurrentCulture, messageFormat, args).ToString());

    private StringBuilder CreateStandardLogPrefix() => AddStandardProperties(new StringBuilder());

    public void LogVerbose(string messageFormat, params object[] args)
    {
        if (DebugLogsEnabled)
        {
            var debugLogPrefix = CreateDebugLogPrefix();
            var logLine = args.Length > 0
                ? debugLogPrefix.AppendFormat(CultureInfo.CurrentCulture, messageFormat, args)
                : debugLogPrefix.Append(messageFormat);
            WriteToOutputPane(logLine.ToString());
        }
    }

    private StringBuilder CreateDebugLogPrefix()
    {
        var builder = new StringBuilder();
        AppendProperty(builder, "DEBUG");
        AddStandardProperties(builder);
        return builder;
    }

    private StringBuilder AddStandardProperties(StringBuilder builder)
    {
        if (sonarLintSettings.DaemonLogLevel == DaemonLogLevel.Verbose)
        {
            AppendPropertyFormat(builder, "ThreadId {0}", Thread.CurrentThread.ManagedThreadId);
        }

        if (contextPropertyValue != null)
        {
            AppendProperty(builder, contextPropertyValue);
        }

        return builder;
    }

    private static void AppendProperty(StringBuilder builder, string property) => builder.Append('[').Append(property).Append(']').Append(' ');

    private static void AppendPropertyFormat(StringBuilder builder, string property, params object[] args) => builder.Append('[').AppendFormat(property, args).Append(']').Append(' ');

    private void WriteToOutputPane(string message) => VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, message);
}

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

using System.Globalization;
using System.Text;

namespace SonarLint.VisualStudio.Core.Logging;

internal class LoggerBase(
    ILogContextManager logContextManager,
    ILogWriter logWriter,
    ILogVerbosityIndicator logVerbosityIndicator) : ILogger
{
    public ILogger ForContext(params string[] context) =>
        new LoggerBase(
            logContextManager.CreateAugmentedContext(context.Where(x => !string.IsNullOrEmpty(x))),
            logWriter,
            logVerbosityIndicator);

    public ILogger ForVerboseContext(params string[] context) =>
        new LoggerBase(
            logContextManager.CreateAugmentedVerboseContext(context.Where(x => !string.IsNullOrEmpty(x))),
            logWriter,
            logVerbosityIndicator);

    public void WriteLine(string message) =>
        logWriter.WriteLine(CreateStandardLogPrefix().Append(message).ToString());

    public void WriteLine(string messageFormat, params object[] args) =>
        logWriter.WriteLine(CreateStandardLogPrefix().AppendFormat(CultureInfo.CurrentCulture, messageFormat, args).ToString());

    public void LogVerbose(string messageFormat, params object[] args)
    {
        if (!logVerbosityIndicator.IsVerboseEnabled)
        {
            return;
        }

        var debugLogPrefix = CreateDebugLogPrefix();
        var logLine = args.Length > 0
            ? debugLogPrefix.AppendFormat(CultureInfo.CurrentCulture, messageFormat, args)
            : debugLogPrefix.Append(messageFormat);
        logWriter.WriteLine(logLine.ToString());
    }

    private StringBuilder CreateStandardLogPrefix() =>
        AddStandardProperties(new StringBuilder());

    private StringBuilder CreateDebugLogPrefix() =>
        AppendProperty(AddStandardProperties(new StringBuilder()), "DEBUG");

    private StringBuilder AddStandardProperties(StringBuilder builder)
    {
        if (logVerbosityIndicator.IsThreadIdEnabled)
        {
            AppendPropertyFormat(builder, "ThreadId {0, 3}", Thread.CurrentThread.ManagedThreadId);
        }

        if (logContextManager.FormatedContext != null)
        {
            AppendProperty(builder, logContextManager.FormatedContext);
        }

        if (logVerbosityIndicator.IsVerboseEnabled && logContextManager.FormatedVerboseContext != null)
        {
            AppendProperty(builder, logContextManager.FormatedVerboseContext);
        }

        return builder;
    }

    private static StringBuilder AppendProperty(StringBuilder builder, string property) =>
        builder.Append('[').Append(property).Append(']').Append(' ');

    private static StringBuilder AppendPropertyFormat(StringBuilder builder, string property, params object[] args) =>
        builder.Append('[').AppendFormat(property, args).Append(']').Append(' ');
}

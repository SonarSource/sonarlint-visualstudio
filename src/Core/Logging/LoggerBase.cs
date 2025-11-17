/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
    ILoggerContextManager contextManager,
    ILoggerWriter writer,
    ILoggerSettingsProvider settingsProvider) : ILogger
{

    public ILogger ForContext(params string[] context) =>
        new LoggerBase(
            contextManager.CreateAugmentedContext(context),
            writer,
            settingsProvider);

    public ILogger ForVerboseContext(params string[] context) =>
        new LoggerBase(
            contextManager.CreateAugmentedVerboseContext(context),
            writer,
            settingsProvider);

    public void WriteLine(string messageFormat, params object[] args) =>
        WriteLine(default, messageFormat, args);

    public void WriteLine(MessageLevelContext context, string messageFormat, params object[] args) =>
        writer.WriteLine(CreateStandardLogPrefix(context).AppendMessage(messageFormat, args).ToString());

    public void LogVerbose(string messageFormat, params object[] args) =>
        LogVerbose(default, messageFormat, args);

    public void LogVerbose(MessageLevelContext context, string messageFormat, params object[] args)
    {
        if (!settingsProvider.IsVerboseEnabled)
        {
            return;
        }

        writer.WriteLine(CreateDebugLogPrefix(context).AppendMessage(messageFormat, args).ToString());
    }

    private StringBuilder CreateStandardLogPrefix(MessageLevelContext context = default) =>
        AddStandardProperties(new StringBuilder(), context);

    private StringBuilder CreateDebugLogPrefix(MessageLevelContext context = default) =>
        AddStandardProperties(new StringBuilder().AppendProperty("DEBUG"), context);

    private StringBuilder AddStandardProperties(StringBuilder builder, MessageLevelContext context)
    {
        if (settingsProvider.IsThreadIdEnabled)
        {
            builder.AppendProperty("ThreadId " + Thread.CurrentThread.ManagedThreadId);
        }

        if (contextManager.GetFormattedContextOrNull(context) is var formatedContext && !string.IsNullOrEmpty(formatedContext))
        {
            builder.AppendProperty(formatedContext);
        }

        if (settingsProvider.IsVerboseEnabled && contextManager.GetFormattedVerboseContextOrNull(context) is var formattedVerboseContext && !string.IsNullOrEmpty(formattedVerboseContext))
        {
            builder.AppendProperty(formattedVerboseContext);
        }

        return builder;
    }
}

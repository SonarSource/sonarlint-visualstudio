/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu;

/// <summary>
/// Base for the hidden Connected Mode commands used by the end-to-end tests. They are in no menu, and they
/// are unavailable unless the IDE process has SONARLINT_INTERNAL_E2E_COMMANDS=true.
/// They take two space-separated arguments and run synchronously: the caller (DTE) returns only when the
/// operation is done, and gets an error if it failed.
/// </summary>
internal abstract class ConnectedModeAutomationCommandBase(IEnvironmentSettings environmentSettings, IThreadHandling threadHandling, ILogger logger)
    : VsCommandBase
{
    internal const string DisabledText = "Connected Mode automation commands are disabled.";

    /// <summary>Used in log and error messages; never includes argument values.</summary>
    protected abstract string Name { get; }

    protected abstract string ArgumentsDescription { get; }

    protected abstract Task<ResponseStatus> ExecuteAsync(string firstArgument, string secondArgument);

    protected override void QueryStatusInternal(OleMenuCommand command)
    {
        var enabled = environmentSettings.AreConnectedModeAutomationCommandsEnabled();
        command.Supported = enabled;
        command.Visible = enabled;
        command.Enabled = enabled;
    }

    protected override void InvokeInternal() => InvokeInternal(null);

    protected override void InvokeInternal(string argument)
    {
        // Checked again here: a caller can raise a command without querying its status first.
        if (!environmentSettings.AreConnectedModeAutomationCommandsEnabled())
        {
            throw new InvalidOperationException(DisabledText);
        }

        var arguments = argument?.Split((char[])null, StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (arguments.Length != 2)
        {
            throw new ArgumentException($"{Name} expects the arguments: {ArgumentsDescription}");
        }

        var result = threadHandling.Run(() => ExecuteAsync(arguments[0], arguments[1]));
        if (!result.Success)
        {
            logger.WriteLine("[E2E] {0} failed: {1}", Name, result.WarningText);
            throw new InvalidOperationException($"{Name} failed: {result.WarningText}");
        }
        logger.WriteLine("[E2E] {0} succeeded", Name);
    }
}

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

using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.DebugMenu;

/// <summary>
/// Command handler for the Debug menu visibility.
/// </summary>
/// <remarks>Has no invocation logic; only has QueryStatus</remarks>
internal class DebugMenuCommand : VsCommandBase
{
    public const int Id = 0x102A;

    protected override void InvokeInternal()
    {
    }

    protected override void QueryStatusInternal(OleMenuCommand command)
    {
#if DEBUG
        command.Visible = true;
        command.Enabled = true;
#else
        command.Enabled = false;
        command.Visible = false;
#endif
    }
}

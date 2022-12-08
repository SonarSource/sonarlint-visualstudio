/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands
{
    internal class ToggleDebugLogsCommand : VsCommandBase
    {
        internal const int Id = 0x108;

        private bool shouldLog = false;

        public ToggleDebugLogsCommand()
        {
            ILoggerExtensions.ShouldLogDebug(shouldLog);
        }

        protected override void InvokeInternal()
        {
            shouldLog = !shouldLog;
            ILoggerExtensions.ShouldLogDebug(shouldLog);
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Text = shouldLog ? "Disable Debug Logs" : "Enable Debug Logs";
        }
    }
}

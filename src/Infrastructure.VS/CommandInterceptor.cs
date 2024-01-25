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

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    public class CommandInterceptor : IOleCommandTarget
    {
        private readonly CommandID commandID;
        private readonly Func<CommandProgression> function;
        private readonly IThreadHandling threadHandling;

        public CommandInterceptor(CommandID commandID, Func<CommandProgression> function) : this(commandID, function, ThreadHandling.Instance)
        {
        }

        internal CommandInterceptor(CommandID commandID, Func<CommandProgression> function, IThreadHandling threadHandling)
        {
            this.commandID = commandID;
            this.function = function;
            this.threadHandling = threadHandling;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            threadHandling.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == commandID.Guid && nCmdID == commandID.ID && function() == CommandProgression.Stop)
            {
                return VSConstants.S_OK;
            }

            return (int)Microsoft.VisualStudio.OLE.Interop.Constants.MSOCMDERR_E_FIRST;
        }
    }

    /// <summary>
    /// Holds values on how the command execution should proceed.
    /// </summary>
    public enum CommandProgression
    {
        /// <summary>Proceed to execute the next command handler for the command.</summary>
        Continue,

        /// <summary>Stop execution and don't continue execution to the next command handler.</summary>
        Stop,
    }
}

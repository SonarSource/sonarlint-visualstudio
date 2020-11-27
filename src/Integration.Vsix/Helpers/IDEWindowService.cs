/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.Native;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(IIDEWindowService))]
    internal class IDEWindowService : IIDEWindowService
    {
        private readonly INativeMethods nativeMethods;
        private readonly IntPtr handle;
        private readonly ILogger logger;

        [ImportingConstructor]
        public IDEWindowService(ILogger logger)
            : this(new NativeMethods(), Process.GetCurrentProcess().MainWindowHandle, logger)
        {
        }

        internal /* for testing */ IDEWindowService(INativeMethods nativeMethods, IntPtr handle, ILogger logger)
        {
            this.nativeMethods = nativeMethods;
            this.handle = handle;
            this.logger = logger;
        }

        public void BringToFront()
        {
            try
            {
                logger.WriteLine(Strings.IDEWindowService_BringingToFront);
                if (handle == IntPtr.Zero)
                {
                    logger.WriteLine(Strings.IDEWindowService_InvalidWindowHandle);
                    return;
                }

                RestoreIfMinimized(handle);
                BringToFront(handle); // We'll still try to "set foreground" even if one of the restore calls failed
            }
            catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.IDEWindowService_GeneralError, ex.Message);
            }
        }

        private void RestoreIfMinimized(IntPtr handle)
        {
            var placement = new WINDOWPLACEMENT();
            placement.length = (uint)Marshal.SizeOf(placement);
            var success = nativeMethods.GetWindowPlacement(handle, ref placement);
            if (!success)
            {
                LogWinApiFailure(nameof(nativeMethods.GetWindowPlacement));
                return;
            }

            if (placement.showCmd == NativeMethods.SW_SHOWMINIMIZED)
            {
                logger.WriteLine(Strings.IDEWindowServer_IDEIsMinimized);
                success = nativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
                if (!success)
                {
                    LogWinApiFailure(nameof(nativeMethods.ShowWindow));
                }
            }
        }
        private void BringToFront(IntPtr handle)
        {
            var success = nativeMethods.SetForegroundWindow(handle);
            if (!success)
            {
                LogWinApiFailure(nameof(nativeMethods.SetForegroundWindow));
            }
        }

        private void LogWinApiFailure(string apiName) =>
            logger.LogDebug(Strings.IDEWindowService_WinAPICallFailed, apiName, Marshal.GetLastWin32Error());
    }
}

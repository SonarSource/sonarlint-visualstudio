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
using static SonarLint.VisualStudio.Integration.Helpers.NativeMethods;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    [Export(typeof(IIDEWindowService))]
    internal class IDEWindowService : IIDEWindowService
    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public IDEWindowService(ILogger logger)
        {
            this.logger = logger;
        }

        public void BringToFront()
        {
            try
            {
                logger.WriteLine($"Bringing IDE to front...");
                var handle = Process.GetCurrentProcess().MainWindowHandle;
                if (handle == IntPtr.Zero)
                {
                    logger.WriteLine($"Invalid window handle");
                    return;
                }

                BringToFront(handle);
            }
            catch(Exception ex)
            {
                logger.WriteLine($"Error bringing IDE to front: {ex.Message}");
            }
        }

        private void BringToFront(IntPtr handle)
        {
            var placement = new WINDOWPLACEMENT();
            placement.length = (uint)Marshal.SizeOf(placement);
            var success = GetWindowPlacement(handle, ref placement);
            if (!success)
            {
                logger.WriteLine($"Failed to get window placement. Last error code: {Marshal.GetLastWin32Error()}");
                return;
            }

            if (placement.showCmd == SW_SHOWMINIMIZED)
            {
                logger.WriteLine("IDE is minimized. Restoring...");
                success = ShowWindow(handle, SW_RESTORE);
                if (!success)
                {
                    logger.WriteLine($"Failed to show IDE window. Last error code: {Marshal.GetLastWin32Error()}");
                }
            }

            success = SetForegroundWindow(handle);
            if (!success)
            {
                logger.WriteLine($"Failed to bring IDE to front. Last error code: {Marshal.GetLastWin32Error()}");
            }
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public uint length;
            public uint flags;
            public uint showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
            public RECT rcDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X, Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        public const uint SW_SHOWMINIMIZED = 2;
        public const int SW_RESTORE = 9;
    }
}

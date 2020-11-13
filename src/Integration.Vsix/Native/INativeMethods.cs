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
using System.Runtime.InteropServices;

namespace SonarLint.VisualStudio.Integration.Vsix.Native
{
    // Interface for testing
    internal interface INativeMethods
    {
        IntPtr GetAncestor(IntPtr hwnd, uint flags);

        int SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        bool SetForegroundWindow(IntPtr hWnd);

        bool ShowWindow(IntPtr hWnd, int nCmdShow);

        bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
    }

    // Passthrough-implementation of the interface
    internal class NativeMethods : INativeMethods
    {
        public const uint GetRootWindow = 2;
        public const uint WM_CLOSE = 0x10;

        public const uint SW_SHOWMINIMIZED = 2;
        public const int SW_RESTORE = 9;

        public IntPtr GetAncestor(IntPtr hwnd, uint flags) => NativeMethodDeclarations.GetAncestor(hwnd, flags);

        public int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam) => NativeMethodDeclarations.SendMessage(hWnd, Msg, wParam, lParam);

        public bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl) => NativeMethodDeclarations.GetWindowPlacement(hWnd, ref lpwndpl);

        public bool SetForegroundWindow(IntPtr hWnd) => NativeMethodDeclarations.SetForegroundWindow(hWnd);

        public bool ShowWindow(IntPtr hWnd, int nCmdShow) => NativeMethodDeclarations.ShowWindow(hWnd, nCmdShow);

        // Private nested class that contains the native method declarations
        // (the method names are the same as the interface so they can't be
        // declared in the parent class).
        private static class NativeMethodDeclarations
        {
            [DllImport("user32.dll", ExactSpelling = true)]
            public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        }
    }

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
}

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Windows.Forms;
using SonarLint.VisualStudio.Integration.Vsix.Native;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class NativeInterop
    {
        private readonly INativeMethods nativeMethods;

        public NativeInterop()
            : this(new NativeMethods()){}

        internal /* for testing */ NativeInterop(INativeMethods nativeMethods)
        {
            this.nativeMethods = nativeMethods;
        }

        public void CloseRootWindow(IWin32Window win32Window)
        {
            // There doesn't appear to be a VS API to close Tools, Options so 
            // we're using PInvoke.
            // NB this is equivalent to clicking Cancel i.e. any changed settings will 
            // not be saved.
            var dialogHwnd = win32Window?.Handle ?? IntPtr.Zero;
            if (dialogHwnd != IntPtr.Zero)
            {
                var topLevelHwnd = nativeMethods.GetAncestor(dialogHwnd, NativeMethods.GetRootWindow);
                if (topLevelHwnd != IntPtr.Zero)
                {
                    nativeMethods.SendMessage(topLevelHwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
            }
        }
    }
}

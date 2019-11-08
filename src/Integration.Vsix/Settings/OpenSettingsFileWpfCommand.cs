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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class OpenSettingsFileWpfCommand : ICommand
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly ILogger logger;
        private readonly IWin32Window win32Window;

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
        private const uint GetRootWindow = 2;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        private const uint WM_CLOSE = 0x10;

        public OpenSettingsFileWpfCommand(IServiceProvider serviceProvider, IUserSettingsProvider userSettingsProvider, IWin32Window win32Window, ILogger logger)
        {
            this.serviceProvider = serviceProvider;
            this.userSettingsProvider = userSettingsProvider;
            this.logger = logger;
            this.win32Window = win32Window;
        }

        #region ICommand methods

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            try
            {
                userSettingsProvider.EnsureFileExists();
                OpenDocumentInVs(userSettingsProvider.SettingsFilePath);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.ToolsOptions_ErrorOpeningSettingsFile, userSettingsProvider.SettingsFilePath, ex.Message);
            }
        }

        protected virtual /* for testing */ void OpenDocumentInVs(string filePath)
        {
            // TryOpenDocument calls several other VS services. From a testing point of view, it's simpler to
            // create a subclass and override this method.
            var viewType = Guid.Empty;
            VsShellUtilities.TryOpenDocument(serviceProvider, filePath, viewType, out var _, out var _, out var _);

            CloseOptionsDialog(win32Window);
        }

        #endregion ICommand methods

        private static void CloseOptionsDialog(IWin32Window win32Window)
        {
            // There doesn't appear to be a VS API to close Tools, Options so 
            // we're using PInvoke.
            // NB this is equivalent to clicking Cancel i.e. any changed settings will
            // not be saved.
            var dialogHwnd = win32Window?.Handle ?? IntPtr.Zero;
            if (dialogHwnd != IntPtr.Zero)
            {
                var topLevelHwnd = GetAncestor(dialogHwnd, GetRootWindow);
                if (topLevelHwnd != IntPtr.Zero)
                {
                    SendMessage(topLevelHwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
            }
        }
    }
}

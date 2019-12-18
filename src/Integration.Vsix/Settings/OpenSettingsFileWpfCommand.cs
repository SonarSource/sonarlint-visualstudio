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
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class OpenSettingsFileWpfCommand : ICommand
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly ILogger logger;
        private readonly IWin32Window win32Window;

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

            new NativeInterop().CloseRootWindow(win32Window);
        }

        #endregion ICommand methods
    }
}

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Windows.Forms;
using System.Windows.Input;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.Settings;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class OpenSettingsFileWpfCommand : ICommand
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IGlobalSettingsStorage globalSettingsStorage;
        private readonly ILogger logger;
        private readonly IWin32Window win32Window;

        public OpenSettingsFileWpfCommand(
            IServiceProvider serviceProvider,
            IGlobalSettingsStorage globalSettingsStorage,
            IWin32Window win32Window,
            ILogger logger)
        {
            this.serviceProvider = serviceProvider;
            this.globalSettingsStorage = globalSettingsStorage;
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
                globalSettingsStorage.EnsureSettingsFileExists();
                OpenDocumentInVs(globalSettingsStorage.SettingsFilePath);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.ToolsOptions_ErrorOpeningSettingsFile, globalSettingsStorage.SettingsFilePath, ex.Message);
            }
        }

        protected virtual /* for testing */ void OpenDocumentInVs(string filePath)
        {
            DocumentOpener.OpenDocumentInVs(serviceProvider, filePath);
            new NativeInterop().CloseRootWindow(win32Window);
        }

        #endregion ICommand methods
    }
}

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Windows;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu
{
    internal class SaveSharedConnectionCommand : VsCommandBase
    {
        internal const int Id = 0x103;

        private readonly ISharedBindingConfigProvider sharedBindingConfigProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IMessageBox messageBox;

        public SaveSharedConnectionCommand(IConfigurationProvider configurationProvider, ISharedBindingConfigProvider sharedBindingConfigProvider) : this(configurationProvider, sharedBindingConfigProvider, new Core.MessageBox())
        { }

        internal /*For testing*/ SaveSharedConnectionCommand(IConfigurationProvider configurationProvider, ISharedBindingConfigProvider sharedBindingConfigProvider, IMessageBox messageBox)
        {
            this.sharedBindingConfigProvider = sharedBindingConfigProvider;
            this.configurationProvider = configurationProvider;
            this.messageBox = messageBox;
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = configurationProvider.GetConfiguration().Mode != SonarLintMode.Standalone;
        }

        protected override void InvokeInternal()
        {
            var project = configurationProvider.GetConfiguration().Project;

            var sharedBindingConfig = new SharedBindingConfigModel { ProjectKey = project.ProjectKey, Uri = project.ServerUri.ToString(), Organization = project.Organization?.Key };

            var saveResult = sharedBindingConfigProvider.SaveSharedBinding(sharedBindingConfig);

            if (saveResult)
            {
                messageBox.Show(Strings.SaveSharedConnectionCommand_SaveSuccess_Message, Strings.SaveSharedConnectionCommand_SaveSuccess_Caption, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                messageBox.Show(Strings.SaveSharedConnectionCommand_SaveFail_Message, Strings.SaveSharedConnectionCommand_SaveFail_Caption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

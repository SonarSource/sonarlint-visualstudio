/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class ManageConnectionsCommand : VsCommandBase
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IConnectedModeUIManager connectedModeUiManager;
        internal const int Id = 0x102;

        public ManageConnectionsCommand(IActiveSolutionBoundTracker activeSolutionBoundTracker, IConnectedModeUIManager connectedModeUiManager)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.connectedModeUiManager = connectedModeUiManager;
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            var isConnected = activeSolutionBoundTracker.CurrentConfiguration.Mode.IsInAConnectedMode();
            command.Text = isConnected ? Resources.Strings.BindingButton_ConnectedText : Resources.Strings.BindingButton_StandaloneText;
        }

        protected override void InvokeInternal()
        {
            connectedModeUiManager.ShowManageBindingDialogAsync().Forget();
        }
    }
}

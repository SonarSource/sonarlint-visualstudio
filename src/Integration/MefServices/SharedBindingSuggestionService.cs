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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    internal interface ISharedBindingSuggestionService
    {
        void Suggest(ServerType? serverType, Func<ICommand<ConnectConfiguration>> connectCommandProvider);

        void Close();
    }
    
    [Export(typeof(ISharedBindingSuggestionService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SharedBindingSuggestionService : ISharedBindingSuggestionService
    {
        internal /* for testing */ readonly ConnectConfiguration autobindEnabledConfiguration =
            new ConnectConfiguration { UseSharedBinding = true };

        private readonly ISuggestSharedBindingGoldBar suggestSharedBindingGoldBar;
        private readonly ITeamExplorerController teamExplorerController;
        private readonly IConnectedModeWindowEventBasedScheduler connectedModeWindowEventBasedScheduler;

        [ImportingConstructor]
        public SharedBindingSuggestionService(ISuggestSharedBindingGoldBar suggestSharedBindingGoldBar,
            ITeamExplorerController teamExplorerController,
            IConnectedModeWindowEventBasedScheduler connectedModeWindowEventBasedScheduler)
        {
            this.suggestSharedBindingGoldBar = suggestSharedBindingGoldBar;
            this.teamExplorerController = teamExplorerController;
            this.connectedModeWindowEventBasedScheduler = connectedModeWindowEventBasedScheduler;
        }
        
        public void Suggest(ServerType? serverType, Func<ICommand<ConnectConfiguration>> connectCommandProvider)
        {
            if (serverType == null)
            {
                return;
            }
            
            suggestSharedBindingGoldBar.Show(serverType.Value, () => ConnectAfterTeamExplorerInitialized(connectCommandProvider));
        }

        public void Close()
        {
            suggestSharedBindingGoldBar.Close();
        }

        private void ConnectAfterTeamExplorerInitialized(Func<ICommand<ConnectConfiguration>> connectCommandProvider)
        {
            if (IsConnectedModeWindowLoaded(connectCommandProvider, out var connectCommand))
            {
                teamExplorerController.ShowSonarQubePage();
                Autobind(connectCommand);
            }
            else
            {
                connectedModeWindowEventBasedScheduler.ScheduleActionOnNextEvent(() => Autobind(connectCommandProvider()));
                teamExplorerController.ShowSonarQubePage();
            }
        }

        private bool IsConnectedModeWindowLoaded(Func<ICommand<ConnectConfiguration>> connectCommandProvider,
            out ICommand<ConnectConfiguration> connectCommand)
        {
            connectCommand = connectCommandProvider();
            return connectCommand != null;
        }
        
        private void Autobind(ICommand<ConnectConfiguration> connectCommand)
        {
            if (connectCommand?.CanExecute(autobindEnabledConfiguration) ?? false)
            {
                connectCommand.Execute(autobindEnabledConfiguration);
            }
        }
    }
}

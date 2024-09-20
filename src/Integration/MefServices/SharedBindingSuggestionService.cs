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

using System.ComponentModel.Composition;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    internal interface ISharedBindingSuggestionService
    {
        void Suggest(ServerType? serverType);

        void Close();
    }
    
    [Export(typeof(ISharedBindingSuggestionService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SharedBindingSuggestionService : ISharedBindingSuggestionService
    {
        private readonly ISuggestSharedBindingGoldBar suggestSharedBindingGoldBar;
        private readonly IConnectedModeServices connectedModeServices;
        private readonly IConnectedModeBindingServices connectedModeBindingServices;

        [ImportingConstructor]
        public SharedBindingSuggestionService(ISuggestSharedBindingGoldBar suggestSharedBindingGoldBar,
            IConnectedModeServices connectedModeServices,
            IConnectedModeBindingServices connectedModeBindingServices)
        {
            this.suggestSharedBindingGoldBar = suggestSharedBindingGoldBar;
            this.connectedModeServices = connectedModeServices;
            this.connectedModeBindingServices = connectedModeBindingServices;
        }
        
        public void Suggest(ServerType? serverType)
        {
            if (serverType == null)
            {
                return;
            }
            
            suggestSharedBindingGoldBar.Show(serverType.Value, AutoBind);
        }

        public void Close()
        {
            suggestSharedBindingGoldBar.Close();
        }

        private void AutoBind()
        {
            new ManageBindingDialog(connectedModeServices, connectedModeBindingServices).ShowDialog(Application.Current.MainWindow);
        }
    }
}

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
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    public interface ISharedBindingSuggestionService : IDisposable
    {
        void Suggest();
    }
    
    [Export(typeof(ISharedBindingSuggestionService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SharedBindingSuggestionService : ISharedBindingSuggestionService
    {
        private readonly ISuggestSharedBindingGoldBar suggestSharedBindingGoldBar;
        private readonly IConnectedModeServices connectedModeServices;
        private readonly IConnectedModeBindingServices connectedModeBindingServices;
        private readonly IActiveSolutionTracker activeSolutionTracker;

        [ImportingConstructor]
        public SharedBindingSuggestionService(
            ISuggestSharedBindingGoldBar suggestSharedBindingGoldBar,
            IConnectedModeServices connectedModeServices,
            IConnectedModeBindingServices connectedModeBindingServices,
            IActiveSolutionTracker activeSolutionTracker)
        {
            this.suggestSharedBindingGoldBar = suggestSharedBindingGoldBar;
            this.connectedModeServices = connectedModeServices;
            this.connectedModeBindingServices = connectedModeBindingServices;
            this.activeSolutionTracker = activeSolutionTracker;

            this.activeSolutionTracker.ActiveSolutionChanged += OnActiveSolutionChanged;
        }

        public void Suggest()
        {
            var sharedBindingConfig = connectedModeBindingServices.SharedBindingConfigProvider.GetSharedBinding();
            var isStandalone = connectedModeServices.ConfigurationProvider.GetConfiguration().Mode == SonarLintMode.Standalone;

            if (sharedBindingConfig?.GetServerType() is { } serverType && isStandalone)
            {
                suggestSharedBindingGoldBar.Show(serverType, ShowManageBindingDialog);
            }
        }

        public void Dispose()
        {
            activeSolutionTracker.ActiveSolutionChanged -= OnActiveSolutionChanged;
        }

        private void ShowManageBindingDialog()
        {
            var manageBindingDialog = new ManageBindingDialog(connectedModeServices, connectedModeBindingServices, useSharedBindingOnInitialization:true);
            manageBindingDialog.ShowDialog(Application.Current.MainWindow);
        }

        private void OnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
        {
            if (e.IsSolutionOpen)
            {
                Suggest();
            }
        }
    }
}

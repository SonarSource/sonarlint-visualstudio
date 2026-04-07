/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.SLCore;

namespace SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;

[ExcludeFromCodeCoverage]
internal sealed partial class SupportedLanguagesDialogWindow : Window
{
    public SupportedLanguagesDialogViewModel ViewModel { get; }

    public SupportedLanguagesDialogWindow(
        IPluginStatusesStore pluginStatusesStore,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        ISLCoreHandler slCoreHandler,
        IServerConnectionsRepository serverConnectionsRepository,
        IConnectedModeUIManager connectedModeUiManager,
        IThreadHandling threadHandling,
        ITelemetryManager telemetryManager)
    {
        try
        {
            ViewModel = new SupportedLanguagesDialogViewModel(pluginStatusesStore, activeConfigScopeTracker, slCoreHandler, serverConnectionsRepository,
                connectedModeUiManager, threadHandling, telemetryManager);
            Closed += DisposeViewModel;
            InitializeComponent();
        }
        catch (Exception)
        {
            Closed -= DisposeViewModel;
            ViewModel?.Dispose();
            throw;
        }
    }

    private void DisposeViewModel(object sender, EventArgs args) => ViewModel.Dispose();

    private void SetUpConnection_Click(object sender, RoutedEventArgs e) => ViewModel.SetUpConnection();

    private void RestartBackend_Click(object sender, RoutedEventArgs e) => ViewModel.RestartBackend();
}

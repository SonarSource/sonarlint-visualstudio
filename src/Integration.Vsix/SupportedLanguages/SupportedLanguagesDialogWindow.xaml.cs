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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;

[ExcludeFromCodeCoverage]
internal sealed partial class SupportedLanguagesDialogWindow : Window
{
    private readonly IConnectedModeUIManager connectedModeUIManager;

    public SupportedLanguagesDialogViewModel ViewModel { get; }

    public SupportedLanguagesDialogWindow(IPluginStatusesStore pluginStatusesStore, Core.IThreadHandling threadHandling, IConnectedModeUIManager connectedModeUIManager)
    {
        this.connectedModeUIManager = connectedModeUIManager;
        ViewModel = new SupportedLanguagesDialogViewModel(pluginStatusesStore, threadHandling);
        InitializeComponent();
    }

    private void SetUpConnection_Click(object sender, RoutedEventArgs e)
    {
        connectedModeUIManager.ShowManageBindingDialogAsync().Forget();
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PluginStatusDto plugin })
        {
            MessageBox.Show($"Retry clicked for: {plugin.pluginName} (state: {plugin.state})", "Debug");
        }
    }
}

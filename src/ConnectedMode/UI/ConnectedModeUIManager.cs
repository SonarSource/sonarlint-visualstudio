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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.TrustConnection;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.UI;

public interface IConnectedModeUIManager
{
    Task<bool> ShowManageBindingDialogAsync(BindingRequest.AutomaticBindingRequest automaticBinding = null);

    Task<bool?> ShowTrustConnectionDialogAsync(ServerConnection serverConnection, string token);

    Task<bool?> ShowEditCredentialsDialogAsync(Connection connection);
}

[Export(typeof(IConnectedModeUIManager))]
[PartCreationPolicy(CreationPolicy.NonShared)]
[method: ImportingConstructor]
internal sealed class ConnectedModeUIManager(IConnectedModeServices connectedModeServices, IConnectedModeBindingServices connectedModeBindingServices, IConnectedModeUIServices connectedModeUiServices)
    : IConnectedModeUIManager
{
    public async Task<bool> ShowManageBindingDialogAsync(BindingRequest.AutomaticBindingRequest automaticBinding = null)
    {
        var result = false;
        await connectedModeServices.ThreadHandling.RunOnUIThreadAsync(() => result = ShowDialogManageBinding(automaticBinding));

        return result;
    }

    public async Task<bool?> ShowTrustConnectionDialogAsync(ServerConnection serverConnection, string token)
    {
        bool? dialogResult = null;
        await connectedModeServices.ThreadHandling.RunOnUIThreadAsync(() => dialogResult = GetTrustConnectionDialogResult(serverConnection, token));

        return dialogResult;
    }

    public async Task<bool?> ShowEditCredentialsDialogAsync(Connection connection)
    {
        bool? dialogResult = null;
        await connectedModeServices.ThreadHandling.RunOnUIThreadAsync(() => dialogResult = GetEditCredentialsDialogResult(connection));

        return dialogResult;
    }

    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    private bool? GetTrustConnectionDialogResult(ServerConnection serverConnection, string token)
    {
        var trustConnectionDialog = new TrustConnectionDialog(connectedModeServices, connectedModeUiServices, serverConnection, token?.ToSecureString());
        return trustConnectionDialog.ShowDialog(Application.Current.MainWindow);
    }

    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    private bool ShowDialogManageBinding(BindingRequest.AutomaticBindingRequest automaticBinding)
    {
        var manageBindingDialog = new ManageBindingDialog(connectedModeServices, connectedModeBindingServices, connectedModeUiServices, this, automaticBinding);
        manageBindingDialog.ShowDialog(Application.Current.MainWindow);
        return manageBindingDialog.ViewModel.BoundProject != null;
    }

    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    private bool? GetEditCredentialsDialogResult(Connection connection)
    {
        var editCredentialsDialog = new EditCredentialsDialog(this, connectedModeServices, connectedModeUiServices, connectedModeBindingServices, connection);
        return editCredentialsDialog.ShowDialog(Application.Current.MainWindow);
    }
}

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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.TrustConnection;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.UI;

public interface IConnectedModeUIManager
{
    void ShowManageBindingDialog(bool useSharedBindingOnInitialization = false);

    Task<bool?> ShowTrustConnectionDialogAsync(ServerConnection serverConnection, string token);
}

[Export(typeof(IConnectedModeUIManager))]
[PartCreationPolicy(CreationPolicy.NonShared)]
[method: ImportingConstructor]
internal sealed class ConnectedModeUIManager(IConnectedModeServices connectedModeServices, IConnectedModeBindingServices connectedModeBindingServices)
    : IConnectedModeUIManager
{
    public void ShowManageBindingDialog(bool useSharedBindingOnInitialization = false) =>
        connectedModeServices.ThreadHandling.RunOnUIThread(() => ShowDialogManageBinding(useSharedBindingOnInitialization));

    public async Task<bool?> ShowTrustConnectionDialogAsync(ServerConnection serverConnection, string token)
    {
        bool? dialogResult = null;
        await connectedModeServices.ThreadHandling.RunOnUIThreadAsync(() => dialogResult = GetTrustConnectionDialogResult(serverConnection, token));

        return dialogResult;
    }

    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    private bool? GetTrustConnectionDialogResult(ServerConnection serverConnection, string token)
    {
        var trustConnectionDialog = new TrustConnectionDialog(connectedModeServices, serverConnection, token?.ToSecureString());
        return trustConnectionDialog.ShowDialog(Application.Current.MainWindow);
    }

    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    private void ShowDialogManageBinding(bool useSharedBindingOnInitialization)
    {
        var manageBindingDialog = new ManageBindingDialog(connectedModeServices, connectedModeBindingServices, useSharedBindingOnInitialization ? new AutomaticBindingRequest.Shared() : null);
        manageBindingDialog.ShowDialog(Application.Current.MainWindow);
    }
}

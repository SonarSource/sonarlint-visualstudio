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

using System.Diagnostics.CodeAnalysis;

namespace SonarLint.VisualStudio.ConnectedMode.UI.Credentials;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
public class EditCredentialsDialog : CredentialsDialog
{
    private readonly EditCredentialsViewModel editConnectionTokenViewModel;

    internal EditCredentialsDialog(
        IConnectedModeServices connectedModeServices,
        IConnectedModeUIServices connectedModeUiServices,
        IConnectedModeBindingServices connectedModeBinding,
        Connection connection) : base(connectedModeServices, connectedModeUiServices, CreateViewModel(connectedModeServices, connectedModeBinding, connection), withNextButton: false)
    {
        editConnectionTokenViewModel = (EditCredentialsViewModel)ViewModel;
    }

    private static EditCredentialsViewModel CreateViewModel(IConnectedModeServices connectedModeServices, IConnectedModeBindingServices connectedModeBinding, Connection connection) =>
        new(connection, connectedModeServices, connectedModeBinding, new ProgressReporterViewModel(connectedModeServices.Logger));

    protected override async Task BeforeWindowCloseWithSuccessAsync() => await editConnectionTokenViewModel.UpdateConnectionCredentialsWithProgressAsync();
}

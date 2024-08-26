﻿/*
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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.VisualStudio;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;

namespace SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;

[ExcludeFromCodeCoverage]
public partial class OrganizationSelectionDialog : Window
{
    private readonly IConnectedModeServices connectedModeServices;

    public OrganizationSelectionDialog(IConnectedModeServices connectedModeServices, ICredentialsModel credentialsModel)
    {
        this.connectedModeServices = connectedModeServices;
        ViewModel = new OrganizationSelectionViewModel(credentialsModel, connectedModeServices.SlCoreConnectionAdapter, new ProgressReporterViewModel());
        InitializeComponent();
    }

    public OrganizationSelectionViewModel ViewModel { get; }

    private void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private async void ChooseAnotherOrganizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedOrganization = null;
        var manualOrganizationSelectionDialog = new ManualOrganizationSelectionDialog();
        var isSelectedManualOrganizationValid = await ValidateManualOrganizationAsync(manualOrganizationSelectionDialog);

        if (isSelectedManualOrganizationValid)
        {
            ViewModel.SelectedOrganization = new OrganizationDisplay(manualOrganizationSelectionDialog.ViewModel.OrganizationKey, null);
            DialogResult = true;
        }
    }

    private async Task<bool> ValidateManualOrganizationAsync(ManualOrganizationSelectionDialog manualOrganizationSelectionDialog)
    {
        try
        {
            var manualSelectionDialogSucceeded = manualOrganizationSelectionDialog.ShowDialog(this);
            var organizationSelectionInvalidMsg = string.Format(UiResources.ManualOrganziationSelectionFailedText, manualOrganizationSelectionDialog.ViewModel.OrganizationKey);
            var isConnectionValid = await ViewModel.ValidateConnectionForOrganizationAsync(manualOrganizationSelectionDialog.ViewModel.OrganizationKey, organizationSelectionInvalidMsg);

            return manualSelectionDialogSucceeded is true && isConnectionValid;
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            connectedModeServices.Logger.WriteLine(e.ToString());
            return false;
        }
    }

    private async void OrganizationSelectionDialog_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadOrganizationsAsync();
    }
}


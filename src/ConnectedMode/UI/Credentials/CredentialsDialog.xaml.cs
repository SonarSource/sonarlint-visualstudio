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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UI.Credentials;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
public partial class CredentialsDialog : Window
{
    private readonly IConnectedModeServices connectedModeServices;
    private readonly IConnectedModeUIServices connectedModeUiServices;

    public CredentialsViewModel ViewModel { get; }

    public CredentialsDialog(
        IConnectedModeServices connectedModeServices,
        IConnectedModeUIServices connectedModeUiServices,
        ConnectionInfo connectionInfo,
        bool withNextButton)
    {
        this.connectedModeServices = connectedModeServices;
        this.connectedModeUiServices = connectedModeUiServices;
        ViewModel = new CredentialsViewModel(connectionInfo,
            connectedModeServices.SlCoreConnectionAdapter,
            new ProgressReporterViewModel(connectedModeServices.Logger));
        InitializeComponent();

        ConfirmationBtn.Content = withNextButton ? UiResources.NextButton : UiResources.OkButton;
    }

    private void TokenPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) => ViewModel.Token = TokenBox.SecurePassword;

    private async void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        var isConnectionValid = await IsConnectionValidAsync();
        if (!isConnectionValid)
        {
            return;
        }
        CloseWindowWithSuccess();
    }

    private void CloseWindowWithSuccess()
    {
        DialogResult = true;
        Close();
    }

    private async Task<bool> IsConnectionValidAsync()
    {
        try
        {
            return await ViewModel.ValidateConnectionAsync();
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            connectedModeServices.Logger.WriteLine(e.ToString());
            return false;
        }
    }

    private async void Generate_OnClick(object sender, RoutedEventArgs e)
    {
        var responseWithData = await ViewModel.GenerateTokenWithProgressAsync();
        if (ViewModel.TokenGenerationCancellationSource.IsCancellationRequested)
        {
            return;
        }
        if (responseWithData.Success)
        {
            TokenBox.Password = responseWithData.ResponseData;
            connectedModeUiServices.IdeWindowService.BringToFront();
            CloseWindowWithSuccess();
        }
        else
        {
            connectedModeUiServices.BrowserService.Navigate(ViewModel.AccountSecurityUrl);
        }
    }

    private void CredentialsDialog_OnClosing(object sender, CancelEventArgs e) => CredentialsViewModel.CancelAndDisposeCancellationToken(ViewModel.TokenGenerationCancellationSource);
}

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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UI.Credentials
{
    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    public partial class CredentialsDialog : Window
    {
        private readonly IConnectedModeServices connectedModeServices;

        public CredentialsDialog(IConnectedModeServices connectedModeServices, ConnectionInfo connectionInfo, bool withNextButton)
        {
            this.connectedModeServices = connectedModeServices;
            ViewModel = new CredentialsViewModel(connectionInfo, connectedModeServices.SlCoreConnectionAdapter);
            InitializeComponent();

            ConfirmationBtn.Content = withNextButton ? UiResources.NextButton : UiResources.OkButton;
        }

        public CredentialsViewModel ViewModel { get; }

        private void GenerateTokenHyperlink_Navigate(object sender, RequestNavigateEventArgs e)
        {
            NavigateToAccountSecurityUrl();
        }

        private void NavigateToAccountSecurityUrl()
        {
            connectedModeServices.BrowserService.Navigate(ViewModel.AccountSecurityUrl);
        }

        private void GenerateLinkIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            NavigateToAccountSecurityUrl();
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Password = PasswordBox.Password;
        }

        private void TokenPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Token = TokenBox.Password;
        }

        private void AuthenticationTypeCombobox_OnSourceUpdated(object sender, DataTransferEventArgs dataTransferEventArgs)
        {
            if (ViewModel.SelectedAuthenticationType == UiResources.AuthenticationTypeOptionToken)
            {
                ViewModel.Username = null;
                PasswordBox.Password = string.Empty;
            }
            else if (ViewModel.SelectedAuthenticationType == UiResources.AuthenticationTypeOptionCredentials)
            {
                TokenBox.Password = string.Empty;
            }
        }

        private async void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var isConnectionValid = await IsConnectionValidAsync();
            if(!isConnectionValid)
            {
                return;
            }
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
    }
}

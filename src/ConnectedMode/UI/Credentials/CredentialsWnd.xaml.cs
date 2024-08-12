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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static SonarLint.VisualStudio.ConnectedMode.ConnectionInfo;

namespace SonarLint.VisualStudio.ConnectedMode.UI.Credentials
{
    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    public partial class CredentialsWnd : Window
    {
        private readonly IBrowserService browserService;

        public CredentialsWnd(IBrowserService browserService, Connection connection, bool isWizardMode)
        {
            this.browserService = browserService;
            ViewModel = new CredentialsViewModel(connection);
            InitializeComponent();

            ConfirmationBtn.Content = isWizardMode ? UiResources.Next : UiResources.Ok;
        }

        public CredentialsViewModel ViewModel { get; }

        private void GenerateToken_Navigate(object sender, RequestNavigateEventArgs e)
        {
            NavigateToAccountSecurityUrl();
        }

        private void NavigateToAccountSecurityUrl()
        {
            browserService.Navigate(ViewModel.AccountSecurityUrl);
        }

        private void GenerateLinkIcon_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToAccountSecurityUrl();
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Password = Password.Password;
        }

        private void Token_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Token = Token.Password;
        }

        private void AuthenticationType_SourceUpdated(object sender, DataTransferEventArgs dataTransferEventArgs)
        {
            if (ViewModel.SelectedAuthenticationType == UiResources.Token)
            {
                ViewModel.Username = null;
                Password.Password = string.Empty;
            }
            else if (ViewModel.SelectedAuthenticationType == UiResources.Credentials)
            {
                Token.Password = string.Empty;
            }
        }
    }
}

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.VisualStudio.PlatformUI;

namespace SonarLint.VisualStudio.Integration.Connection.UI
{
    /// <summary>
    /// Interaction logic for ConnectionInfoDialogView.xaml
    /// </summary>
    [ContentProperty(nameof(ConnectionInfoDialogView))]
    public partial class ConnectionInfoDialogView : DialogWindow
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(ConnectionInfoDialogViewModel), typeof(ConnectionInfoDialogView));

        internal ConnectionInfoDialogViewModel ViewModel
        {
            get { return (ConnectionInfoDialogViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }


        internal ConnectionInfoDialogView()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(this.ViewModel.IsValid, "Should not be able to click 'Connect' if model is not valid.");

            // Close dialog in the affirmative
            this.DialogResult = true;
        }

        internal SecureString Password
        {
            get
            {
                return this.PasswordInput.SecurePassword;
            }
        }

        #region Credential validation

        /* Since the password is not accessible from the view model (cannot be bound due to security concerns), we
         * must trigger revalidation of credentials from the view, passing the SecureString password along.
         */

        private void UsernameInput_TextChanged(object sender, TextChangedEventArgs e) => this.ValidateCredentials();

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e) => this.ValidateCredentials();

        // initial credential validation on dialog display
        private void OnLoaded(object sender, RoutedEventArgs e) => this.ValidateCredentials();

        /// <summary>
        /// Cause validation to occur for the credentials. This is performed separately from other validation
        /// because the <see cref="System.Windows.Controls.PasswordBox"/> control does not allow binding to
        /// the password for reasons of security.
        /// </summary>
        private void ValidateCredentials()
        {
            Debug.Assert(this.ViewModel != null, "ViewModel should be set before calling ShowDialog");

            this.ViewModel?.ValidateCredentials(this.Password);
        }

        #endregion
    }
}

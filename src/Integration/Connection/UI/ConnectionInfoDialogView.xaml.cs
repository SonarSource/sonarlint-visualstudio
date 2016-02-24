//-----------------------------------------------------------------------
// <copyright file="ConnectionInfoDialogView.xaml.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.PlatformUI;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

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

        /* Since the password is not accessable from the view model (cannot be bound due to security concerns), we
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

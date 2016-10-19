//-----------------------------------------------------------------------
// <copyright file="ConnectionInformationDialog.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Connection.UI;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Diagnostics;
using System.Security;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.Connection
{
    internal class ConnectionInformationDialog
    {
        #region Static helpers
        private static ConnectionInfoDialogView CreateView()
        {
            return new ConnectionInfoDialogView();
        }

        internal /*for testing purposes*/ static ConnectionInfoDialogViewModel CreateViewModel(ConnectionInformation currentConnection)
        {
            var vm = new ConnectionInfoDialogViewModel();
            if (currentConnection != null)
            {
                vm.ServerUrlRaw = currentConnection.ServerUri.AbsoluteUri;
                vm.Username = currentConnection.UserName;
            }

            return vm;
        }


        internal /* testing purposes */ static ConnectionInformation CreateConnectionInformation(ConnectionInfoDialogViewModel viewModel, SecureString password)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            ConnectionInformation info = null;

            Debug.Assert(viewModel.IsValid, "View model should be valid when creating connection information");
            if (viewModel.IsValid)
            {
                Uri serverUri = viewModel.ServerUrl;
                string username = viewModel.Username;

                info = new ConnectionInformation(serverUri, username, password);
            }

            return info;
        }
        #endregion

        /// <summary>
        /// Opens a window and returns only when the dialog is closed.
        /// </summary>
        /// <param name="currentConnection">Optional, the current connection information to show in the dialog</param>
        /// <returns>Captured connection information if closed successfully, null otherwise.</returns>
        public ConnectionInformation ShowDialog(ConnectionInformation currentConnection)
        {
            ConnectionInfoDialogViewModel vm = CreateViewModel(currentConnection);
            ConnectionInfoDialogView dialog = CreateView();
            dialog.ViewModel = vm;
            dialog.Owner = Application.Current.MainWindow;

            bool? result = dialog.ShowDialog();

            if (result.GetValueOrDefault())
            {
                return CreateConnectionInformation(vm, dialog.Password);
            }

            return null;
        }
    }
}

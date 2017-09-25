/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using System;
using System.Diagnostics;
using System.Security;
using System.Windows;
using SonarLint.VisualStudio.Integration.Connection.UI;
using SonarQube.Client.Models;

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

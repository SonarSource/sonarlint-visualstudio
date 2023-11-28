/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Connection.UI;
using SonarLint.VisualStudio.Integration.Transition;
using SonarQube.Client.Models;

//TODO: This should be cleaned up if you see this reject the PR
namespace SonarLint.VisualStudio.Integration.Connection
{
    internal class ConnectionInformationDialog
    {
        #region Static helpers

        private static MuteWindowDialog CreateView()
        {
            return new MuteWindowDialog(true);
        }

        internal /*for testing purposes*/ static ConnectionInfoDialogViewModel CreateViewModel(ConnectionInformation currentConnection)
        {
            var vm = new ConnectionInfoDialogViewModel();
            if (currentConnection != null)
            {
                vm.ServerUrlRaw = currentConnection.ServerUri.AbsoluteUri;
                // Security: don't populate the user name field, as this might be a token
                // See https://github.com/SonarSource/sonarlint-visualstudio/issues/1081
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
            ThreadHandling.Instance.RunOnBackgroundThread(() => test(currentConnection)).Forget();

            return null;
        }

        private async Task<ConnectionInformation> test(ConnectionInformation currentConnection)
        {
            //await ThreadHandling.Instance.SwitchToBackgroundThread();
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

            ConnectionInfoDialogViewModel vm = CreateViewModel(currentConnection);

            bool? result = null; string comment; SonarQubeIssueTransition? sonarQubeIssueTransition;

            await ThreadHandling.Instance.RunOnUIThreadAsync(() =>
            {
                tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
                var dialog = CreateView();
                //dialog.ViewModel = vm;

                dialog.Owner = Application.Current.MainWindow;

                result = dialog.ShowDialog();
                comment = dialog.Comment;
                sonarQubeIssueTransition = dialog.SelectedIssueTransition;
            });

            //await ThreadHandling.Instance.SwitchToBackgroundThread();
            tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

            if (result.GetValueOrDefault())
            {
                return null;
            }

            return null;
        }
    }
}

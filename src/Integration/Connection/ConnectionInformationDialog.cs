/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

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

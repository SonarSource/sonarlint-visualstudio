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

using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using SonarLint.VisualStudio.Integration.State;
using System;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    internal class ConnectSectionViewModel : TeamExplorerSectionViewModelBase,
                                        IUserNotification /* Most of it implemented by TeamExplorerSectionViewModelBase */
    {
        private TransferableVisualState state;
        private ICommand connectCommand;
        private ICommand bindCommand;
        private ICommand browseToUrl;

        public ConnectSectionViewModel()
        {
            this.Title = Resources.Strings.ConnectSectionTitle;
            this.IsExpanded = true;
            this.IsVisible = true;
        }

        #region IUserNotification

        public void ShowNotificationError(string message, Guid notificationId, ICommand associatedCommand)
        {
            this.ShowNotification(message, NotificationType.Error, NotificationFlags.NoTooltips/*No need for them since we don't use hyperlinks*/, associatedCommand, notificationId);
        }

        public void ShowNotificationWarning(string message, Guid notificationId, ICommand associatedCommand)
        {
            this.ShowNotification(message, NotificationType.Warning, NotificationFlags.NoTooltips/*No need for them since we don't use hyperlinks*/, associatedCommand, notificationId);
        }

        #endregion

        #region Properties

        public TransferableVisualState State
        {
            get { return this.state; }
            set { this.SetAndRaisePropertyChanged(ref this.state, value); }
        }

        #endregion

        #region Commands

        public ICommand ConnectCommand
        {
            get { return this.connectCommand; }
            set { SetAndRaisePropertyChanged(ref this.connectCommand, value); }
        }

        public ICommand BindCommand
        {
            get { return this.bindCommand; }
            set { SetAndRaisePropertyChanged(ref this.bindCommand, value); }
        }

        public ICommand BrowseToUrlCommand
        {
            get { return this.browseToUrl; }
            set { SetAndRaisePropertyChanged(ref this.browseToUrl, value); }
        }

        #endregion
    }
}

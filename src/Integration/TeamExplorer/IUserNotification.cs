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

using System;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    /// <summary>
    /// Show notifications to the user
    /// </summary>
    internal interface IUserNotification
    {
        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowBusy"/>
        /// </summary>
        void ShowBusy();

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.HideBusy"/>
        /// </summary>
        void HideBusy();

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowError(string)"/>
        /// </summary>
        void ShowError(string errorMessage);

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowException(Exception, bool)"/>
        /// </summary>
        void ShowException(Exception ex, bool clearOtherNotifications = true);

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowMessage(string)"/>
        /// </summary>
        void ShowMessage(string message);

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowWarning(string)"/>
        /// </summary>
        void ShowWarning(string warningMessage);

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ClearNotifications"/>
        /// </summary>
        void ClearNotifications();

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.HideNotification(Guid)"/>
        /// </summary>
        bool HideNotification(Guid id);

        void ShowNotificationError(string message, Guid notificationId, ICommand associatedCommand);

        void ShowNotificationWarning(string message, Guid notificationId, ICommand associatedCommand);
    }
}

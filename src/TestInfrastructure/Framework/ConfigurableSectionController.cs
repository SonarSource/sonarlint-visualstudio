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

using System.Windows.Input;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSectionController : ISectionController
    {
        #region ISectionController

        public ICommand BindCommand
        {
            get;
            set;
        }

        public ICommand ConnectCommand
        {
            get;
            set;
        }

        public ICommand DisconnectCommand
        {
            get;
            set;
        }

        public IProgressControlHost ProgressHost
        {
            get;
            set;
        }

        public ICommand RefreshCommand
        {
            get;
            set;
        }

        public ICommand ToggleShowAllProjectsCommand
        {
            get;
            set;
        }

        public IUserNotification UserNotifications
        {
            get;
            set;
        }

        public ConnectSectionViewModel ViewModel
        {
            get;
            set;
        }

        public ConnectSectionView View
        {
            get;
            set;
        }

        public ICommand BrowseToUrlCommand
        {
            get;
            set;
        }

        public ICommand BrowseToProjectDashboardCommand
        {
            get;
            set;
        }

        #endregion ISectionController

        #region Test helpers

        public static ConfigurableSectionController CreateDefault()
        {
            var section = new ConfigurableSectionController();
            section.ViewModel = new ConnectSectionViewModel();
            section.View = new ConnectSectionView();
            section.ProgressHost = new ConfigurableProgressControlHost();
            section.UserNotifications = new ConfigurableUserNotification();
            section.BindCommand = new RelayCommand(() => { });
            section.ConnectCommand = new RelayCommand(() => { });
            section.DisconnectCommand = new RelayCommand(() => { });
            section.RefreshCommand = new RelayCommand(() => { });
            section.BrowseToUrlCommand = new RelayCommand(() => { });
            section.BrowseToProjectDashboardCommand = new RelayCommand(() => { });
            section.ToggleShowAllProjectsCommand = new RelayCommand(() => { });
            return section;
        }

        #endregion Test helpers
    }
}
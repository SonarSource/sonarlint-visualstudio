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

using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    internal class ServerViewModel : ViewModelBase
    {
        private readonly ConnectionInformation connectionInformation;
        private readonly ObservableCollection<ProjectViewModel> projects = new ObservableCollection<ProjectViewModel>();
        private readonly ContextualCommandsCollection commands = new ContextualCommandsCollection();
        private bool showAllProjects;
        private bool isExpanded;

        public ServerViewModel(ConnectionInformation connectionInformation, bool isExpanded = true)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            this.connectionInformation = connectionInformation;
            this.IsExpanded = isExpanded;
        }

        /// <summary>
        /// Will clear any existing project view models and will replace them with the specified ones.
        /// The project view models will be alphabetically sorted by <see cref="ProjectInformation.Name"/> for the <see cref="StringComparer.CurrentCulture"/>
        /// </summary>
        public void SetProjects(IEnumerable<ProjectInformation> projectsToSet)
        {
            this.Projects.Clear();
            if (projectsToSet == null)
            {
                return; // all done
            }

            IEnumerable<ProjectViewModel> projectViewModels = projectsToSet
                .OrderBy(p => p.Name, StringComparer.CurrentCulture)
                .Select(project => new ProjectViewModel(this, project));

            foreach (var projectVM in projectViewModels)
            {
                this.Projects.Add(projectVM);
            }

            this.ShowAllProjects = true;
        }

        #region Properties

        public ConnectionInformation ConnectionInformation
        {
            get { return this.connectionInformation; }
        }

        public bool ShowAllProjects
        {
            get { return this.showAllProjects; }
            set { this.SetAndRaisePropertyChanged(ref this.showAllProjects, value); }
        }

        public Uri Url
        {
            get { return this.connectionInformation.ServerUri; }
        }

        public ObservableCollection<ProjectViewModel> Projects
        {
            get { return this.projects; }
        }

        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set { SetAndRaisePropertyChanged(ref this.isExpanded, value); }
        }

        public string AutomationName
        {
            get
            {
                return this.Projects.Any()
                    ? string.Format(CultureInfo.CurrentCulture, Strings.AutomationServerDescription, this.Url)
                    : string.Format(CultureInfo.CurrentCulture, Strings.AutomationServerNoProjectsDescription, this.Url);
            }
        }

        #endregion

        #region Commands

        public ContextualCommandsCollection Commands
        {
            get { return this.commands; }
        }

        #endregion
    }
}

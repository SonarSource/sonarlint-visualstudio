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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

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
        /// The project view models will be alphabetically sorted by <see cref="SonarQubeProject.Name"/> for the <see cref="StringComparer.CurrentCulture"/>
        /// </summary>
        public void SetProjects(IEnumerable<SonarQubeProject> projectsToSet)
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

        public string OrganizationName => this.connectionInformation.Organization?.Name;

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

//-----------------------------------------------------------------------
// <copyright file="ServerViewModel.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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

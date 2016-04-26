//-----------------------------------------------------------------------
// <copyright file="ProjectViewModel.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    internal class ProjectViewModel : ViewModelBase
    {
        private readonly ContextualCommandsCollection commands = new ContextualCommandsCollection();
        private bool isBound;

        public ProjectViewModel(ServerViewModel owner, ProjectInformation projectInformation)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (projectInformation == null)
            {
                throw new ArgumentNullException(nameof(projectInformation));
            }

            this.Owner = owner;
            this.ProjectInformation = projectInformation;
        }

        #region Properties
        public ServerViewModel Owner
        {
            get;
        }

        public ProjectInformation ProjectInformation
        {
            get; 
        }

        public string Key
        {
            get { return this.ProjectInformation.Key; }
        }

        public string ProjectName
        {
            get { return this.ProjectInformation.Name; }
        }

        public bool IsBound
        {
            get { return this.isBound; }
            set { this.SetAndRaisePropertyChanged(ref this.isBound, value); }
        }

        public string ToolTipProjectName
        {
            get
            {
                return this.IsBound
                    ? string.Format(CultureInfo.CurrentCulture, Strings.ProjectToolTipProjectNameFormat, this.ProjectName)
                    : this.ProjectName;
            }
        }

        public string ToolTipKey
        {
            get
            {
                return string.Format(CultureInfo.CurrentCulture, Strings.ProjectToolTipKeyFormat, this.Key);
            }
        }

        public string AutomationName
        {
            get
            {
                return this.IsBound
                    ? string.Format(CultureInfo.CurrentCulture, Strings.AutomationProjectBoundDescription, this.ProjectName)
                    : this.ProjectName;
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

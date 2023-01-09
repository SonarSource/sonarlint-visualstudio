﻿/*
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
using System.Globalization;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    public class ProjectViewModel : ViewModelBase
    {
        private readonly ContextualCommandsCollection commands = new ContextualCommandsCollection();
        private bool isBound;

        public ProjectViewModel(ServerViewModel owner, SonarQubeProject projectInformation)
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
            this.Project = projectInformation;
        }

        #region Properties
        public ServerViewModel Owner
        {
            get;
        }

        public SonarQubeProject Project
        {
            get;
        }

        public string Key
        {
            get { return this.Project.Key; }
        }

        public string ProjectName
        {
            get { return this.Project.Name; }
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

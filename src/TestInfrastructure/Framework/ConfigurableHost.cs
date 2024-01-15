/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using FluentAssertions;
using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client;

namespace SonarLint.VisualStudio.TestInfrastructure
{
    internal class ConfigurableHost : IHost
    {
        public ConfigurableHost()
        {
            this.VisualStateManager = new ConfigurableStateManager { Host = this };
            Logger = new TestLogger();
        }

        #region IHost

        public event EventHandler ActiveSectionChanged;

        public SharedBindingConfigModel SharedBindingConfig { get; set; }

        public Credential GetCredentialsForSharedConfig()
        {
            return CredentialsForSharedConfig;
        }

        public ISectionController ActiveSection
        {
            get;
            private set;
        }

        public ISonarQubeService SonarQubeService
        {
            get;
            set;
        }

        public IStateManager VisualStateManager
        {
            get;
            set;
        }

        public ILogger Logger { get; set; }

        public void ClearActiveSection()
        {
            this.ActiveSection = null;

            // Simulate product code
            this.VisualStateManager.SyncCommandFromActiveSection();
        }

        public void SetActiveSection(ISectionController section)
        {
            section.Should().NotBeNull();

            this.ActiveSection = section;

            // Simulate product code
            this.VisualStateManager.SyncCommandFromActiveSection();
        }

        #endregion IHost

        #region Test helpers

        public Credential CredentialsForSharedConfig { get; set; }
        
        public void SimulateActiveSectionChanged()
        {
            this.ActiveSectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public ConfigurableStateManager TestStateManager
        {
            get { return (ConfigurableStateManager)this.VisualStateManager; }
        }

        #endregion Test helpers
    }
}

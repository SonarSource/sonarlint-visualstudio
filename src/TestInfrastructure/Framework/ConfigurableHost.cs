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
using System.Linq;
using System.Windows.Threading;
using FluentAssertions;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableHost : IHost
    {
        private readonly ConfigurableServiceProvider serviceProvider;

        public ConfigurableHost()
            : this(new ConfigurableServiceProvider(), Dispatcher.CurrentDispatcher)
        {
        }

        public ConfigurableHost(ConfigurableServiceProvider sp, Dispatcher dispatcher)
        {
            this.serviceProvider = sp;
            this.UIDispatcher = dispatcher;
            this.VisualStateManager = new ConfigurableStateManager { Host = this };
        }

        #region IHost

        public event EventHandler ActiveSectionChanged;

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

        public Dispatcher UIDispatcher
        {
            get;
            private set;
        }

        public IStateManager VisualStateManager
        {
            get;
            set;
        }

        public void ClearActiveSection()
        {
            this.ActiveSection = null;

            // Simulate product code
            this.VisualStateManager.SyncCommandFromActiveSection();
        }

        public object GetService(Type serviceType)
        {
            if (typeof(ILocalService).IsAssignableFrom(serviceType))
            {
                VsSessionHost.SupportedLocalServices.Contains(serviceType).Should().BeTrue("The specified service type '{0}' will not be serviced in the real IHost.", serviceType.FullName);
            }

            return this.serviceProvider.GetService(serviceType);
        }

        public void SetActiveSection(ISectionController section)
        {
            section.Should().NotBeNull();

            this.ActiveSection = section;

            // Simulate product code
            this.VisualStateManager.SyncCommandFromActiveSection();
        }

        public ISet<Language> SupportedPluginLanguages { get; } = new HashSet<Language>();

        #endregion IHost

        #region Test helpers

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
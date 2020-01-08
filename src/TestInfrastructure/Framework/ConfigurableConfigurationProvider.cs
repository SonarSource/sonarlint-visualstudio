/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Configurable service provider used for testing
    /// </summary>
    internal class ConfigurableConfigurationProvider : IConfigurationProvider
    {
        public BindingConfiguration GetConfiguration()
        {
            GetConfigurationAction?.Invoke();
            
            if (ModeToReturn == SonarLintMode.Standalone)
            {
                return BindingConfiguration.Standalone;
            }
            return BindingConfiguration.CreateBoundConfiguration(ProjectToReturn, SonarLintMode.LegacyConnected == ModeToReturn);
        }

        public bool WriteConfiguration(BindingConfiguration configuration)
        {
            configuration.Should().NotBeNull();
            this.SavedConfiguration = configuration;
            return this.WriteResultToReturn;
        }

        #region Test helpers

        public BoundSonarQubeProject ProjectToReturn { get; set; }
        public SonarLintMode ModeToReturn { get; set; }
        public Action GetConfigurationAction { get; set; }

        public bool WriteResultToReturn { get; set; }
        public BindingConfiguration SavedConfiguration { get; set; }

        #endregion Test helpers

    }
}
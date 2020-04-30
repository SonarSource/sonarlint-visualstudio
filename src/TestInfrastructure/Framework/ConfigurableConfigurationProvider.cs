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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Configurable service provider used for testing
    /// </summary>
    internal class ConfigurableConfigurationProvider : IConfigurationProvider, IConfigurationPersister
    {
        public BindingConfiguration GetConfiguration()
        {
            GetConfigurationAction?.Invoke();

            return ModeToReturn == SonarLintMode.Standalone
                ? BindingConfiguration.Standalone
                : BindingConfiguration.CreateBoundConfiguration(ProjectToReturn, ModeToReturn);
        }

        public bool Persist(BoundSonarQubeProject project, SonarLintMode bindingMode)
        {
            project.Should().NotBeNull();
            SavedProject = project;
            SavedMode = bindingMode;
            return WriteResultToReturn;
        }

        #region Test helpers

        public BoundSonarQubeProject ProjectToReturn { get; set; }
        public SonarLintMode ModeToReturn { get; set; }
        public Action GetConfigurationAction { get; set; }

        public bool WriteResultToReturn { get; set; }
        public BoundSonarQubeProject SavedProject { get; set; }
        public SonarLintMode SavedMode { get; set; }

        #endregion Test helpers
    }
}

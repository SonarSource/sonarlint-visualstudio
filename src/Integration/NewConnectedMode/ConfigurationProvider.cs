/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    internal class ConfigurationProvider : IConfigurationProvider
    {
        private readonly ISolutionBindingSerializer legacySerializer;
        private readonly ISolutionBindingSerializer newConnectedModeSerializer;

        public ConfigurationProvider(ISolutionBindingSerializer legacySerializer, ISolutionBindingSerializer newConnectedModeSerializer)
        {
            if (legacySerializer == null)
            {
                throw new ArgumentNullException(nameof(legacySerializer));
            }
            if (newConnectedModeSerializer == null)
            {
                throw new ArgumentNullException(nameof(newConnectedModeSerializer));
            }
            this.legacySerializer = legacySerializer;
            this.newConnectedModeSerializer = newConnectedModeSerializer;
        }

        public BindingConfiguration GetConfiguration()
        {
            var project = legacySerializer.ReadSolutionBinding();
            if (project != null)
            {
                return BindingConfiguration.CreateBoundConfiguration(project, isLegacy: true);
            }

            project = newConnectedModeSerializer.ReadSolutionBinding();
            if (project != null)
            {
                return BindingConfiguration.CreateBoundConfiguration(project, isLegacy: false);
            }

            return BindingConfiguration.Standalone;
        }

        public bool WriteConfiguration(BindingConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            string fileName = null;
            switch (configuration.Mode)
            {
                case SonarLintMode.LegacyConnected:
                    return legacySerializer.WriteSolutionBinding(configuration.Project) != null;
                case SonarLintMode.Connected:
                    return newConnectedModeSerializer.WriteSolutionBinding(configuration.Project) != null;
                case SonarLintMode.Standalone:
                    throw new InvalidOperationException(Strings.Bind_CannotSaveStandaloneConfiguration);
                default:
                    Debug.Fail("Unrecognised write mode");
                    return false;
            }
        }
    }
}
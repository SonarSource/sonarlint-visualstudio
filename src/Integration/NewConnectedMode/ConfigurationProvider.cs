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

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    internal class ConfigurationProvider : IConfigurationProvider
    {
        private readonly ISolutionBindingSerializer legacySerializer;
        private readonly IConfigurationProvider wrappedConfigProvider;

        public ConfigurationProvider(ISolutionBindingSerializer legacySerializer, IConfigurationProvider wrappedConfigProvider)
        {
            if (legacySerializer == null)
            {
                throw new ArgumentNullException(nameof(legacySerializer));
            }
            if (wrappedConfigProvider == null)
            {
                throw new ArgumentNullException(nameof(wrappedConfigProvider));
            }
            this.legacySerializer = legacySerializer;
            this.wrappedConfigProvider = wrappedConfigProvider;
        }

        public BindingConfiguration GetConfiguration()
        {
            var project = legacySerializer.ReadSolutionBinding();
            if (project != null)
            {
                return BindingConfiguration.CreateBoundConfiguration(project, isLegacy: true);
            }

            return wrappedConfigProvider.GetConfiguration();
        }

        public bool WriteConfiguration(BindingConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            switch (configuration.Mode)
            {
                case SonarLintMode.LegacyConnected:
                    return legacySerializer.WriteSolutionBinding(configuration.Project) != null;
                case SonarLintMode.Connected:
                case SonarLintMode.Standalone:
                    return wrappedConfigProvider.WriteConfiguration(configuration);
                default:
                    Debug.Fail("Unrecognised write mode");
                    return false;
            }
        }

        public void DeleteConfiguration()
        {
            var mode = this.GetConfiguration().Mode;
            Debug.Assert(mode == SonarLintMode.Connected,
                "Can only delete a configuration when in new connected mode");

            if (mode != SonarLintMode.LegacyConnected)
            {
                wrappedConfigProvider.DeleteConfiguration();
            }
        }
    }
}
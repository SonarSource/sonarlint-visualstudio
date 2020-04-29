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
using System.Diagnostics;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    internal class ConfigurationProvider : IConfigurationProvider
    {
        private readonly ISolutionBindingPathProvider legacyPathProvider;
        private readonly ISolutionBindingPathProvider connectedModePathProvider;
        private readonly ISolutionBindingSerializer solutionBindingSerializer;
        private readonly ILegacySonarQubeFolderModifier legacySonarQubeFolderModifier;

        public ConfigurationProvider(ISolutionBindingPathProvider legacyPathProvider,
            ISolutionBindingPathProvider connectedModePathProvider,
            ISolutionBindingSerializer solutionBindingSerializer,
            ILegacySonarQubeFolderModifier legacySonarQubeFolderModifier)
        {
            this.legacyPathProvider = legacyPathProvider ??
                                      throw new ArgumentNullException(nameof(legacyPathProvider));

            this.connectedModePathProvider = connectedModePathProvider ??
                                             throw new ArgumentNullException(nameof(connectedModePathProvider));

            this.solutionBindingSerializer = solutionBindingSerializer ??
                                             throw new ArgumentNullException(nameof(solutionBindingSerializer));

            this.legacySonarQubeFolderModifier = legacySonarQubeFolderModifier ??
                                           throw new ArgumentNullException(nameof(legacySonarQubeFolderModifier));
        }

        public BindingConfiguration GetConfiguration()
        {
            var bindingConfiguration =
                TryGetBindingConfiguration(legacyPathProvider.Get(), SonarLintMode.LegacyConnected) ??
                TryGetBindingConfiguration(connectedModePathProvider.Get(), SonarLintMode.Connected);

            return bindingConfiguration ?? BindingConfiguration.Standalone;
        }

        public bool WriteConfiguration(BindingConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var writeSettings = GetWriteSettings(configuration);

            return writeSettings.HasValue && 
                   solutionBindingSerializer.Write(writeSettings?.ConfigPath, configuration.Project, writeSettings?.OnSuccessfulFileWrite);
        }

        private BindingConfiguration TryGetBindingConfiguration(string bindingPath, SonarLintMode sonarLintMode)
        {
            if (bindingPath == null)
            {
                return null;
            }

            var boundProject = solutionBindingSerializer.Read(bindingPath);

            return boundProject == null
                ? null
                : BindingConfiguration.CreateBoundConfiguration(boundProject, sonarLintMode);
        }

        private WriteSettings? GetWriteSettings(BindingConfiguration configuration)
        {
            var writeSettings = new WriteSettings();

            switch (configuration.Mode)
            {
                case SonarLintMode.LegacyConnected:
                {
                    writeSettings.ConfigPath = legacyPathProvider.Get();
                    writeSettings.OnSuccessfulFileWrite = legacySonarQubeFolderModifier.AddToFolder;
                    break;
                }
                case SonarLintMode.Connected:
                {
                    writeSettings.ConfigPath = connectedModePathProvider.Get();
                    break;
                }
                case SonarLintMode.Standalone:
                {
                    throw new InvalidOperationException(Strings.Bind_CannotSaveStandaloneConfiguration);
                }
                default:
                {
                    Debug.Fail("Unrecognized write mode " + configuration.Mode);
                    return null;
                }
            }

            return writeSettings;
        }

        private struct WriteSettings
        {
            public Action<string> OnSuccessfulFileWrite { get; set; }
            public string ConfigPath { get; set; }
        }
    }
}

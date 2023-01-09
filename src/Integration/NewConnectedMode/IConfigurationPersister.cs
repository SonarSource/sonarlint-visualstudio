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
using System.Diagnostics;
using System.IO;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    public interface IConfigurationPersister : ILocalService
    {
        BindingConfiguration Persist(BoundSonarQubeProject project, SonarLintMode bindingMode);
    }

    internal class ConfigurationPersister : IConfigurationPersister
    {
        private readonly ISolutionBindingPathProvider legacyPathProvider;
        private readonly ISolutionBindingPathProvider connectedModePathProvider;
        private readonly ISolutionBindingDataWriter solutionBindingDataWriter;
        private readonly ILegacyConfigFolderItemAdder legacyConfigFolderItemAdder;

        public ConfigurationPersister(ISolutionBindingPathProvider legacyPathProvider,
            ISolutionBindingPathProvider connectedModePathProvider,
            ISolutionBindingDataWriter solutionBindingDataWriter,
            ILegacyConfigFolderItemAdder legacyConfigFolderItemAdder)
        {
            this.legacyPathProvider = legacyPathProvider ??
                                      throw new ArgumentNullException(nameof(legacyPathProvider));

            this.connectedModePathProvider = connectedModePathProvider ??
                                             throw new ArgumentNullException(nameof(connectedModePathProvider));

            this.solutionBindingDataWriter = solutionBindingDataWriter ??
                                             throw new ArgumentNullException(nameof(solutionBindingDataWriter));

            this.legacyConfigFolderItemAdder = legacyConfigFolderItemAdder ??
                                               throw new ArgumentNullException(nameof(legacyConfigFolderItemAdder));
        }

        public BindingConfiguration Persist(BoundSonarQubeProject project, SonarLintMode bindingMode)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var writeSettings = GetWriteSettings(bindingMode);

            var success = writeSettings.HasValue &&
                          solutionBindingDataWriter.Write(writeSettings.Value.ConfigPath, project, writeSettings?.OnSuccessfulFileWrite);

            return success ? CreateBindingConfiguration(writeSettings.Value.ConfigPath, project, bindingMode) : null;
        }

        private BindingConfiguration CreateBindingConfiguration(string bindingPath, BoundSonarQubeProject boundProject, SonarLintMode sonarLintMode)
        {
            var bindingConfigDirectory = Path.GetDirectoryName(bindingPath);

            return BindingConfiguration.CreateBoundConfiguration(boundProject, sonarLintMode, bindingConfigDirectory);
        }

        private WriteSettings? GetWriteSettings(SonarLintMode bindingMode)
        {
            var writeSettings = new WriteSettings();

            switch (bindingMode)
            {
                case SonarLintMode.LegacyConnected:
                {
                    writeSettings.ConfigPath = legacyPathProvider.Get();
                    writeSettings.OnSuccessfulFileWrite = legacyConfigFolderItemAdder.AddToFolder;
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
                    Debug.Fail("Unrecognized write mode " + bindingMode);
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

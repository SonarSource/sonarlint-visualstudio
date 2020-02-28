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
        private readonly ISolutionBindingFile solutionBindingFile;
        private readonly ISolutionBindingPostSaveOperation legacyPostSaveOperation;

        public ConfigurationProvider(ISolutionBindingPathProvider legacyPathProvider,
            ISolutionBindingPathProvider connectedModePathProvider,
            ISolutionBindingFile solutionBindingFile,
            ISolutionBindingPostSaveOperation legacyPostSaveOperation)
        {
            this.legacyPathProvider = legacyPathProvider ??
                                      throw new ArgumentNullException(nameof(legacyPathProvider));

            this.connectedModePathProvider = connectedModePathProvider ??
                                             throw new ArgumentNullException(nameof(connectedModePathProvider));

            this.solutionBindingFile = solutionBindingFile ??
                                             throw new ArgumentNullException(nameof(solutionBindingFile));

            this.legacyPostSaveOperation = legacyPostSaveOperation ??
                                           throw new ArgumentNullException(nameof(legacyPostSaveOperation));
        }

        public BindingConfiguration GetConfiguration()
        {
            var project = GetBoundProject(out var sonarLintMode, out _);

            return project == null
                ? BindingConfiguration.Standalone
                : BindingConfiguration.CreateBoundConfiguration(project, sonarLintMode);
        }

        private BoundSonarQubeProject GetBoundProject(out SonarLintMode sonarLintMode, out string bindingPath)
        {
            bindingPath = legacyPathProvider.Get();
            BoundSonarQubeProject project;

            if (bindingPath != null)
            {
                project = solutionBindingFile.ReadSolutionBinding(bindingPath);

                if (project != null)
                {
                    sonarLintMode = SonarLintMode.LegacyConnected;
                    return project;
                }
            }

            bindingPath = connectedModePathProvider.Get();

            if (bindingPath != null)
            {
                project = solutionBindingFile.ReadSolutionBinding(bindingPath);

                if (project != null)
                {
                    sonarLintMode = SonarLintMode.Connected;
                    return project;
                }
            }

            sonarLintMode = SonarLintMode.LegacyConnected;
            bindingPath = null;

            return null;
        }

        public bool WriteConfiguration(BindingConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var onSuccessfulFileWrite = GetOnSuccessfulFileWriteOperation(configuration);
            
            if (onSuccessfulFileWrite == null)
            {
                return false;
            }

            GetBoundProject(out _, out var bindingPath);

            return solutionBindingFile.WriteSolutionBinding(bindingPath, configuration.Project, onSuccessfulFileWrite);
        }

        private Predicate<string> GetOnSuccessfulFileWriteOperation(BindingConfiguration configuration)
        {
            Predicate<string> onSuccessfulFileWrite;

            switch (configuration.Mode)
            {
                case SonarLintMode.LegacyConnected:
                {
                    onSuccessfulFileWrite = legacyPostSaveOperation.OnSuccessfulSave;
                    break;
                }
                case SonarLintMode.Connected:
                {
                    onSuccessfulFileWrite = s => true;
                    break;
                }
                case SonarLintMode.Standalone:
                {
                    throw new InvalidOperationException(Strings.Bind_CannotSaveStandaloneConfiguration);
                }
                default:
                {
                    Debug.Fail("Unrecognised write mode " + configuration.Mode);
                    return null;
                }
            }

            return onSuccessfulFileWrite;
        }
    }
}

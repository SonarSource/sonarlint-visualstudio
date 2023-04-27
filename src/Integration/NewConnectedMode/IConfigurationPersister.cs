/*
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
        private readonly ISolutionBindingPathProvider connectedModePathProvider;
        private readonly ISolutionBindingDataWriter solutionBindingDataWriter;

        public ConfigurationPersister(ISolutionBindingPathProvider connectedModePathProvider,
            ISolutionBindingDataWriter solutionBindingDataWriter)
        {
            this.connectedModePathProvider = connectedModePathProvider ??
                                             throw new ArgumentNullException(nameof(connectedModePathProvider));

            this.solutionBindingDataWriter = solutionBindingDataWriter ??
                                             throw new ArgumentNullException(nameof(solutionBindingDataWriter));
        }

        public BindingConfiguration Persist(BoundSonarQubeProject project, SonarLintMode bindingMode)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var targetDirectory = GetTargetDirectory(bindingMode);

            var success = targetDirectory != null &&
                          solutionBindingDataWriter.Write(targetDirectory, project);

            return success ? CreateBindingConfiguration(targetDirectory, project, bindingMode) : null;
        }

        private BindingConfiguration CreateBindingConfiguration(string bindingPath, BoundSonarQubeProject boundProject, SonarLintMode sonarLintMode)
        {
            var bindingConfigDirectory = Path.GetDirectoryName(bindingPath);

            return BindingConfiguration.CreateBoundConfiguration(boundProject, sonarLintMode, bindingConfigDirectory);
        }

        private string GetTargetDirectory(SonarLintMode bindingMode)
        {
            switch (bindingMode)
            {
                // TODO - CM cleanup - going forwards we'll only support saving in the new-new format.
                case SonarLintMode.LegacyConnected:
                {
                    return null;  // effect is that settings won't be saved
                }
                case SonarLintMode.Connected:
                {
                    return connectedModePathProvider.Get();
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
        }
    }
}

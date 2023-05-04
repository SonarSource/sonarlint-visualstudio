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
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    /// <summary>
    /// Service to return the configuration for "old" Connected Mode settings i.e. pre-unintrusive Connected Mode
    /// </summary>
    /// <remarks>This service is only used by the settings migration and cleanup components to help users migrate
    /// to the new Connected Mode settings format and locations. It and all of the implementing classes can be
    /// dropped at some point in the future i.e. once we think users have had enough opportunity to move to the
    /// new format.
    /// See https://github.com/SonarSource/sonarlint-visualstudio/issues/4171
    /// </remarks>
    internal interface IObsoleteConfigurationProviderService : IConfigurationProvider, ILocalService
    {
    }

    [Export(typeof(IObsoleteConfigurationProviderService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ObsoleteConfigurationProvider : IObsoleteConfigurationProviderService
    {
        private readonly ISolutionBindingPathProvider legacyPathProvider;
        private readonly ISolutionBindingPathProvider connectedModePathProvider;
        private readonly ISolutionBindingDataReader solutionBindingDataReader;

        [ImportingConstructor]
        public ObsoleteConfigurationProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ISolutionBindingDataReader solutionBindingDataReader)
            : this(
                new LegacySolutionBindingPathProvider(serviceProvider),
                new ObsoleteConnectedModeSolutionBindingPathProvider(serviceProvider),
                solutionBindingDataReader)
        {
        }

        internal ObsoleteConfigurationProvider(ISolutionBindingPathProvider legacyPathProvider,
            ISolutionBindingPathProvider connectedModePathProvider,
            ISolutionBindingDataReader solutionBindingDataReader)
        {
            this.legacyPathProvider = legacyPathProvider ??
                                      throw new ArgumentNullException(nameof(legacyPathProvider));

            this.connectedModePathProvider = connectedModePathProvider ??
                                             throw new ArgumentNullException(nameof(connectedModePathProvider));

            this.solutionBindingDataReader = solutionBindingDataReader ??
                                             throw new ArgumentNullException(nameof(solutionBindingDataReader));
        }

        public BindingConfiguration GetConfiguration()
        {
            var bindingConfiguration =
                TryGetBindingConfiguration(legacyPathProvider.Get(), SonarLintMode.LegacyConnected) ??
                TryGetBindingConfiguration(connectedModePathProvider.Get(), SonarLintMode.Connected);

            return bindingConfiguration ?? BindingConfiguration.Standalone;
        }

        private BindingConfiguration TryGetBindingConfiguration(string bindingPath, SonarLintMode sonarLintMode)
        {
            if (bindingPath == null)
            {
                return null;
            }

            var boundProject = solutionBindingDataReader.Read(bindingPath);

            if (boundProject == null)
            {
                return null;
            }

            var bindingConfigDirectory = Path.GetDirectoryName(bindingPath);

            return BindingConfiguration.CreateBoundConfiguration(boundProject, sonarLintMode, bindingConfigDirectory);
        }
    }
}

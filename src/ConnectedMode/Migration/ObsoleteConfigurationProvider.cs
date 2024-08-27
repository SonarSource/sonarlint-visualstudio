/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IObsoleteConfigurationProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ObsoleteConfigurationProvider : IObsoleteConfigurationProvider
    {
        private readonly ISolutionBindingPathProvider legacyPathProvider;
        private readonly ISolutionBindingPathProvider connectedModePathProvider;
        private readonly ISolutionBindingRepository solutionBindingRepository;

        [ImportingConstructor]
        public ObsoleteConfigurationProvider(
            ISolutionInfoProvider solutionInfoProvider,
            ISolutionBindingRepository solutionBindingRepository)
            : this(
                new LegacySolutionBindingPathProvider(solutionInfoProvider),
                new ObsoleteConnectedModeSolutionBindingPathProvider(solutionInfoProvider),
                solutionBindingRepository)
        {
        }

        internal /* for testing */ ObsoleteConfigurationProvider(ISolutionBindingPathProvider legacyPathProvider,
            ISolutionBindingPathProvider connectedModePathProvider,
            ISolutionBindingRepository solutionBindingRepository)
        {
            this.legacyPathProvider = legacyPathProvider ?? throw new ArgumentNullException(nameof(legacyPathProvider));
            this.connectedModePathProvider = connectedModePathProvider ?? throw new ArgumentNullException(nameof(connectedModePathProvider));
            this.solutionBindingRepository = solutionBindingRepository ?? throw new ArgumentNullException(nameof(solutionBindingRepository));
        }

        public LegacyBindingConfiguration GetConfiguration()
        {
            var bindingConfiguration =
                TryGetBindingConfiguration(legacyPathProvider.Get(), SonarLintMode.LegacyConnected) ??
                TryGetBindingConfiguration(connectedModePathProvider.Get(), SonarLintMode.Connected);

            return bindingConfiguration ?? LegacyBindingConfiguration.Standalone;
        }

        private LegacyBindingConfiguration TryGetBindingConfiguration(string bindingPath, SonarLintMode sonarLintMode)
        {
            if (bindingPath == null)
            {
                return null;
            }

            var boundProject = solutionBindingRepository.Read(bindingPath);

            if (boundProject == null)
            {
                return null;
            }

            var bindingConfigDirectory = Path.GetDirectoryName(bindingPath);

            return LegacyBindingConfiguration.CreateBoundConfiguration(boundProject, sonarLintMode, bindingConfigDirectory);
        }
    }
}

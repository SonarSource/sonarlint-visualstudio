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
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{

    // TODO: remove this class
    // It's a temporary measure to keep the existing code working while
    // refactoring/adding the new lightweight connected mode

    internal class LegacyConfigurationProviderAdapter : IConfigurationProvider
    {
        private readonly ISolutionBindingSerializer legacySerializer;

        public LegacyConfigurationProviderAdapter(ISolutionBindingSerializer legacySerializer)
        {
            if (legacySerializer == null)
            {
                throw new ArgumentNullException(nameof(legacySerializer));
            }
            this.legacySerializer = legacySerializer;
        }

        public BindingConfiguration GetConfiguration()
        {
            //TODO: support new connected mode
            var project = legacySerializer.ReadSolutionBinding();
            if (project == null)
            {
                return BindingConfiguration.Standalone;
            }
            return BindingConfiguration.CreateBoundConfiguration(project, isLegacy: true);
        }

        /// <summary>
        /// Returns the currently bound project, or null if the current solution is
        /// not bound, or if there is not a current solution
        /// </summary>
        public BoundSonarQubeProject GetBoundProject()
        {
            return legacySerializer.ReadSolutionBinding();
        }

        public SonarLintMode GetMode()
        {
            //TODO: support new connected mode
            return GetBoundProject() != null ? SonarLintMode.LegacyConnected : SonarLintMode.Standalone;
        }
    }
}
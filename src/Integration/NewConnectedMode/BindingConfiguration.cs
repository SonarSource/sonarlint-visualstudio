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
    public class BindingConfiguration
    {
        public readonly static BindingConfiguration Standalone = new BindingConfiguration(null, SonarLintMode.Standalone);

        public static BindingConfiguration CreateBoundConfiguration(BoundSonarQubeProject project, bool isLegacy)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
            return new BindingConfiguration(project, isLegacy ? SonarLintMode.LegacyConnected : SonarLintMode.Connected);
        }

        internal /*for testing purposes*/ BindingConfiguration(BoundSonarQubeProject project, SonarLintMode mode)
        {
            this.Project = project;
            this.Mode = mode;
        }

        public BoundSonarQubeProject Project { get; }

        public SonarLintMode Mode { get; }
    }
}
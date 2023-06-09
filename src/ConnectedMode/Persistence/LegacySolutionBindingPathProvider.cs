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
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.ConnectedMode.Persistence;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class LegacySolutionBindingPathProvider : ISolutionBindingPathProvider
    {
        private readonly IVsSolution vsSolution;
        public const string LegacyBindingConfigurationFileName = "SolutionBinding.sqconfig";

        public LegacySolutionBindingPathProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
        }

        public string Get()
        {
            vsSolution.GetSolutionInfo(out var solutionDirectory, out _, out _);

            // Solution closed?
            if (string.IsNullOrWhiteSpace(solutionDirectory))
            {
                return null;
            }

            return Path.Combine(solutionDirectory, PersistenceConstants.LegacySonarQubeManagedFolderName, LegacyBindingConfigurationFileName);
        }
    }
}

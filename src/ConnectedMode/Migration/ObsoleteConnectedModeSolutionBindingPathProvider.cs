﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    /// <summary>
    /// Return the path of solution's binding configuration file when in connected mode.
    /// The legacy binding is calculated using <see cref="LegacySolutionBindingPathProvider"/>.
    /// </summary>
    internal class ObsoleteConnectedModeSolutionBindingPathProvider : ISolutionBindingPathProvider
    {
        private readonly ISolutionInfoProvider solutionInfoProvider;

        public ObsoleteConnectedModeSolutionBindingPathProvider(ISolutionInfoProvider solutionInfoProvider)
        {
            if (solutionInfoProvider == null)
            {
                throw new ArgumentNullException(nameof(solutionInfoProvider));
            }

            this.solutionInfoProvider = solutionInfoProvider;
        }

        public string Get()
        {
            var fullSolutionName = solutionInfoProvider.GetFullSolutionFilePath();

            return GetConnectionFilePath(fullSolutionName);
        }

        private static string GetConnectionFilePath(string solutionFilePath)
        {
            if (solutionFilePath == null)
            {
                return null;
            }

            var solutionFolder = Path.GetDirectoryName(solutionFilePath);
            var solutionName = Path.GetFileNameWithoutExtension(solutionFilePath);

            return Path.Combine(solutionFolder, PersistenceConstants.SonarlintManagedFolderName, $"{solutionName}.slconfig");
        }
    }
}

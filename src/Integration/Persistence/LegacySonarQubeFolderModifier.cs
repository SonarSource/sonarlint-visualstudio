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
using System.IO;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class LegacySonarQubeFolderModifier : ILegacySonarQubeFolderModifier
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IFileSystem fileSystem;

        public LegacySonarQubeFolderModifier(IServiceProvider serviceProvider)
            : this(serviceProvider, new FileSystem())
        {
        }

        internal LegacySonarQubeFolderModifier(IServiceProvider serviceProvider, IFileSystem fileSystem)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public void Add(string filePath)
        {
            Debug.Assert(Path.IsPathRooted(filePath) && fileSystem.File.Exists(filePath), "Expecting a rooted path to existing file");

            var projectSystemHelper = serviceProvider.GetService<IProjectSystemHelper>();
            projectSystemHelper.AssertLocalServiceIsNotNull();

            var solutionItemsProject = projectSystemHelper.GetSolutionFolderProject(Constants.LegacySonarQubeManagedFolderName, true);

            if (solutionItemsProject == null)
            {
                Debug.Fail("Could not find the solution items project"); // Should never happen
            }
            else
            {
                projectSystemHelper.AddFileToProject(solutionItemsProject, filePath);
            }
        }
    }
}

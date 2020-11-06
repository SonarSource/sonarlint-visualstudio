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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    [Export(typeof(IAbsoluteFilePathLocator))]
    internal class AbsoluteFilePathLocator : IAbsoluteFilePathLocator
    {
        private readonly IProjectSystemHelper projectSystemHelper;

        [ImportingConstructor]
        public AbsoluteFilePathLocator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : this(new ProjectSystemHelper(serviceProvider))
        {
        }

        internal AbsoluteFilePathLocator(IProjectSystemHelper projectSystemHelper)
        {
            this.projectSystemHelper = projectSystemHelper;
        }

        public string Locate(string relativeFilePath)
        {
            if (relativeFilePath == null)
            {
                throw new ArgumentNullException(nameof(relativeFilePath));
            }

            foreach (var vsHierarchy in projectSystemHelper.EnumerateProjects())
            {
                var vsProject = vsHierarchy as IVsProject;
                var projectFilePath = projectSystemHelper.GetItemFilePath(vsProject, VSConstants.VSITEMID.Root);

                if (string.IsNullOrEmpty(projectFilePath))
                {
                    continue;
                }

                var itemIdsInProject = projectSystemHelper.GetAllItems(vsHierarchy);

                foreach (var vsItemId in itemIdsInProject)
                {
                    var absoluteItemFilePath = projectSystemHelper.GetItemFilePath(vsProject, vsItemId);

                    if (string.IsNullOrEmpty(absoluteItemFilePath))
                    {
                        continue;
                    }

                    if (absoluteItemFilePath.EndsWith(relativeFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return absoluteItemFilePath;
                    }
                }
            }

            return null;
        }
    }
}

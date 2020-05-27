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
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class CSharpVBAdditionalFileReferenceChecker : ICSharpVBAdditionalFileReferenceChecker
    {
        private readonly IProjectSystemHelper projectSystemHelper;

        public CSharpVBAdditionalFileReferenceChecker(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            projectSystemHelper = serviceProvider.GetService<IProjectSystemHelper>();
            projectSystemHelper.AssertLocalServiceIsNotNull();
        }

        public bool IsReferenced(Project project, string additionalFilePath)
        {
            var fileItem = projectSystemHelper.FindFileInProject(project, additionalFilePath);
            
            if (fileItem == null)
            {
                return false;
            }

            var property = VsShellUtils.FindProperty(fileItem.Properties, Constants.ItemTypePropertyKey);
            var isMarkedAsAdditionalFile = Constants.AdditionalFilesItemTypeName.Equals(property.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
            
            return isMarkedAsAdditionalFile;
        }
    }
}

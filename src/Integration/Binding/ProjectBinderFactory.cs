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
using System.IO.Abstractions;
using System.Linq;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class ProjectBinderFactory : IProjectBinderFactory
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IFileSystem fileSystem;

        public ProjectBinderFactory(IServiceProvider serviceProvider)
            : this(serviceProvider, new FileSystem())
        {
        }

        internal ProjectBinderFactory(IServiceProvider serviceProvider, IFileSystem fileSystem)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public IProjectBinder Get(Project project)
        {
            var languages = ProjectToLanguageMapper.GetAllBindingLanguagesForProject(project).ToList();
            var isRoslynProject = languages.Contains(Core.Language.VBNET) || languages.Contains(Core.Language.CSharp);

            return isRoslynProject ? new RoslynProjectBinder(serviceProvider, fileSystem) : null;
        }
    }
}

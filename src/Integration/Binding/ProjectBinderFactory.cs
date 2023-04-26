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
using System.IO.Abstractions;
using System.Linq;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class ProjectBinderFactory : IProjectBinderFactory
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;
        private readonly IProjectToLanguageMapper projectToLanguageMapper;

        public ProjectBinderFactory(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, logger, new FileSystem())
        {
        }

        internal ProjectBinderFactory(IServiceProvider serviceProvider, ILogger logger, IFileSystem fileSystem)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            projectToLanguageMapper = serviceProvider.GetMefService<IProjectToLanguageMapper>();
        }

        public IProjectBinder Get(Project project)
        {
            // TODO - CM cleanup - remove IProjectBinderFactory
            return null;
        }
    }
}

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using EnvDTE;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Resources;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class CFamilyProjectBinder : IProjectBinder
    {
        private readonly IProjectToLanguageMapper projectToLanguageMapper;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public CFamilyProjectBinder(IProjectToLanguageMapper projectToLanguageMapper, ILogger logger, IFileSystem fileSystem)
        {
            this.projectToLanguageMapper = projectToLanguageMapper ?? throw new ArgumentNullException(nameof(projectToLanguageMapper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public bool IsBindingRequired(BindingConfiguration binding, Project project)
        {
            Debug.Assert(binding != null);
            Debug.Assert(project != null);

            var languages = projectToLanguageMapper.GetAllBindingLanguagesForProject(project);
            languages = languages.Where(x => x.Equals(Language.C) || x.Equals(Language.Cpp));

            return languages.Any(language =>
            {
                var slnLevelBindingConfigFilepath = binding.BuildPathUnderConfigDirectory(language.FileSuffixAndExtension);

                var configFileExists = fileSystem.File.Exists(slnLevelBindingConfigFilepath);
                logger.LogDebug($"[Binding check] Does config file exists: {configFileExists} (language: '{language}', file path: '{slnLevelBindingConfigFilepath}')");
                return !configFileExists;
            });
        }

        public BindProject GetBindAction(IBindingConfig config, Project project, CancellationToken cancellationToken)
        {
            logger.WriteLine(Strings.Bind_Project_NotRequired, project.FullName);

            return () => { };
        }
    }
}

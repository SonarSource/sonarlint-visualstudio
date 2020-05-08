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
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using EnvDTE;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class CFamilyProjectBinder : IProjectBinder
    {
        private readonly ISolutionRuleSetsInformationProvider ruleSetInfoProvider;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public CFamilyProjectBinder(IServiceProvider serviceProvider, ILogger logger, IFileSystem fileSystem)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            ruleSetInfoProvider = serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();
        }

        public bool IsBound(BindingConfiguration binding, Project project)
        {
            Debug.Assert(binding != null);
            Debug.Assert(project != null);

            var languages = ProjectToLanguageMapper.GetAllBindingLanguagesForProject(project);

            return languages.All(language =>
            {
                var slnLevelBindingConfigFilepath = ruleSetInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(binding.Project.ProjectKey, language, binding.Mode);

                return fileSystem.File.Exists(slnLevelBindingConfigFilepath);
            });
        }

        public BindProject GetBindAction(IBindingConfig config, Project project, CancellationToken cancellationToken)
        {
            logger.WriteLine(Strings.Bind_Project_NotRequired, project.FullName);

            return () => { };
        }
    }
}

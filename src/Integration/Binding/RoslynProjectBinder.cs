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
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class RoslynProjectBinder : IProjectBinder
    {
        public delegate IBindingOperation CreateBindingOperationFunc(Project project, IBindingConfigFile bindingConfigFile);

        private readonly IFileSystem fileSystem;
        private readonly ISolutionRuleSetsInformationProvider ruleSetInfoProvider;
        private readonly IRuleSetSerializer ruleSetSerializer;
        private readonly CreateBindingOperationFunc createBindingOperationFunc;

        public RoslynProjectBinder(IServiceProvider serviceProvider, IFileSystem fileSystem)
            :  this(serviceProvider, fileSystem, GetCreateBindingOperationFunc(serviceProvider))
        {
        }

        internal RoslynProjectBinder(IServiceProvider serviceProvider, IFileSystem fileSystem, CreateBindingOperationFunc createBindingOperationFunc)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.createBindingOperationFunc = createBindingOperationFunc ?? throw new ArgumentNullException(nameof(createBindingOperationFunc));

            ruleSetInfoProvider = serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();

            ruleSetSerializer = serviceProvider.GetService<IRuleSetSerializer>();
            ruleSetSerializer.AssertLocalServiceIsNotNull();
        }

        private static CreateBindingOperationFunc GetCreateBindingOperationFunc(IServiceProvider serviceProvider)
        {
            return (project, configFile) => new ProjectBindingOperation(serviceProvider, project, configFile as IBindingConfigFileWithRuleset);
        }

        public BindProject GetBindAction(IBindingConfigFile configFile, Project project, CancellationToken cancellationToken)
        {
            var bindingOperation = createBindingOperationFunc(project, configFile);
            bindingOperation.Initialize();
            bindingOperation.Prepare(cancellationToken);

            return bindingOperation.Commit;
        }

        public bool IsBound(BindingConfiguration binding, Project project)
        {
            Debug.Assert(binding != null);
            Debug.Assert(project != null);

            var languages = ProjectToLanguageMapper.GetAllBindingLanguagesForProject(project);

            return languages.All(l => IsFullyBoundProject(binding, project, l));
        }

        private bool IsFullyBoundProject(BindingConfiguration binding, Project project, Core.Language language)
        {
            // If solution is not bound/is missing a rules configuration file, no need to go further
            var slnLevelBindingConfigFilepath = ruleSetInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(binding.Project.ProjectKey, language, binding.Mode);

            if (!fileSystem.File.Exists(slnLevelBindingConfigFilepath))
            {
                return false;
            }

            // Projects that required project-level binding should be using RuleSets for configuration,
            // so we assume that the solution-level config file is a ruleset.
            var sonarQubeRuleSet = ruleSetSerializer.LoadRuleSet(slnLevelBindingConfigFilepath);

            if (sonarQubeRuleSet == null)
            {
                return false;
            }

            var declarations = ruleSetInfoProvider.GetProjectRuleSetsDeclarations(project).ToArray();

            return declarations.Length > 0 // Need at least one
                   && declarations.All(declaration => IsRuleSetBound(project, declaration, sonarQubeRuleSet));
        }

        private bool IsRuleSetBound(Project project, RuleSetDeclaration declaration, RuleSet sonarQubeRuleSet)
        {
            var projectRuleSet = FindDeclarationRuleSet(project, declaration);

            return projectRuleSet != null && RuleSetIncludeChecker.HasInclude(projectRuleSet, sonarQubeRuleSet);
        }

        private RuleSet FindDeclarationRuleSet(Project project, RuleSetDeclaration declaration)
        {
            // Check if project rule set is found (we treat missing/erroneous rule set settings as not found)
            if (!ruleSetInfoProvider.TryGetProjectRuleSetFilePath(project, declaration, out var ruleSetFilePath))
            {
                return null;
            }

            return ruleSetSerializer.LoadRuleSet(ruleSetFilePath);
        }
    }
}

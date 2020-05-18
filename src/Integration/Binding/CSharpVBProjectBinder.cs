﻿/*
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
using System.Linq;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Core.Binding;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class CSharpVBProjectBinder : IProjectBinder
    {
        public delegate ICSharpVBBindingOperation CreateBindingOperationFunc(Project project, IBindingConfig bindingConfig);

        private readonly IFileSystem fileSystem;
        private readonly ISolutionRuleSetsInformationProvider ruleSetInfoProvider;
        private readonly IRuleSetSerializer ruleSetSerializer;
        private readonly CreateBindingOperationFunc createBindingOperationFunc;
        private readonly IProjectSystemHelper projectSystemHelper;

        public CSharpVBProjectBinder(IServiceProvider serviceProvider, IFileSystem fileSystem)
            :  this(serviceProvider, fileSystem, GetCreateBindingOperationFunc(serviceProvider))
        {
        }

        internal CSharpVBProjectBinder(IServiceProvider serviceProvider, IFileSystem fileSystem, CreateBindingOperationFunc createBindingOperationFunc)
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

            projectSystemHelper = serviceProvider.GetService<IProjectSystemHelper>();
            projectSystemHelper.AssertLocalServiceIsNotNull();
        }

        private static CreateBindingOperationFunc GetCreateBindingOperationFunc(IServiceProvider serviceProvider)
        {
            return (project, configFile) => new CSharpVBBindingOperation(serviceProvider, project, configFile as ICSharpVBBindingConfig);
        }

        public BindProject GetBindAction(IBindingConfig config, Project project, CancellationToken cancellationToken)
        {
            var bindingOperation = createBindingOperationFunc(project, config);
            bindingOperation.Initialize();
            bindingOperation.Prepare(cancellationToken);

            return bindingOperation.Commit;
        }

        public bool IsBindingRequired(BindingConfiguration binding, Project project)
        {
            Debug.Assert(binding != null);
            Debug.Assert(project != null);

            var languages = ProjectToLanguageMapper.GetAllBindingLanguagesForProject(project);
            languages = languages.Where(x => x.Equals(Language.VBNET) || x.Equals(Language.CSharp));

            return languages.Any(l => !IsFullyBoundProject(binding, project, l));
        }

        private bool IsFullyBoundProject(BindingConfiguration binding, Project project, Core.Language language)
        {
            if (!IsSolutionBound(binding, language, out var solutionRuleset, out var solutionAdditionalFilePath))
            {
                return false;
            }

            var isAdditionalFileBound = DoesProjectReferenceAdditionalFile(project, solutionAdditionalFilePath);

            if (!isAdditionalFileBound)
            {
                return false;
            }

            var declarations = ruleSetInfoProvider.GetProjectRuleSetsDeclarations(project).ToArray();

            var isRuleSetBound = declarations.Length > 0 && declarations.All(declaration => IsRuleSetBound(project, declaration, solutionRuleset));

            return isRuleSetBound;
        }

        private bool IsSolutionBound(BindingConfiguration binding, Language language, out RuleSet solutionRuleset, out string solutionAdditionalFilePath)
        {
            solutionRuleset = null;
            solutionAdditionalFilePath = CSharpVBBindingConfigProvider.GetSolutionAdditionalFilePath(language, binding);

            if (!fileSystem.File.Exists(solutionAdditionalFilePath))
            {
                return false;
            }

            solutionRuleset = GetSolutionRuleset(binding, language);

            return solutionRuleset != null;
        }


        private RuleSet GetSolutionRuleset(BindingConfiguration binding, Language language)
        {
            // If solution is not bound/is missing a rules configuration file, no need to go further
            var slnLevelBindingConfigFilepath = CSharpVBBindingConfigProvider.GetSolutionRuleSetFilePath(language, binding);

            // Projects that required project-level binding should be using RuleSets for configuration,
            // so we assume that the solution-level config file is a ruleset.
            return fileSystem.File.Exists(slnLevelBindingConfigFilepath)
                ? ruleSetSerializer.LoadRuleSet(slnLevelBindingConfigFilepath)
                : null;
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

        public bool DoesProjectReferenceAdditionalFile(Project project, string solutionAdditionalFilePath)
        {
            var additionalFile = projectSystemHelper.FindFileInProject(project, solutionAdditionalFilePath);

            if (additionalFile == null)
            {
                var additionalFileName = Path.GetFileName(solutionAdditionalFilePath);
                var hasAnotherAdditionalFile = projectSystemHelper.IsFileInProject(project, additionalFileName);

                if (hasAnotherAdditionalFile)
                {
                    throw new Exception($"Could not bind project {project.Name}, project already contains {additionalFileName} file. Please remove {additionalFileName} from the project.");
                }

                return false;
            }

            var property = VsShellUtils.FindProperty(additionalFile.Properties, Constants.ItemTypePropertyKey);
            var isMarkedAsAdditionalFile = "AdditionalFiles".Equals(property.Value);

            return isMarkedAsAdditionalFile;
        }
    }
}

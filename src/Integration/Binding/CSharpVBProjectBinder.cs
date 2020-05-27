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
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class CSharpVBProjectBinder : IProjectBinder
    {
        public delegate ICSharpVBBindingOperation CreateBindingOperationFunc(Project project, IBindingConfig bindingConfig);

        private readonly IFileSystem fileSystem;
        private readonly IRuleSetReferenceChecker ruleSetReferenceChecker;
        private readonly ICSharpVBAdditionalFileReferenceChecker additionalFileReferenceChecker;
        private readonly IRuleSetSerializer ruleSetSerializer;
        private readonly CreateBindingOperationFunc createBindingOperationFunc;

        public CSharpVBProjectBinder(IServiceProvider serviceProvider, IFileSystem fileSystem)
            : this(serviceProvider, fileSystem, new RuleSetReferenceChecker(serviceProvider), new CSharpVBAdditionalFileReferenceChecker(serviceProvider), GetCreateBindingOperationFunc(serviceProvider))
        {
        }

        internal CSharpVBProjectBinder(IServiceProvider serviceProvider, IFileSystem fileSystem, IRuleSetReferenceChecker ruleSetReferenceChecker, ICSharpVBAdditionalFileReferenceChecker additionalFileReferenceChecker, CreateBindingOperationFunc createBindingOperationFunc)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.ruleSetReferenceChecker = ruleSetReferenceChecker ?? throw new ArgumentNullException(nameof(ruleSetReferenceChecker));
            this.additionalFileReferenceChecker = additionalFileReferenceChecker ?? throw new ArgumentNullException(nameof(additionalFileReferenceChecker));
            this.createBindingOperationFunc = createBindingOperationFunc ?? throw new ArgumentNullException(nameof(createBindingOperationFunc));

            ruleSetSerializer = serviceProvider.GetService<IRuleSetSerializer>();
            ruleSetSerializer.AssertLocalServiceIsNotNull();
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

        private bool IsFullyBoundProject(BindingConfiguration binding, Project project, Language language)
        {
            if (!IsSolutionBound(binding, language, out var solutionRuleSet, out var additionalFilePath))
            {
                return false;
            }

            var isAdditionalFileBound = additionalFileReferenceChecker.IsReferenced(project, additionalFilePath);

            if (!isAdditionalFileBound)
            {
                return false;
            }

            var isRuleSetBound = ruleSetReferenceChecker.IsReferenced(project, solutionRuleSet);

            return isRuleSetBound;
        }

        private bool IsSolutionBound(BindingConfiguration binding, Language language, out RuleSet solutionRuleSet, out string additionalFilePath)
        {
            solutionRuleSet = null;
            additionalFilePath = CSharpVBBindingConfigProvider.GetSolutionAdditionalFilePath(language, binding);

            if (!fileSystem.File.Exists(additionalFilePath))
            {
                return false;
            }

            solutionRuleSet = GetSolutionRuleSet(binding, language);

            return solutionRuleSet != null;
        }

        private RuleSet GetSolutionRuleSet(BindingConfiguration binding, Language language)
        {
            // If solution is not bound/is missing a rules configuration file, no need to go further
            var slnLevelBindingConfigFilepath = CSharpVBBindingConfigProvider.GetSolutionRuleSetFilePath(language, binding);

            // Projects that required project-level binding should be using RuleSets for configuration,
            // so we assume that the solution-level config file is a ruleset.
            return fileSystem.File.Exists(slnLevelBindingConfigFilepath)
                ? ruleSetSerializer.LoadRuleSet(slnLevelBindingConfigFilepath)
                : null;
        }
    }
}

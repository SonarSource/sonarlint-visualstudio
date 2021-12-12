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
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Helpers;
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
        private readonly ILogger logger;
        private readonly IProjectToLanguageMapper projectToLanguageMapper;

        public CSharpVBProjectBinder(IServiceProvider serviceProvider, IProjectToLanguageMapper projectToLanguageMapper, IFileSystem fileSystem, ILogger logger)
            : this(serviceProvider, projectToLanguageMapper, fileSystem, logger,
                  new RuleSetReferenceChecker(serviceProvider, logger),
                  new CSharpVBAdditionalFileReferenceChecker(serviceProvider),
                  GetCreateBindingOperationFunc(serviceProvider, logger))
        {
        }

        internal CSharpVBProjectBinder(IServiceProvider serviceProvider,
            IProjectToLanguageMapper projectToLanguageMapper,
            IFileSystem fileSystem,
            ILogger logger,
            IRuleSetReferenceChecker ruleSetReferenceChecker,
            ICSharpVBAdditionalFileReferenceChecker additionalFileReferenceChecker,
            CreateBindingOperationFunc createBindingOperationFunc)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.projectToLanguageMapper = projectToLanguageMapper ?? throw new ArgumentNullException(nameof(projectToLanguageMapper));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.ruleSetReferenceChecker = ruleSetReferenceChecker ?? throw new ArgumentNullException(nameof(ruleSetReferenceChecker));
            this.additionalFileReferenceChecker = additionalFileReferenceChecker ?? throw new ArgumentNullException(nameof(additionalFileReferenceChecker));
            this.createBindingOperationFunc = createBindingOperationFunc ?? throw new ArgumentNullException(nameof(createBindingOperationFunc));

            ruleSetSerializer = serviceProvider.GetService<IRuleSetSerializer>();
            ruleSetSerializer.AssertLocalServiceIsNotNull();
        }

        private static CreateBindingOperationFunc GetCreateBindingOperationFunc(IServiceProvider serviceProvider, ILogger logger)
        {
            return (project, configFile) => new CSharpVBBindingOperation(serviceProvider, project, configFile as ICSharpVBBindingConfig, logger);
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
            ETW.CodeMarkers.Instance.CSharpVBProjectIsBindingRequiredStart(project.Name);

            Debug.Assert(binding != null);
            Debug.Assert(project != null);

            var languages = projectToLanguageMapper.GetAllBindingLanguagesForProject(project);
            languages = languages.Where(x => x.Equals(Language.VBNET) || x.Equals(Language.CSharp));


            var result = languages.Any(l => !IsFullyBoundProject(binding, project, l));

            ETW.CodeMarkers.Instance.CSharpVBIsBindingRequiredStop();

            return result;
        }

        private bool IsFullyBoundProject(BindingConfiguration binding, Project project, Language language)
        {
            logger.LogDebug($"[Binding check] Checking binding for project '{project.Name}', language '{language}'");

            // PERF: don't need to check the solution binding for every project
            var isSolutionBound = IsSolutionBound(binding, language, out var solutionRuleSetFilePath, out var additionalFilePath);
            logger.LogDebug($"[Binding check] Is solution bound: {isSolutionBound}");

            if (!isSolutionBound)
            {
                logger.LogDebug("[Binding check] Solution is not bound. Skipping project-level checks.");
                return false;
            }

            var isAdditionalFileBound = additionalFileReferenceChecker.IsReferenced(project, additionalFilePath);
            logger.LogDebug($"[Binding check] Is additional file referenced: {isAdditionalFileBound} (file path: '{additionalFilePath}')");

            if (!isAdditionalFileBound)
            {
                return false;
            }

            var isRuleSetBound = ruleSetReferenceChecker.IsReferencedByAllDeclarations(project, solutionRuleSetFilePath);
            logger.LogDebug($"[Binding check] Is ruleset referenced: {isRuleSetBound}.  (file path: {solutionRuleSetFilePath})");
            return isRuleSetBound;
        }

        private bool IsSolutionBound(BindingConfiguration binding, Language language, out string solutionRuleSetFilePath, out string additionalFilePath)
        {
            solutionRuleSetFilePath = null;
            additionalFilePath = CSharpVBBindingConfigProvider.GetSolutionAdditionalFilePath(language, binding);

            var additionalFileExists = fileSystem.File.Exists(additionalFilePath);
            logger.LogDebug($"[Binding check] Solution-level additional file exists: {additionalFileExists} (file path: '{additionalFilePath}')");
            if (!additionalFileExists)
            {
                return false;
            }

            var solutionRuleSet = GetSolutionRuleSet(binding, language);
            solutionRuleSetFilePath = solutionRuleSet?.FilePath;

            var rulesetExists = solutionRuleSet != null;
            logger.LogDebug($"[Binding check] Solution-level ruleset exists: {rulesetExists} (file path: '{solutionRuleSetFilePath}')");

            return rulesetExists;
        }

        // PERF: this method loads and returns the solution ruleset as an object, but it isn't used; only the file path is used.
        // Loading the file path does validate that file is valid, but we don't need to do this for every project - once would be enough.
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

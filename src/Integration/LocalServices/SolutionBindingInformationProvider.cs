/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration
{
    internal class SolutionBindingInformationProvider : ISolutionBindingInformationProvider
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ISolutionRuleSetsInformationProvider ruleSetInfoProvider;

        public SolutionBindingInformationProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;

            ruleSetInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();
        }

        #region ISolutionBindingInformationProvider

        public IEnumerable<Project> GetUnboundProjects()
        {
            var configProvider = this.serviceProvider.GetService<IConfigurationProvider>();
            configProvider.AssertLocalServiceIsNotNull();

            // Only applicable in connected mode (legacy or new)
            var bindingConfig = configProvider.GetConfiguration();

            if (!bindingConfig.Mode.IsInAConnectedMode())
            {
                return Enumerable.Empty<Project>();
            }

            return this.GetUnboundProjects(bindingConfig);
        }
        #endregion

        #region Non-public API

        private IEnumerable<Project> GetUnboundProjects(BindingConfiguration binding)
        {
            Debug.Assert(binding.Mode.IsInAConnectedMode());
            Debug.Assert(binding.Project != null);

            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            var rulesetSerializer = this.serviceProvider.GetService<IRuleSetSerializer>();
            rulesetSerializer.AssertLocalServiceIsNotNull();

            // Projects will be using the same solution ruleset in most of the cases,
            // projects could have multiple configurations all of which using the same rule set,
            // we want to minimize the number of disk operations since the
            // method can be called from the UI thread, hence this short-lived cache
            var cachingSerializer = new CachingRulesetSerializer(rulesetSerializer);

            // Note: we will still may end up analyzing the same project rule set
            // but that should in marginal since it will be already loaded into memory

            // Reuse the binding information passed in to avoid reading it more than once
            return projectSystem.GetFilteredSolutionProjects()
                .Where(p => !this.IsFullyBoundProject(cachingSerializer, binding, p));
        }

        private bool IsFullyBoundProject(IRuleSetSerializer ruleSetSerializer, BindingConfiguration binding, Project project)
        {
            Debug.Assert(binding != null);
            Debug.Assert(project != null);

            // If solution is not bound/has a missing ruleset, no need to go further
            RuleSet sonarQubeRuleSet = this.FindSonarQubeSolutionRuleSet(ruleSetSerializer, binding, project);
            if (sonarQubeRuleSet == null)
            {
                return false;
            }

            RuleSetDeclaration[] declarations = ruleSetInfoProvider.GetProjectRuleSetsDeclarations(project).ToArray();
            return declarations.Length > 0 // Need at least one
                && declarations.All(declaration => this.IsRuleSetBound(ruleSetSerializer, project, declaration, sonarQubeRuleSet));
        }

        private bool IsRuleSetBound(IRuleSetSerializer ruleSetSerializer, Project project, RuleSetDeclaration declaration, RuleSet sonarQubeRuleSet)
        {
            RuleSet projectRuleSet = this.FindDeclarationRuleSet(ruleSetSerializer, project, declaration);
            return (projectRuleSet != null && RuleSetIncludeChecker.HasInclude(projectRuleSet, sonarQubeRuleSet));
        }
        
        private RuleSet FindSonarQubeSolutionRuleSet(IRuleSetSerializer ruleSetSerializer, BindingConfiguration binding, Project project)
        {
            string expectedSolutionRuleSet = ruleSetInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(
                         binding.Project.ProjectKey,
                         Language.ForProject(project),
                         binding.Mode);

            return ruleSetSerializer.LoadRuleSet(expectedSolutionRuleSet);
        }

        private RuleSet FindDeclarationRuleSet(IRuleSetSerializer ruleSetSerializer, Project project, RuleSetDeclaration declaration)
        {
            string ruleSetFilePath;

            // Check if project rule set is found (we treat missing/erroneous rule set settings as not found)
            if (!ruleSetInfoProvider.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetFilePath))
            {
                return null;
            }

            return ruleSetSerializer.LoadRuleSet(ruleSetFilePath);
        }

        private class CachingRulesetSerializer : IRuleSetSerializer
        {
            private readonly IRuleSetSerializer serializer;
            private readonly Dictionary<string, RuleSet> pathToRulesetMap = new Dictionary<string, RuleSet>(StringComparer.OrdinalIgnoreCase);

            public CachingRulesetSerializer(IRuleSetSerializer ruleSetSerializer)
            {
                Debug.Assert(ruleSetSerializer != null);
                serializer = ruleSetSerializer;
            }

            public RuleSet LoadRuleSet(string path)
            {
                Debug.Assert(System.IO.Path.IsPathRooted(path));

                RuleSet locatedRuleset;
                if (!pathToRulesetMap.TryGetValue(path, out locatedRuleset))
                {
                    locatedRuleset = serializer.LoadRuleSet(path);
                    pathToRulesetMap[path] = locatedRuleset;
                }
                return locatedRuleset;
            }

            public void WriteRuleSetFile(RuleSet ruleSet, string path)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}

/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    internal class SolutionBindingInformationProvider : ISolutionBindingInformationProvider
    {
        private readonly IServiceProvider serviceProvider;

        public SolutionBindingInformationProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        #region ISolutionBindingInformationProvider
        public bool IsSolutionBound()
        {
            return this.GetSolutionBinding() != null;
        }

        public IEnumerable<Project> GetBoundProjects()
        {
            return this.GetBoundProjects(this.GetSolutionBinding());
        }

        public IEnumerable<Project> GetUnboundProjects()
        {
            return this.GetUnboundProjects(this.GetSolutionBinding());
        }
        #endregion

        #region Non-public API
        private BoundSonarQubeProject GetSolutionBinding()
        {
            var bindingSerializer = this.serviceProvider.GetService<ISolutionBindingSerializer>();
            bindingSerializer.AssertLocalServiceIsNotNull();

            return bindingSerializer.ReadSolutionBinding();
        }

        private IEnumerable<Project> GetUnboundProjects(BoundSonarQubeProject binding)
        {
            if (binding == null)
            {
                return Enumerable.Empty<Project>();
            }

            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            // Reuse the binding information passed in to avoid reading it more than once
            return projectSystem.GetFilteredSolutionProjects().Except(this.GetBoundProjects(binding));
        }

        private IEnumerable<Project> GetBoundProjects(BoundSonarQubeProject binding)
        {
            if (binding == null)
            {
                return Enumerable.Empty<Project>();
            }

            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            // Projects will be using the same solution ruleset in most of the cases,
            // projects could have multiple configurations all of which using the same rule set,
            // we want to minimize the number of disk operations since the
            // method ca be called from the UI thread, hence this short-lived cache
            Dictionary<string, RuleSet> cache = new Dictionary<string, RuleSet>(StringComparer.OrdinalIgnoreCase);

            // Note: we will still may end up analyzing the same project rule set
            // but that should in marginal since it will be already loaded into memory

            // Reuse the binding information passed in to avoid reading it more than once
            return projectSystem.GetFilteredSolutionProjects()
                .Where(p => this.IsFullyBoundProject(cache, binding, p));
        }

        private bool IsFullyBoundProject(Dictionary<string, RuleSet> cache, BoundSonarQubeProject binding, Project project)
        {
            Debug.Assert(binding != null);
            Debug.Assert(project != null);

            // If solution is not bound/has a missing ruleset, no need to go further
            RuleSet sonarQubeRuleSet = this.FindSonarQubeSolutionRuleSet(cache, binding, project);
            if (sonarQubeRuleSet == null)
            {
                return false;
            }

            var ruleSetInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();

            RuleSetDeclaration[] declarations = ruleSetInfoProvider.GetProjectRuleSetsDeclarations(project).ToArray();
            return declarations.Length > 0 // Need at least one
                && declarations.All(declaration => this.IsRuleSetBound(cache, project, declaration, sonarQubeRuleSet));
        }

        private bool IsRuleSetBound(Dictionary<string, RuleSet> cache, Project project, RuleSetDeclaration declaration, RuleSet sonarQubeRuleSet)
        {
            RuleSet projectRuleSet = this.FindDeclarationRuleSet(cache, project, declaration);

            return (projectRuleSet != null && RuleSetHelper.FindInclude(projectRuleSet, sonarQubeRuleSet) != null);
        }

        private RuleSet FindSonarQubeSolutionRuleSet(Dictionary<string, RuleSet> cache, BoundSonarQubeProject binding, Project project)
        {
            var ruleSetInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();

            string expectedSolutionRuleSet = ruleSetInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(
                         binding.ProjectKey,
                         Language.ForProject(project));

            RuleSet solutionRuleSet;
            if (!cache.TryGetValue(expectedSolutionRuleSet, out solutionRuleSet))
            {
                var ruleSetSerializer = this.serviceProvider.GetService<IRuleSetSerializer>();
                ruleSetSerializer.AssertLocalServiceIsNotNull();

                solutionRuleSet = ruleSetSerializer.LoadRuleSet(expectedSolutionRuleSet);
                cache[expectedSolutionRuleSet] = solutionRuleSet;
            }

            return solutionRuleSet;
        }

        private RuleSet FindDeclarationRuleSet(Dictionary<string, RuleSet> cache, Project project, RuleSetDeclaration declaration)
        {
            var ruleSetInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();

            var ruleSetSerializer = this.serviceProvider.GetService<IRuleSetSerializer>();
            ruleSetSerializer.AssertLocalServiceIsNotNull();

            string ruleSetFilePath;

            // Check if project rule set is found (we treat missing/erroneous rule set settings as not found)
            if (!ruleSetInfoProvider.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetFilePath))
            {
                return null;
            }

            RuleSet projectRuleSet;
            if (!cache.TryGetValue(ruleSetFilePath, out projectRuleSet))
            {
                // We treat corrupted rulesets as not found
                projectRuleSet = ruleSetSerializer.LoadRuleSet(ruleSetFilePath);
                cache[ruleSetFilePath] = projectRuleSet;
            }

            return projectRuleSet;
        }
        #endregion
    }
}

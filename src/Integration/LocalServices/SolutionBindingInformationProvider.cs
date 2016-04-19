//-----------------------------------------------------------------------
// <copyright file="SolutionBindingInformationProvider.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
        public IEnumerable<Project> GetBoundProjects()
        {
            var bindingSerializer = this.serviceProvider.GetService<ISolutionBindingSerializer>();
            bindingSerializer.AssertLocalServiceIsNotNull();

            // We only have bound projects if the solution has persisted solution binding
            BoundSonarQubeProject binding = bindingSerializer.ReadSolutionBinding();
            if (binding == null)
            {
                return Enumerable.Empty<Project>();
            }

            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            return projectSystem.GetFilteredSolutionProjects()
                .Where(p => this.IsFullyBoundProject(binding, p));
        }

        public IEnumerable<Project> GetUnboundProjects()
        {
            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            return projectSystem.GetFilteredSolutionProjects().Except(this.GetBoundProjects());
        }
        #endregion

        #region Non-public API
        private bool IsFullyBoundProject(BoundSonarQubeProject binding, Project project)
        {
            Debug.Assert(binding != null);
            Debug.Assert(project != null);

            // If solution is not bound/has a missing ruleset, no need to go further
            RuleSet sonarQubeRuleSet = this.FindSonarQubeSolutionRuleSet(binding, project);
            if (sonarQubeRuleSet == null)
            {
                return false;
            }

            var ruleSetInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();

            RuleSetDeclaration[] declarations = ruleSetInfoProvider.GetProjectRuleSetsDeclarations(project).ToArray();
            return declarations.Length > 0 // Need at least one
                && declarations.All(declaration => this.IsRuleSetBound(project, declaration, sonarQubeRuleSet));
        }

        private bool IsRuleSetBound(Project project, RuleSetDeclaration declaration, RuleSet sonarQubeRuleSet)
        {
            RuleSet projectRuleSet = this.FindDeclarationRuleSet(project, declaration);

            return (projectRuleSet != null && RuleSetHelper.FindInclude(projectRuleSet, sonarQubeRuleSet) != null);
        }

        private RuleSet FindSonarQubeSolutionRuleSet(BoundSonarQubeProject binding, Project project)
        {
            var ruleSetInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();

            var ruleSetSerializer = this.serviceProvider.GetService<IRuleSetSerializer>();
            ruleSetSerializer.AssertLocalServiceIsNotNull();

            string expectedSolutionRuleSet = ruleSetInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(
                         binding.ProjectKey,
                         ProjectBindingOperation.GetProjectGroup(project));

            return ruleSetSerializer.LoadRuleSet(expectedSolutionRuleSet);
        }

        private RuleSet FindDeclarationRuleSet(Project project, RuleSetDeclaration declaration)
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

            // We treat corrupted rulesets as not found
            return ruleSetSerializer.LoadRuleSet(ruleSetFilePath);
        }
        #endregion
    }
}

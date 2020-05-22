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
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class RuleSetReferenceChecker : IRuleSetReferenceChecker
    {
        private readonly ISolutionRuleSetsInformationProvider ruleSetInfoProvider;
        private readonly IRuleSetSerializer ruleSetSerializer;

        public RuleSetReferenceChecker(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            ruleSetInfoProvider = serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();

            ruleSetSerializer = serviceProvider.GetService<IRuleSetSerializer>();
            ruleSetSerializer.AssertLocalServiceIsNotNull();
        }

        public bool IsReferenced(Project project, RuleSet ruleSet)
        {
            var declarations = ruleSetInfoProvider.GetProjectRuleSetsDeclarations(project).ToArray();

            var isRuleSetBound = declarations.Length > 0 &&
                                 declarations.All(declaration => IsRuleSetReferenced(project, declaration, ruleSet));

            return isRuleSetBound;
        }

        private bool IsRuleSetReferenced(Project project, RuleSetDeclaration declaration, RuleSet ruleSet)
        {
            var projectRuleSet = FindDeclarationRuleSet(project, declaration);

            return projectRuleSet != null && RuleSetIncludeChecker.HasInclude(projectRuleSet, ruleSet);
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

//-----------------------------------------------------------------------
// <copyright file="ConfigurableConflictsManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableConflictsManager : IConflictsManager
    {
        private readonly List<ProjectRuleSetConflict> currentConflicts = new List<ProjectRuleSetConflict>();

        #region IConflictsManager
        IReadOnlyList<ProjectRuleSetConflict> IConflictsManager.GetCurrentConflicts()
        {
            return this.currentConflicts;
        }
        #endregion

        #region Test helpers
        public static ProjectRuleSetConflict CreateConflict(string projectFilePath = "project.csproj", string baselineRuleSet = "baseline.ruleset", string projectRuleSet = "project.csproj", int numberOfConflictingRules = 1)
        {
            IEnumerable<string> ids = Enumerable.Range(0, numberOfConflictingRules).Select(i => "id" + i);
            var ruleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(ids);

            var conflict = new ProjectRuleSetConflict(
                    new RuleConflictInfo(ruleSet.Rules, new Dictionary<RuleReference, RuleAction>()),
                    new RuleSetAggregate(projectFilePath, baselineRuleSet, projectRuleSet, null));

            return conflict;

        }
        public ProjectRuleSetConflict AddConflict()
        {
            ProjectRuleSetConflict conflict = CreateConflict();
            this.AddConflict(conflict);
            return conflict;
        }

        public void AddConflict(ProjectRuleSetConflict conflict)
        {
            this.currentConflicts.Add(conflict);
        }

        public void ClearConflicts()
        {
            this.currentConflicts.Clear();
        }
        #endregion
    }
}

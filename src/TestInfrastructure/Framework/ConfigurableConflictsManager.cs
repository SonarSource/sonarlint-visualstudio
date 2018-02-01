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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.ProfileConflicts;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableConflictsManager : IConflictsManager
    {
        private readonly List<ProjectRuleSetConflict> currentConflicts = new List<ProjectRuleSetConflict>();

        #region IConflictsManager

        IReadOnlyList<ProjectRuleSetConflict> IConflictsManager.GetCurrentConflicts()
        {
            return this.currentConflicts;
        }

        #endregion IConflictsManager

        #region Test helpers

        public static ProjectRuleSetConflict CreateConflict(string projectFilePath = "project.csproj", string baselineRuleSet = "baseline.ruleset", string projectRuleSet = "project.csproj", int numberOfConflictingRules = 1)
        {
            IEnumerable<string> ids = Enumerable.Range(0, numberOfConflictingRules).Select(i => "id" + i);
            var ruleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(ids);

            var conflict = new ProjectRuleSetConflict(
                    new RuleConflictInfo(ruleSet.Rules, new Dictionary<RuleReference, RuleAction>()),
                    new RuleSetInformation(projectFilePath, baselineRuleSet, projectRuleSet, null));

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

        #endregion Test helpers
    }
}
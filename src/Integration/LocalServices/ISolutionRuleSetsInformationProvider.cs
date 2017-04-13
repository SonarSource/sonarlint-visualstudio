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
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration
{
    interface ISolutionRuleSetsInformationProvider : ILocalService
    {
        /// <summary>
        /// For a given <paramref name="project"/> will return all the <see cref="RuleSetDeclaration"/>
        /// </summary>
        /// <param name="project">Required</param>
        /// <returns>Not null</returns>
        IEnumerable<RuleSetDeclaration> GetProjectRuleSetsDeclarations(Project project);

        /// <summary>
        /// Will return a calculate file path to the shared SonarQube RuleSet
        /// that corresponds to the <paramref name="sonarQubeProjectKey"/> (with  <paramref name="fileNameSuffix"/>).
        /// </summary>
        /// <param name="sonarQubeProjectKey">Required</param>
        /// <param name="language">The language this rule set corresponds to</param>
        /// <returns>Full file path. The file may not actually exist on disk</returns>
        string CalculateSolutionSonarQubeRuleSetFilePath(string sonarQubeProjectKey, Language language);

        /// <summary>
        /// Will return a calculated file path to the expected project RuleSet
        /// that corresponds to the <paramref name="declaration"/>.
        /// </summary>
        /// <param name="project">Required</param>
        /// <param name="declaration">Required</param>
        /// <param name="fullFilePath">A full file path to an existing RuleSet, or null if failed.</param>
        /// <returns>Whether succeeded in which case the <param name="fullFilePath" /> will point to an existing file</returns>
        bool TryGetProjectRuleSetFilePath(Project project, RuleSetDeclaration declaration, out string fullFilePath);

        /// <summary>
        /// Returns the path to solution level rulesets.
        /// When the solution is closed returns null.
        /// </summary>
        string GetSolutionSonarQubeRulesFolder();
    }
}

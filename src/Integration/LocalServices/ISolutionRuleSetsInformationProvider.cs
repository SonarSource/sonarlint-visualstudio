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

using System.Collections.Generic;
using EnvDTE;
using SonarLint.VisualStudio.Core.Binding;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration
{
    // Legacy connected mode:
    // Gets the rulesets/paths/solution folder for the old connected mode.

    interface ISolutionRuleSetsInformationProvider : ILocalService
    {
        /// <summary>
        /// For a given <paramref name="project"/> will return all the <see cref="RuleSetDeclaration"/>
        /// </summary>
        /// <param name="project">Required</param>
        /// <returns>Not null</returns>
        IEnumerable<RuleSetDeclaration> GetProjectRuleSetsDeclarations(Project project);

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
        /// <param name="bindingMode">The binding mode (legacy or new)</param>
        string GetSolutionSonarQubeRulesFolder(SonarLintMode bindingMode);
    }
}

//-----------------------------------------------------------------------
// <copyright file="ISolutionRuleSetsInformationProvider.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
        /// <param name="ruleSetGroup">The logical group of RuleSets that the solution RuleSet is created for</param>
        /// <returns>Full file path. The file may not actually exist on disk</returns>
        string CalculateSolutionSonarQubeRuleSetFilePath(string sonarQubeProjectKey, LanguageGroup ruleSetGroup);

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

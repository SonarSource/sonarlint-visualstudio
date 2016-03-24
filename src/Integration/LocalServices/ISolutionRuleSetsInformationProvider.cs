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
        /// For a given <paramref name="project"/> will return a calculate file path to the 
        /// shared SonarQube RuleSet that corresponds to the <paramref name="sonarQubeProjectKey"/> (with  <paramref name="fileNameSuffix"/>). 
        /// </summary>
        /// <param name="sonarQubeProjectKey">Required</param>
        /// <param name="fileNameSuffix">Required</param>
        /// <returns>Full file path. The file may not actually exist on disk</returns>
        string CalculateSolutionSonarQubeRuleSetFilePath(string sonarQubeProjectKey, string fileNameSuffix);
    }
}

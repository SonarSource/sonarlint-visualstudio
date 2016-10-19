//-----------------------------------------------------------------------
// <copyright file="ConfigurableSolutionRuleSetsInformationProvider.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableSolutionRuleSetsInformationProvider : ISolutionRuleSetsInformationProvider
    {
        private readonly Dictionary<Project, List<RuleSetDeclaration>> registeredProjectData = new Dictionary<Project, List<RuleSetDeclaration>>();

        #region ISolutionRuleSetsInformationProvider
        IEnumerable<RuleSetDeclaration> ISolutionRuleSetsInformationProvider.GetProjectRuleSetsDeclarations(Project project)
        {
            Assert.IsNotNull(project);

            List<RuleSetDeclaration> result;
            if (!this.registeredProjectData.TryGetValue(project, out result))
            {
                result = new List<RuleSetDeclaration>();
                this.registeredProjectData[project] = result;
            }

            return result;
        }
        string ISolutionRuleSetsInformationProvider.GetSolutionSonarQubeRulesFolder()
        {
            return Path.Combine(this.SolutionRootFolder, Constants.SonarQubeManagedFolderName);
        }

        string ISolutionRuleSetsInformationProvider.CalculateSolutionSonarQubeRuleSetFilePath(string sonarQubeProjectKey, Language language)
        {
            string fileName = $"{sonarQubeProjectKey}{language.Id}.{Constants.RuleSetFileExtension}";
            return Path.Combine(((ISolutionRuleSetsInformationProvider)this).GetSolutionSonarQubeRulesFolder(), fileName);
        }

        bool ISolutionRuleSetsInformationProvider.TryGetProjectRuleSetFilePath(Project project, RuleSetDeclaration declaration, out string fullFilePath)
        {
            fullFilePath = declaration.RuleSetPath;

            return true;
        }

        #endregion

        #region Test helpers
        public void RegisterProjectInfo(Project project, params RuleSetDeclaration[] info)
        {
            List<RuleSetDeclaration> declarations;

            if (!this.registeredProjectData.TryGetValue(project, out declarations))
            {
                declarations = new List<RuleSetDeclaration>();
                this.registeredProjectData[project] = declarations;
            }

            declarations.AddRange(info);
        }

        public void ClearProjectInfo(Project project)
        {
            this.registeredProjectData.Remove(project);
        }

        public string SolutionRootFolder { get; set; }
        #endregion
    }
}

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
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableSolutionRuleSetsInformationProvider : ISolutionRuleSetsInformationProvider
    {
        private readonly Dictionary<Project, IEnumerable<RuleSetDeclaration>> registeredProjectData = new Dictionary<Project, IEnumerable<RuleSetDeclaration>>();

        #region ISolutionRuleSetsInformationProvider
        IEnumerable<RuleSetDeclaration> ISolutionRuleSetsInformationProvider.GetProjectRuleSetsDeclarations(Project project)
        {
            Assert.IsNotNull(project);

            IEnumerable<RuleSetDeclaration> result;
            if (!this.registeredProjectData.TryGetValue(project, out result))
            {
                result = Enumerable.Empty<RuleSetDeclaration>();
            }

            return result;
        }

        string ISolutionRuleSetsInformationProvider.CalculateSolutionSonarQubeRuleSetFilePath(string sonarQubeProjectKey, string fileNameSuffix)
        {
            return Path.Combine(this.SolutionRootFolder, Constants.SonarQubeManagedFolderName, sonarQubeProjectKey + fileNameSuffix + "." + Constants.RuleSetFileExtension);
        }
        #endregion

        #region Test helpers
        public void RegsiterProjectInfo(Project project, params RuleSetDeclaration[] info )
        {
            this.registeredProjectData[project] = info;
        }

        public string SolutionRootFolder { get; set; }
        #endregion
    }
}

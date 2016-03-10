//-----------------------------------------------------------------------
// <copyright file="SolutionRuleSetWriter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Diagnostics;
using System.IO;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class SolutionRuleSetWriter : RuleSetWriter
    {
        private readonly ProjectInformation projectInformation;

        public SolutionRuleSetWriter(ProjectInformation projectInformation, IRuleSetGenerationFileSystem fileSystem = null)
            :base(fileSystem)
        {
            if (projectInformation == null)
            {
                throw new ArgumentNullException(nameof(projectInformation));
            }

            this.projectInformation = projectInformation;
        }

        /// <summary>
        /// Write out the provided <see cref="RuleSet"/> file under the specified solution root path.
        /// </summary>
        /// <param name="solutionFullPath">Full path to the solution file</param>
        /// <param name="downloadedRuleSet"><see cref="RuleSet"/> that was downloaded from the server</param>
        /// <param name="fileNameSuffix">Rule set file suffix</param>
        /// <returns>Full file path of the file that was written out</returns>
        public string WriteSolutionLevelRuleSet(string solutionFullPath, RuleSet downloadedRuleSet, string fileNameSuffix)
        {
            if (string.IsNullOrWhiteSpace(solutionFullPath))
            {
                throw new ArgumentNullException(nameof(solutionFullPath));
            }

            if (downloadedRuleSet == null)
            {
                throw new ArgumentNullException(nameof(downloadedRuleSet));
            }

            if (fileNameSuffix == null)
            {
                throw new ArgumentNullException(nameof(fileNameSuffix));
            }

            string solutionRoot = Path.GetDirectoryName(solutionFullPath);
            string ruleSetRoot = this.GetOrCreateRuleSetDirectory(solutionRoot);

            // Create or overwrite existing rule set
            string existingRuleSetPath = GenerateSolutionRuleSetPath(ruleSetRoot, this.projectInformation, fileNameSuffix);
            this.FileSystem.WriteRuleSetFile(downloadedRuleSet, existingRuleSetPath);

            return existingRuleSetPath;
        }

        /// <summary>
        /// Generate a solution level rule set file path from the given <see cref="ProjectInformation"/>.
        /// </summary>
        /// <param name="ruleSetRootPath">Root directory to generate the full file path under</param>
        /// <param name="sonarQubeProject">SonarQube project to generate a rule set file name path for</param>
        /// <param name="fileNameSuffix">Fixed file name suffix</param>
        internal /* testing purposes */ static string GenerateSolutionRuleSetPath(string ruleSetRootPath, ProjectInformation sonarQubeProject, string fileNameSuffix)
        {
            Debug.Assert(fileNameSuffix != null);

            // Cannot use Path.ChangeExtension here because if the sonar project name contains
            // a dot (.) then everything after this will be replaced with .ruleset
            string fileName = $"{PathHelper.EscapeFileName(sonarQubeProject.Key + fileNameSuffix)}.{FileExtension}";
            return Path.Combine(ruleSetRootPath, fileName);
        }

        /// <summary>
        /// Ensure that the solution level SonarQube rule set folder exists and return the full path to it.
        /// </summary>
        public string GetOrCreateRuleSetDirectory(string solutionRoot)
        {
            string ruleSetDirectoryPath = Path.Combine(solutionRoot, Constants.SonarQubeManagedFolderName);
            if (!this.FileSystem.DirectoryExists(ruleSetDirectoryPath))
            {
                this.FileSystem.CreateDirectory(ruleSetDirectoryPath);
            }
            return ruleSetDirectoryPath;
        }
    }
}

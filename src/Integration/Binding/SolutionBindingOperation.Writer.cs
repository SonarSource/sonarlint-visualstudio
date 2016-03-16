//-----------------------------------------------------------------------
// <copyright file="SolutionBindingOperation.Writer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System;
using System.IO;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal partial class SolutionBindingOperation
    {
        private readonly IRuleSetFileSystem ruleSetFileSystem;

        /// <summary>
        /// Queues a write of the provided <see cref="RuleSet"/> file under the specified solution root path.
        /// </summary>
        /// <param name="solutionFullPath">Full path to the solution file</param>
        /// <param name="downloadedRuleSet"><see cref="RuleSet"/> that was downloaded from the server</param>
        /// <param name="fileNameSuffix">Rule set file suffix</param>
        /// <returns>Full file path of the file that we expect to write to</returns>
        internal string QueueWriteSolutionLevelRuleSet(string solutionFullPath, RuleSet downloadedRuleSet, string fileNameSuffix)
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
            string solutionRuleSet = GenerateSolutionRuleSetPath(ruleSetRoot, this.sonarQubeProjectKey, fileNameSuffix);
            this.sourceControlledFileSystem.QueueFileWrite(solutionRuleSet, () =>
            {
                this.ruleSetFileSystem.WriteRuleSetFile(downloadedRuleSet, solutionRuleSet);

                return true;
            });

            return solutionRuleSet;
        }

        /// <summary>
        /// Generate a solution level rule set file path from the given <see cref="ProjectInformation"/>.
        /// </summary>
        /// <param name="ruleSetRootPath">Root directory to generate the full file path under</param>
        /// <param name="sonarQubeProjectKey">SonarQube project key to generate a rule set file name path for</param>
        /// <param name="fileNameSuffix">Fixed file name suffix</param>
        private static string GenerateSolutionRuleSetPath(string ruleSetRootPath, string sonarQubeProjectKey, string fileNameSuffix)
        {
            // Cannot use Path.ChangeExtension here because if the sonar project name contains
            // a dot (.) then everything after this will be replaced with .ruleset
            string fileName = $"{PathHelper.EscapeFileName(sonarQubeProjectKey + fileNameSuffix ?? string.Empty)}.{Constants.RuleSetFileExtension}";
            return Path.Combine(ruleSetRootPath, fileName);
        }

        /// <summary>
        /// Ensure that the solution level SonarQube rule set folder exists and return the full path to it.
        /// </summary>
        private string GetOrCreateRuleSetDirectory(string solutionRoot)
        {
            string ruleSetDirectoryPath = Path.Combine(solutionRoot, Constants.SonarQubeManagedFolderName);
            if (!this.sourceControlledFileSystem.DirectoryExists(ruleSetDirectoryPath))
            {
                this.sourceControlledFileSystem.CreateDirectory(ruleSetDirectoryPath);
            }
            return ruleSetDirectoryPath;
        }
    }
}

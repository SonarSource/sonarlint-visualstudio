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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal partial class ProjectBindingOperation
    {
        internal const string DefaultProjectRuleSet = "MinimumRecommendedRules.ruleset";
        private readonly IRuleSetSerializer ruleSetSerializer;

        internal /*for testing purposes*/ IDictionary<string, RuleSet> AlreadyUpdatedExistingRuleSetPaths
        {
            get;
        } = new Dictionary<string, RuleSet>(StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// Queues a write to a project-level SonarQube <see cref="RuleSet"/> file in the <param name="projectRoot"/> directory.
        /// </summary>
        /// <param name="projectFullPath">The absolute full path to the project</param>
        /// <param name="ruleSetFileName">The rule set file name</param>
        /// <param name="solutionRuleSet\">Full path of the parent solution-level SonarQube rule set</param>
        /// <param name="existingRuleSetPath">Existing project rule set</param>
        /// <returns>Full file path of the file that we expect to write to</returns>
        internal /*for testing purposes*/ string QueueWriteProjectLevelRuleSet(string projectFullPath, string ruleSetFileName, RuleSetInformation solutionRuleSet, string currentRuleSetPath)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(projectFullPath));
            Debug.Assert(!string.IsNullOrWhiteSpace(ruleSetFileName));
            Debug.Assert(solutionRuleSet != null);

            string projectRoot = Path.GetDirectoryName(projectFullPath);
            string ruleSetRoot = PathHelper.ForceDirectoryEnding(projectRoot);

            string existingRuleSetPath;
            RuleSet existingRuleSet;
            if (this.TryUpdateExistingProjectRuleSet(solutionRuleSet.NewRuleSetFilePath, ruleSetRoot, currentRuleSetPath, out existingRuleSetPath, out existingRuleSet))
            {
                Debug.Assert(existingRuleSetPath != null);
                Debug.Assert(existingRuleSet != null);

                // Pend update
                this.sourceControlledFileSystem.QueueFileWrite(existingRuleSetPath, () =>
                {
                    existingRuleSet.WriteToFile(existingRuleSetPath);
                    return true;
                });

                return existingRuleSetPath;
            }

            // Create a new project level rule set
            string solutionIncludePath = PathHelper.CalculateRelativePath(ruleSetRoot, solutionRuleSet.NewRuleSetFilePath);
            RuleSet newRuleSet = GenerateNewProjectRuleSet(solutionIncludePath, currentRuleSetPath, solutionRuleSet.RuleSet.DisplayName);
            string newRuleSetPath = this.GenerateNewProjectRuleSetPath(ruleSetRoot, ruleSetFileName);

            // Pend new
            this.sourceControlledFileSystem.QueueFileWrite(newRuleSetPath, () =>
            {
                this.ruleSetSerializer.WriteRuleSetFile(newRuleSet, newRuleSetPath);
                return true;
            });

            return newRuleSetPath;
        }

        #region Rule Set Helpers
        internal /*for testing purposes*/ bool TryUpdateExistingProjectRuleSet(string solutionRuleSetPath, string projectRuleSetRootFolder, string currentRuleSet, out string existingRuleSetPath, out RuleSet existingRuleSet)
        {
            existingRuleSetPath = null;
            existingRuleSet = null;

            if (ShouldIgnoreConfigureRuleSetValue(currentRuleSet))
            {
                return false;
            }

            existingRuleSetPath = PathHelper.ResolveRelativePath(currentRuleSet, projectRuleSetRootFolder);
            if (!PathHelper.IsPathRootedUnderRoot(existingRuleSetPath, projectRuleSetRootFolder))
            {
                // Not our file (i.e. absolute path to some other ruleset)
                existingRuleSetPath = null;
                return false;
            }

            if (this.AlreadyUpdatedExistingRuleSetPaths.TryGetValue(existingRuleSetPath, out existingRuleSet))
            {
                return true;
            }

            existingRuleSet = this.ruleSetSerializer.LoadRuleSet(existingRuleSetPath);
            if (existingRuleSet == null)
            {
                existingRuleSetPath = null;
                return false;
            }

            RuleSetUpdater.UpdateExistingProjectRuleSet(existingRuleSet, solutionRuleSetPath);
            this.AlreadyUpdatedExistingRuleSetPaths.Add(existingRuleSetPath, existingRuleSet);
            return true;
        }


        /// <summary>
        /// Generate a new project level rule set with the provided inclusions (if not an ignored rule set).
        /// </summary>
        /// <param name="solutionIncludePath">Solution level rule set include path (always included)</param>
        /// <param name="currentRuleSetPath">Original rule set include path (only included if not white-listed as 'ignore')</param>
        /// <param name="ruleSetName">The name given to the project ruleset.</param>
        internal /* testing purposes */ static RuleSet GenerateNewProjectRuleSet(string solutionIncludePath, string currentRuleSetPath, string ruleSetName)
        {
            var ruleSet = new RuleSet(ruleSetName);
            ruleSet.RuleSetIncludes.Add(new RuleSetInclude(solutionIncludePath, RuleAction.Default));

            // No way to detect whether a ruleset was set by the user or just the default value in case of the
            // default rule set property value. The idea here is that we would like to preserve any explicit setting by the user
            // and we assume that the default rule set can be safely ignored since wasn't set explicitly by the user (or even if it was
            // it has low value in comparison to what is configured in SQ).
            if (!ShouldIgnoreConfigureRuleSetValue(currentRuleSetPath))
            {
                ruleSet.RuleSetIncludes.Add(new RuleSetInclude(currentRuleSetPath, RuleAction.Default));
            }
            return ruleSet;
        }

        /// <summary>
        /// Gets whether or not the provided rule set should be ignored for inclusion in a rule set.
        /// </summary>
        public static bool ShouldIgnoreConfigureRuleSetValue(string ruleSet)
        {
            return string.IsNullOrWhiteSpace(ruleSet) || StringComparer.OrdinalIgnoreCase.Equals(DefaultProjectRuleSet, ruleSet);
        }

        #endregion

        #region Path Helpers

        /// <summary>
        /// Generate a project level rule set file path from the given project name.
        /// </summary>
        /// <param name="ruleSetFileName">Name of the rule set file</param>
        /// <param name="configuration">Project configuration (optional)</param>
        /// <returns>Full unique file path of project level rule set file</returns>
        internal /* testing purposes */ string GenerateNewProjectRuleSetPath(string ruleSetRootPath, string ruleSetFileName)
        {

            string escapedFileName = PathHelper.EscapeFileName(ruleSetFileName);

            // Set a reasonable maximum number of integer-based unique names to try
            const uint maxAttempts = 9;
            string candiatePath;
            uint i = 0;
            do
            {
                string uniqueStr = i > 0 ? $"-{i}" : string.Empty;

                // In case of failed exhaustive available 'nice' file name search, use a GUID
                if (i == maxAttempts)
                {
                    uniqueStr = $"-{Guid.NewGuid():N}";
                }

                // Cannot use Path.ChangeExtension here because if the sonar project name contains
                // a dot (.) then everything after this will be replaced with .ruleset
                string candidateFileName = $"{escapedFileName}{uniqueStr}.{Constants.RuleSetFileExtension}";

                candiatePath = Path.Combine(ruleSetRootPath, candidateFileName);

                // Increment index
                i++;
            } while (this.sourceControlledFileSystem.FileExistOrQueuedToBeWritten(candiatePath));

            return candiatePath;
        }

        #endregion
    }
}

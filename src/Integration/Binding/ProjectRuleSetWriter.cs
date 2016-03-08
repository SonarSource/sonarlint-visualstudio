//-----------------------------------------------------------------------
// <copyright file="ProjectRuleSetWriter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class ProjectRuleSetWriter : RuleSetWriter
    {
        internal const string DefaultProjectRuleSet = "MinimumRecommendedRules.ruleset";

        internal /*for testing purposes*/ ISet<string> AlreadyUpdatedExistingRuleSetPaths
        {
            get;
        } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ProjectRuleSetWriter(IRuleSetGenerationFileSystem fileSystem = null)
            : base(fileSystem)
        {
        }

        /// <summary>
        /// Write a project-level SonarQube <see cref="RuleSet"/> file in the <param name="projectRoot"/> directory.
        /// </summary>
        /// <param name="projectFullPath">The absolute full path to the project</param>
        /// <param name="configurationName">Configuration of the project</param>
        /// <param name="solutionRuleSetPath">Full path of the parent solution-level SonarQube rule set</param>
        /// <param name="existingRuleSetPath">Existing project rule set</param>
        /// <returns>Full file path of the file that was written out</returns>
        public string WriteProjectLevelRuleSet(string projectFullPath, string configurationName, string solutionRuleSetPath, string currentRuleSetPath)
        {
            if (string.IsNullOrWhiteSpace(projectFullPath))
            {
                throw new ArgumentNullException(nameof(projectFullPath));
            }

            if (string.IsNullOrWhiteSpace(solutionRuleSetPath))
            {
                throw new ArgumentNullException(nameof(solutionRuleSetPath));
            }

            string projectRoot = Path.GetDirectoryName(projectFullPath);
            string ruleSetRoot = PathHelper.ForceDirectoryEnding(projectRoot);

            string existingRuleSetPath;
            if (this.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, ruleSetRoot, currentRuleSetPath, out existingRuleSetPath))
            {
                Debug.Assert(existingRuleSetPath != null);
                return existingRuleSetPath;
            }

            // Create a new project level rule set
            string projectName = Path.GetFileNameWithoutExtension(projectFullPath);
            string solutionIncludePath = PathHelper.CalculateRelativePath(ruleSetRoot, solutionRuleSetPath);
            RuleSet newRuleSet = GenerateNewProjectRuleSet(solutionIncludePath, currentRuleSetPath);
            string newRuleSetPath = this.GenerateNewProjectRuleSetPath(ruleSetRoot, projectName, configurationName);
            this.FileSystem.WriteRuleSetFile(newRuleSet, newRuleSetPath);

            return newRuleSetPath;
        }

        #region Rule Set Helpers
        internal /*for testing purposes*/ bool TryUpdateExistingProjectRuleSet(string solutionRuleSetPath, string projectRuleSetRootFolder, string currentRuleSet, out string existingRuleSetPath)
        {
            existingRuleSetPath = null;
            if (string.IsNullOrWhiteSpace(currentRuleSet))
            {
                return false;
            }

            existingRuleSetPath = PathHelper.ResolveRelativePath(currentRuleSet, projectRuleSetRootFolder);

            if (this.AlreadyUpdatedExistingRuleSetPaths.Contains(existingRuleSetPath))
            {
                return true;
            }

            RuleSet existingRuleSet = this.SafeLoadRuleSet(existingRuleSetPath);
            if (existingRuleSet != null)
            {
                this.UpdateExistingProjectRuleSet(existingRuleSet, existingRuleSetPath, solutionRuleSetPath);
                this.AlreadyUpdatedExistingRuleSetPaths.Add(existingRuleSetPath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove all rule set inclusions which exist under the specified root directory.
        /// </summary>
        internal /* testing purposes */  static void RemoveAllIncludesUnderRoot(RuleSet ruleSet, string rootDirectory)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(rootDirectory));
            
            // List<T> required as RuleSetIncludes will be modified
            List<RuleSetInclude> toRemove = ruleSet.RuleSetIncludes
                                            .Where(include =>
                                            {
                                                string fullIncludePath = PathHelper.ResolveRelativePath(include.FilePath, rootDirectory);
                                                return PathHelper.IsPathRootedUnderRoot(fullIncludePath, rootDirectory);
                                            })
                                            .ToList();
            toRemove.ForEach(x => ruleSet.RuleSetIncludes.Remove(x));
        }

        /// <summary>
        /// Update the existing project rule set inclusions if required, otherwise do nothing.
        /// </summary>
        /// <param name="existingProjectRuleSet">Existing project level rule set</param>
        /// <param name="projectRuleSetPath">Full path of <paramref name="existingProjectRuleSet"/></param>
        /// <param name="solutionRuleSetPath">Full path of solution level rule set</param>
        /// <returns>Updated <see cref="RuleSet"/> (<paramref name="existingProjectRuleSet"/>) with correct inclusions</returns>
        internal /* testing purposes */ void UpdateExistingProjectRuleSet(RuleSet existingProjectRuleSet, string projectRuleSetPath, string solutionRuleSetPath)
        {
            // Remove all solution level inclusions
            string solutionRuleSetRoot = PathHelper.ForceDirectoryEnding(Path.GetDirectoryName(solutionRuleSetPath));
            RemoveAllIncludesUnderRoot(existingProjectRuleSet, solutionRuleSetRoot);

            // Add correct inclusion
            string expectedIncludePath = PathHelper.CalculateRelativePath(projectRuleSetPath, solutionRuleSetPath);
            existingProjectRuleSet.RuleSetIncludes.Add(new RuleSetInclude(expectedIncludePath, RuleAction.Default));

            this.FileSystem.WriteRuleSetFile(existingProjectRuleSet, projectRuleSetPath);
        }

        /// <summary>
        /// Generate a new project level rule set with the provided inclusions (if not an ignored rule set).
        /// </summary>
        /// <param name="solutionIncludePath">Solution level rule set include path (always included)</param>
        /// <param name="currentRuleSetPath">Original rule set include path (only included if not whitelisted as 'ignore')</param>
        internal /* testing purposes */ static RuleSet GenerateNewProjectRuleSet(string solutionIncludePath, string currentRuleSetPath)
        {
            var ruleSet = new RuleSet(Constants.RuleSetName);
            ruleSet.RuleSetIncludes.Add(new RuleSetInclude(solutionIncludePath, RuleAction.Default));

            // No way to detect whether a ruleset was set by the user or just the default value in case of the
            // default rule set property value. The idea here is that we would like to preserve any explicit setting by the user
            // and we assume that the default rule set can be safely ignored since wasn't set explicitly by the user (or even if it was 
            // it has low value in comparison to what is configured in SQ).
            if (!IsIgnoredRuleSet(currentRuleSetPath))
            {
                ruleSet.RuleSetIncludes.Add(new RuleSetInclude(currentRuleSetPath, RuleAction.Default));
            }
            return ruleSet;
        }

        /// <summary>
        /// Gets whether or not the provided rule set should be ignored for inclusion in a rule set.
        /// </summary>
        internal /* testing purposes */ static bool IsIgnoredRuleSet(string ruleSet)
        {
            return string.IsNullOrWhiteSpace(ruleSet) || StringComparer.OrdinalIgnoreCase.Equals(DefaultProjectRuleSet, ruleSet);
        }

        /// <summary>
        /// Try and load the <see cref="RuleSet"/> from the given full file path.
        /// </summary>
        /// <returns>Rule set or null if does not exist or is malformed</returns>
        internal /* testing purposes */ RuleSet SafeLoadRuleSet(string ruleSetPath)
        {
            if (this.FileSystem.FileExists(ruleSetPath))
            {
                try
                {
                    return this.FileSystem.LoadRuleSet(ruleSetPath);
                }
                catch (Exception ex) when (ex is InvalidRuleSetException || ex is XmlException || ex is IOException)
                {
                    return null;
                }
            }
            return null;
        }

        #endregion

        #region Path Helpers

        /// <summary>
        /// Generate a project level rule set file path from the given project name.
        /// </summary>
        /// <param name="ruleSetRootPath">Root directory path</param>
        /// <param name="projectName">Name of project</param>
        /// <param name="configuration">Project configuration (optional)</param>
        /// <returns>Full unique file path of project level rule set file</returns>
        internal /* testing purposes */ string GenerateNewProjectRuleSetPath(string ruleSetRootPath, string projectName, string configuration)
        {
            configuration = string.IsNullOrWhiteSpace(configuration) ? string.Empty : $".{configuration}";

            string escapedProjectName = PathHelper.EscapeFileName(projectName);
            string escapedConfiguration = PathHelper.EscapeFileName(configuration);

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
                string candidateFileName = $"{escapedProjectName}{uniqueStr}{escapedConfiguration}.{FileExtension}";

                candiatePath = Path.Combine(ruleSetRootPath, candidateFileName);

                // Increment index
                i++;
            } while (this.FileSystem.FileExists(candiatePath));

            return candiatePath;
        }

        #endregion
    }
}

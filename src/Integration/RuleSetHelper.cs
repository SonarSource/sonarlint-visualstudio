//-----------------------------------------------------------------------
// <copyright file="RuleSetHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    internal static class RuleSetHelper
    {
        /// <summary>
        /// Updates the existing project rule set inclusions by deleting all the previous solution includes (determined by solution rule set folder)
        /// </summary>
        /// <remarks>The update is in-memory to the <paramref name="ruleSet"/></remarks>
        /// <param name="existingProjectRuleSet">Existing project level rule set</param>
        /// <param name="projectRuleSetPath">Full path of <paramref name="existingProjectRuleSet"/></param>
        /// <param name="solutionRuleSetPath">Full path of solution level rule set</param>
        public static void UpdateExistingProjectRuleSet(RuleSet ruleSet, string projectRuleSetPath, string solutionRuleSetPath)
        {
            // Remove all solution level inclusions
            string solutionRuleSetRoot = PathHelper.ForceDirectoryEnding(Path.GetDirectoryName(solutionRuleSetPath));
            RuleSetHelper.RemoveAllIncludesUnderRoot(ruleSet, solutionRuleSetRoot);

            // Add correct inclusion
            string expectedIncludePath = PathHelper.CalculateRelativePath(projectRuleSetPath, solutionRuleSetPath);
            ruleSet.RuleSetIncludes.Add(new RuleSetInclude(expectedIncludePath, RuleAction.Default));
        }

        /// <summary>
        /// Remove all rule set inclusions which exist under the specified root directory.
        /// </summary>
        internal /* testing purposes */ static void RemoveAllIncludesUnderRoot(RuleSet ruleSet, string rootDirectory)
        {
            Debug.Assert(ruleSet != null, "RuleSet expected");
            Debug.Assert(!string.IsNullOrWhiteSpace(rootDirectory), "Root directory expected");

            string ruleSetRoot = PathHelper.ForceDirectoryEnding(Path.GetDirectoryName(ruleSet.FilePath));

            // List<T> required as RuleSetIncludes will be modified
            List<RuleSetInclude> toRemove = ruleSet.RuleSetIncludes
                                            .Where(include =>
                                            {
                                                string fullIncludePath = PathHelper.ResolveRelativePath(include.FilePath, ruleSetRoot);
                                                return PathHelper.IsPathRootedUnderRoot(fullIncludePath, rootDirectory);
                                            })
                                            .ToList();
            toRemove.ForEach(x => ruleSet.RuleSetIncludes.Remove(x));
        }
    }
}

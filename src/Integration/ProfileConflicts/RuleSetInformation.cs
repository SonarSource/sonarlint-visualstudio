//-----------------------------------------------------------------------
// <copyright file="RuleSetInformation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Data-only class that represents aggregated rule set information. 
    /// Same rule sets should be represented by the same instance of <see cref="RuleSetInformation"/> and have their <see cref="RuleSetDeclaration.ConfigurationContext"/>
    /// associated with the shared instance by adding them into <see cref="ConfigurationContexts"/>.
    /// </summary>
    /// <seealso cref="RuleSetDeclaration"/>
    public class RuleSetInformation
    {
        public RuleSetInformation(string projectFullName, string baselineRuleSet, string projectRuleSet, IEnumerable<string> ruleSetDirectories)
        {
            if (string.IsNullOrWhiteSpace(projectFullName))
            {
                throw new ArgumentNullException(nameof(projectFullName));
            }

            if (string.IsNullOrWhiteSpace(baselineRuleSet))
            {
                throw new ArgumentNullException(nameof(baselineRuleSet));
            }

            if (string.IsNullOrWhiteSpace(projectRuleSet))
            {
                throw new ArgumentNullException(nameof(projectRuleSet));
            }

            this.RuleSetProjectFullName = projectFullName;
            this.BaselineFilePath = baselineRuleSet;
            this.RuleSetFilePath = projectRuleSet;
            this.RuleSetDirectories = ruleSetDirectories?.ToArray() ?? new string[0];
        }

        public string RuleSetProjectFullName { get; }

        public string BaselineFilePath { get; }

        public string RuleSetFilePath { get; }

        public string[] RuleSetDirectories { get; }

        public HashSet<string> ConfigurationContexts { get; } = new HashSet<string>();
    }
}

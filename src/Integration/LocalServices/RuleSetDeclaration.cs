//-----------------------------------------------------------------------
// <copyright file="RuleSetDeclaration.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Data-only class that represents rule set information for specific configuration
    /// </summary>
    public class RuleSetDeclaration
    {
        public RuleSetDeclaration(Property ruleSetProperty, string ruleSetPath, string activationContext, params string[] ruleSetDirectories)
        {
            if (ruleSetProperty == null)
            {
                throw new ArgumentNullException(nameof(ruleSetProperty));
            }

            this.DeclaringProperty = ruleSetProperty;
            this.ActivationContext = activationContext;
            this.RuleSetPath = ruleSetPath;
            this.RuleSetDirectories = ruleSetDirectories.ToList(); // avoid aliasing bugs
        }

        /// <summary>
        /// The property declaring the rule set
        /// </summary>
        public Property DeclaringProperty { get; }

        /// <summary>
        /// Path to the rule set file. File name, relative path, absolute path and whitespace are all valid.
        /// </summary>
        public string RuleSetPath { get; }

        /// <summary>
        /// Additional rule set directories to search the <see cref="RuleSetPath"/> i.e. in case the rule set is not an absolute path.
        /// </summary>
        public IEnumerable<string> RuleSetDirectories { get; }

        /// <summary>
        /// In which context the ruleset is active i.e. the configuration name
        /// </summary>
        public string ActivationContext { get; }
    }
}

//-----------------------------------------------------------------------
// <copyright file="RuleSetInformation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Data class that exposes simple data that can be accessed from any thread.
    /// The class itself is not thread safe and assumes only one thread accessing it at any given time.
    /// </summary>
    public class RuleSetInformation
    {
        public RuleSetInformation(Language language, RuleSet ruleSet)
        {
            if (ruleSet == null)
            {
                throw new ArgumentNullException(nameof(ruleSet));
            }

            this.RuleSet = ruleSet;
        }

        public RuleSet RuleSet { get; }

        public string NewRuleSetFilePath { get; set; }
    }
}

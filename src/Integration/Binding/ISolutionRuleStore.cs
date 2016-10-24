//-----------------------------------------------------------------------
// <copyright file="ISolutionRuleStore.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Provides access to solution level rules
    /// </summary>
    internal interface ISolutionRuleStore
    {
        /// <summary>
        /// Registers a mapping of <see cref="Language"/> to <see cref="RuleSet"/>.
        /// </summary>
        /// <param name="ruleSets">Required</param>
        void RegisterKnownRuleSets(IDictionary<Language, RuleSet> ruleSets);

        /// <summary>
        /// Retrieves the solution-level <see cref="RuleSet"/> mapped to the <see cref="Language"/>.
        /// </summary>
        RuleSetInformation GetRuleSetInformation(Language language);
    }
}

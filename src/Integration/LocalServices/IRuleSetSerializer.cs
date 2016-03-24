//-----------------------------------------------------------------------
// <copyright file="IRuleSetSerializer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration
{
    // Interface for testing purposes only
    internal interface IRuleSetSerializer
    {
        void WriteRuleSetFile(RuleSet ruleSet, string path);

        RuleSet LoadRuleSet(string path);
    }
}

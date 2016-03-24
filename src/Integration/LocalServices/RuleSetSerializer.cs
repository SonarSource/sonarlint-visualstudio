//-----------------------------------------------------------------------
// <copyright file="RuleSetSerializer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration
{
    internal sealed class RuleSetSerializer : IRuleSetSerializer
    {
        public RuleSet LoadRuleSet(string path)
        {
            return RuleSet.LoadFromFile(path);
        }

        public void WriteRuleSetFile(RuleSet ruleSet, string path)
        {
            ruleSet.WriteToFile(path);
        }
    }
}

//-----------------------------------------------------------------------
// <copyright file="IRuleSetSerializer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Abstraction for reading and writing of <see cref="RuleSet"/> instances.
    /// </summary>
    internal interface IRuleSetSerializer : ILocalService
    {
        /// <summary>
        /// Will write the specified <paramref name="ruleSet"/> into specified path.
        /// The caller needs to handler the various possible errors.
        /// </summary>
        void WriteRuleSetFile(RuleSet ruleSet, string path);

        /// <summary>
        /// Will load a RuleSet in specified <paramref name="path"/>.
        /// In case of error, null will be returned.
        /// </summary>
        RuleSet LoadRuleSet(string path);
    }
}

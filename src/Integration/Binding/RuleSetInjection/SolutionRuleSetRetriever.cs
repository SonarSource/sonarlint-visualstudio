//-----------------------------------------------------------------------
// <copyright file="SolutionRuleSetRetriever.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Binding.RuleSetInjection
{
    /// <summary>
    /// A delegate that will retrieve the solution rule set full file path based on the specified solution, identified by <paramref name="solutionFilePath"/>
    /// </summary>
    /// <param name="group">Rule set group</param>
    /// <param name="solutionFilePath">Not null</param>
    /// <returns>Rule set full file path or null if there is no ruleset.</returns>
    internal delegate string SolutionRuleSetRetriever(RuleSetGroup group, string solutionFilePath);
}

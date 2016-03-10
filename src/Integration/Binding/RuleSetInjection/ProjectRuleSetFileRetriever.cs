//-----------------------------------------------------------------------
// <copyright file="ProjectRuleSetFileRetriever.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Binding.RuleSetInjection
{
    /// <summary>
    /// A delegate that for the specific project, identified by <paramref name="projectFileFullPath"/>, will provide its ruleset file full path
    /// </summary>
    /// <param name="group"><see cref="RuleSetGroup"/></param>
    /// <param name="projectFileFullPath"> Not null</param>
    /// <param name="configurationName">Depending on the specific project implementation of the rule set property,
    /// this will be either the configuring name for which we need to generate a rule set or null if not specific to particular configuration</param>
    /// <param name="currentCodeAnalysisRuleSet">The current code analysis rule set if configured for the project</param>
    /// <returns>Rule set full file path or null if there is no ruleset for the specified <paramref name="project"/>.</returns>
    internal delegate string ProjectRuleSetFileRetriever(RuleSetGroup group, string projectFileFullPath, string configurationName, string currentCodeAnalysisRuleSet);
}

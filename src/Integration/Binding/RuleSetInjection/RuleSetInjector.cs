//-----------------------------------------------------------------------
// <copyright file="RuleSetInjector.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.Binding.RuleSetInjection
{
    /// <summary>
    /// Handles injecting ruleset into VS projects
    /// </summary>
    /// <remarks>
    /// This is class has state which is intendant to be able to split the rule set injection work into several states:
    /// 1. Preprocessing - recording the initial state (on construction)
    /// 2. Generating the file- producing the rule sets and returning the full file paths which will be stored in the object state
    /// 3. Injecting the rule sets - using the state from previous step and translate it back in to VS projects and project properties.
    /// </remarks>
    internal class RuleSetInjector
    {
        private readonly IProjectSystemHelper projectSystemHelper;

        // ------------------------------- Step 1 state ---------------------------------------
        private readonly List<RuleSetHandlerBase> handlers = new List<RuleSetHandlerBase>();
        // ------------------------------- Step 2 state ---------------------------------------
        private readonly Dictionary<string, RuleSetHandlerBase> ruleSetUpdates = new Dictionary<string, RuleSetHandlerBase>(StringComparer.OrdinalIgnoreCase);

        #region State
        internal /*for testing purposes*/ IList<RuleSetHandlerBase> Handlers
        {
            get { return this.handlers; }
        }

        internal /*for testing purposes*/ IReadOnlyDictionary<string, RuleSetHandlerBase> Updates
        {
            get { return this.ruleSetUpdates; }
        }
        #endregion

        #region Step 1
        public RuleSetInjector(IProjectSystemHelper projectSystemHelper, SolutionRuleSetRetriever solutionRuleSetRetriever, ProjectRuleSetFileRetriever projectRuleSetRetriever)
        {
            if (projectSystemHelper == null)
            {
                throw new ArgumentNullException(nameof(projectSystemHelper));
            }

            if (solutionRuleSetRetriever == null)
            {
                throw new ArgumentNullException(nameof(solutionRuleSetRetriever));
            }

            if (projectRuleSetRetriever == null)
            {
                throw new ArgumentNullException(nameof(projectRuleSetRetriever));
            }

            this.projectSystemHelper = projectSystemHelper;

            this.InitializeHandlers(solutionRuleSetRetriever, projectRuleSetRetriever);
        }

        private void InitializeHandlers(SolutionRuleSetRetriever solutionRuleSetRetriever, ProjectRuleSetFileRetriever projectRuleSetRetriever)
        {
            HashSet<RuleSetGroup> requiredGroups = new HashSet<RuleSetGroup>();
            foreach (var project in this.projectSystemHelper.GetSolutionManagedProjects())
            {
                foreach (var property in VsShellUtils.EnumerateProjectProperties(project, Constants.CodeAnalysisRuleSetPropertyKey))
                {
                    if (property == null)
                    {
                        VsShellUtils.WriteToGeneralOutputPane(this.projectSystemHelper.ServiceProvider, Strings.FailedToSetCodeAnalysisRuleSetMessage, project.UniqueName);
                    }
                    else
                    {
                        var handler = new ProjectRuleSetHandler(this.projectSystemHelper, project, TryGetPropertyConfiguration(property), property, projectRuleSetRetriever);
                        requiredGroups.Add(handler.ThreadSafeData.Group);
                        this.handlers.Add(handler);
                    }
                }
            }

            if (this.handlers.Count > 0)
            {
                // When we don't have any project we will not need a solution full file path
                Solution solution = this.handlers.OfType<ProjectRuleSetHandler>()
                    .Select(h => h.Project.DTE.Solution)
                    .First();
                Debug.Assert(solution != null);

                // We expect the solution rule sets to be first
                foreach (var group in requiredGroups)
                {
                    this.handlers.Insert(0, new SolutionRuleSetHandler(group, this.projectSystemHelper, solution, solutionRuleSetRetriever));
                }
            }
        }

        #endregion

        #region Step 2
        public void PrepareUpdates(CancellationToken token)
        {
            Debug.Assert(this.handlers.Any(), "Not expecting to be called in case there are no handler");

            foreach(var handler in handlers)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                string ruleSetFullFilePath = handler.GetUpdatedRuleSet();
                if (!string.IsNullOrWhiteSpace(ruleSetFullFilePath))
                {
                    this.ruleSetUpdates[ruleSetFullFilePath] = handler;
                }
            }
        }
        #endregion

        #region Step 3
        public void CommitUpdates()
        {
            foreach(var update in this.Updates)
            {
                string addRuleSetFile = update.Key;
                RuleSetHandlerBase handler = update.Value;
                handler.CommitRuleSet(update.Key);
            }
        }
        #endregion

        #region Static helpers
        private static Configuration TryGetPropertyConfiguration(Property property)
        {
            Configuration configuration = property.Collection.Parent as Configuration; // Could be null if the one used is the Project level one.
            Debug.Assert(configuration != null || property.Collection.Parent is Project, $"Unexpected property parent type: {property.Collection.Parent.GetType().FullName}");
            return configuration;
        }

        #endregion
    }
}

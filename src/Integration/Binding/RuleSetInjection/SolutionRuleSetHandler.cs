//-----------------------------------------------------------------------
// <copyright file="SolutionRuleSetHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Binding.RuleSetInjection
{
    internal class SolutionRuleSetHandler : RuleSetHandlerBase, SolutionRuleSetHandler.IThreadSafeData
    {
        private readonly SolutionRuleSetRetriever solutionRuleSetRetriever;
        private readonly string solutionFullName;
        private readonly RuleSetGroup group;

        public SolutionRuleSetHandler(RuleSetGroup group, IProjectSystemHelper projectSystemHelper, Solution solution, SolutionRuleSetRetriever solutionRuleSetRetriever)
            : base(projectSystemHelper)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (solutionRuleSetRetriever == null)
            {
                throw new ArgumentNullException(nameof(solutionRuleSetRetriever));
            }

            this.solutionRuleSetRetriever = solutionRuleSetRetriever;

            this.Solution = solution;
            this.solutionFullName = solution.FullName;
            this.group = group;
        }

        #region UI-thread properties
        public Solution Solution
        {
            get;
        }
        #endregion

        #region IThreadSafeData
        internal IThreadSafeData ThreadSafeData
        {
            get { return this; }
        }

        string IThreadSafeData.SolutionFullPath
        {
            get { return this.solutionFullName; }
        }
        #endregion

        #region Overrides
        protected override string GetUpdatedRuleSetCore()
        {
            return this.solutionRuleSetRetriever(this.group, this.ThreadSafeData.SolutionFullPath);
        }

        protected override void CommitRuleSetCore(string ruleSetFullFilePath)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(ruleSetFullFilePath), "Invalid rule set file");

            Project solutionItemsProject = this.ProjectSystemHelper.GetSolutionItemsProject();
            if (solutionItemsProject == null)
            {
                Debug.Fail("Could not find the solution items project");
            }
            else
            {
                this.AddFileToProject(solutionItemsProject, ruleSetFullFilePath);
            }
        }
        #endregion

        internal interface IThreadSafeData
        {
            /// <summary>
            /// Required project full path
            /// </summary>
            string SolutionFullPath { get; }
        }
    }
}

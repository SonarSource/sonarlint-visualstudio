//-----------------------------------------------------------------------
// <copyright file="RuleSetHandlerBase.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System.Diagnostics;
using System.IO;
using System;

namespace SonarLint.VisualStudio.Integration.Binding.RuleSetInjection
{
    internal abstract class RuleSetHandlerBase
    {
        public RuleSetHandlerBase(IProjectSystemHelper projectSystemHelper)
        {
            if (projectSystemHelper == null)
            {
                throw new ArgumentNullException(nameof(projectSystemHelper));
            }
            this.ProjectSystemHelper = projectSystemHelper;
        }

        protected IProjectSystemHelper ProjectSystemHelper
        {
            get;
        }

        public string GetUpdatedRuleSet()
        {
            return this.GetUpdatedRuleSetCore();
        }

        public void CommitRuleSet(string ruleSetFullFilePath)
        {
            this.CommitRuleSetCore(ruleSetFullFilePath);
        }

        protected abstract void CommitRuleSetCore(string ruleSetFullFilePath);

        protected abstract string GetUpdatedRuleSetCore();

        protected void AddFileToProject(Project project, string fullFilePath)
        {
            Debug.Assert(Path.IsPathRooted(fullFilePath) && File.Exists(fullFilePath), "Expecting a rooted path to existing file");

            if (!this.ProjectSystemHelper.IsFileInProject(project, fullFilePath))
            {
                this.ProjectSystemHelper.AddFileToProject(project, fullFilePath);
            }
        }
    }
}

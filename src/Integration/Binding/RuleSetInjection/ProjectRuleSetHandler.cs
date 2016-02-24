//-----------------------------------------------------------------------
// <copyright file="ProjectRuleSetHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Binding.RuleSetInjection
{
    internal class ProjectRuleSetHandler : RuleSetHandlerBase, ProjectRuleSetHandler.IThreadSafeData
    {
        private readonly ProjectRuleSetFileRetriever projectRuleSetRetriever;
        private readonly string projectFullName;
        private readonly string configurationName;
        private readonly string currentCodeAnalysisRuleSet;
        private readonly RuleSetGroup group;

        public ProjectRuleSetHandler(IProjectSystemHelper projectSystemHelper, Project project, Configuration configuration, Property codeAnalysisRuleSetProperty, ProjectRuleSetFileRetriever projectRuleSetRetriever)
            : base(projectSystemHelper)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (codeAnalysisRuleSetProperty == null)
            {
                throw new ArgumentNullException(nameof(codeAnalysisRuleSetProperty));
            }

            if (projectRuleSetRetriever == null)
            {
                throw new ArgumentNullException(nameof(projectRuleSetRetriever));
            }

            this.projectRuleSetRetriever = projectRuleSetRetriever;

            this.Project = project;
            this.projectFullName = project.FullName;

            this.Configuration = configuration;
            this.configurationName = configuration?.ConfigurationName;

            this.CodeAnalysisRuleSetProperty = codeAnalysisRuleSetProperty;
            this.currentCodeAnalysisRuleSet = codeAnalysisRuleSetProperty.Value as string;

            Debug.Assert(Integration.ProjectSystemHelper.IsCSharpProject(project) || Integration.ProjectSystemHelper.IsVBProject(project), "Unexpected project kind");
            this.group = Integration.ProjectSystemHelper.IsCSharpProject(project) ? RuleSetGroup.CSharp : RuleSetGroup.VB;
        }

        public void UpdatePropertyValue(string ruleSetFullFilePath)
        {
            string updatedRuleSetValue = PathHelper.CalculateRelativePath(this.ThreadSafeData.ProjectFullPath, ruleSetFullFilePath);
            this.CodeAnalysisRuleSetProperty.Value = updatedRuleSetValue;
        }

        #region UI-thread properties
        public Property CodeAnalysisRuleSetProperty
        {
            get;
        }

        public Project Project
        {
            get;
        }

        public Configuration Configuration
        {
            get;
        }
        #endregion  

        #region IThreadSafeData
        internal IThreadSafeData ThreadSafeData
        {
            get { return this; }
        }

        string IThreadSafeData.CodeAnalysisRuleSetPropertyValue
        {
            get { return this.currentCodeAnalysisRuleSet; }
        }

        string IThreadSafeData.ProjectFullPath
        {
            get { return this.projectFullName; }
        }

        string IThreadSafeData.ConfigurationName
        {
            get { return this.configurationName; }
        }

        RuleSetGroup IThreadSafeData.Group
        {
            get { return this.group; }
        }
        #endregion

        #region Overrides
        protected override string GetUpdatedRuleSetCore()
        {
            return this.projectRuleSetRetriever(this.ThreadSafeData.Group, this.ThreadSafeData.ProjectFullPath, this.ThreadSafeData.ConfigurationName, this.currentCodeAnalysisRuleSet);
        }

        protected override void CommitRuleSetCore(string ruleSetFullFilePath)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(ruleSetFullFilePath), "Invalid rule set file");

            string updatedRuleSetValue = PathHelper.CalculateRelativePath(this.ThreadSafeData.ProjectFullPath, ruleSetFullFilePath);
            this.CodeAnalysisRuleSetProperty.Value = updatedRuleSetValue;
            this.AddFileToProject(this.Project, ruleSetFullFilePath);
        }
        #endregion

        internal interface IThreadSafeData
        {
            /// <summary>
            /// The current value for the CodeAnalysisRuleSet property
            /// </summary>
            string CodeAnalysisRuleSetPropertyValue { get; }

            /// <summary>
            /// Required project full path
            /// </summary>
            string ProjectFullPath { get; }

            /// <summary>
            /// Optional configuration name
            /// </summary>
            string ConfigurationName { get; }

            /// <summary>
            /// Rule set association
            /// </summary>
            RuleSetGroup Group { get; }
        }
    }
}

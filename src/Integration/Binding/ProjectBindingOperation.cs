//-----------------------------------------------------------------------
// <copyright file="ProjectBindingOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal partial class ProjectBindingOperation : IBindingOperation
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ISourceControlledFileSystem sourceControlledFileSystem;
        private readonly ISolutionRuleStore ruleStore;
        private readonly IProjectSystemHelper projectSystem;

        private readonly Dictionary<Property, PropertyInformation> propertyInformationMap = new Dictionary<Property, PropertyInformation>();
        private readonly Project initializedProject;

        public ProjectBindingOperation(IServiceProvider serviceProvider, ISourceControlledFileSystem sccFileSystem, Project project, IProjectSystemHelper projectSystem, ISolutionRuleStore ruleStore)
            :this(serviceProvider, sccFileSystem, project, projectSystem, ruleStore, null)
        {
            
        }

        internal /*for testing purposes*/ ProjectBindingOperation(IServiceProvider serviceProvider, ISourceControlledFileSystem sccFileSystem, Project project, IProjectSystemHelper projectSystem, ISolutionRuleStore ruleStore, IRuleSetFileSystem rsFileSystem)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (sccFileSystem == null)
            {
                throw new ArgumentNullException(nameof(sccFileSystem));
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (projectSystem == null)
            {
                throw new ArgumentNullException(nameof(projectSystem));
            }

            if (ruleStore == null)
            {
                throw new ArgumentNullException(nameof(ruleStore));
            }

            this.serviceProvider = serviceProvider;
            this.sourceControlledFileSystem = sccFileSystem;
            this.initializedProject = project;
            this.ruleStore = ruleStore;
            this.projectSystem = projectSystem;
            this.ruleSetFileSystem = rsFileSystem ?? new RuleSetFileSystem();
        }


        #region State
        internal /*for testing purposes*/ RuleSetGroup ProjectGroup { get; private set; }

        internal /*for testing purposes*/ string ProjectFullPath { get; private set; }

        internal /*for testing purposes*/ IReadOnlyDictionary<Property, PropertyInformation> PropertyInformationMap { get { return this.propertyInformationMap; } }
        #endregion

        #region IBindingOperation
        public void Initialize()
        {
            this.CaptureProjectInformation();
            this.CalculateRuleSetInformation();
        }

        public void Prepare(CancellationToken token)
        {
            string solutionRuleSetPath = this.ruleStore.GetRuleSetFilePath(this.ProjectGroup);

            // We want to limit the number of rulesets so for this we use the previously calculated TargetRuleSetFileName
            // and group by it. This handles the special case of all the properties having the same ruleset and also the case
            // in which the user didn't configure anything and we're getting only default value from the properties.
            foreach (IGrouping<string, PropertyInformation> group in this.propertyInformationMap.Values.GroupBy(info=>info.TargetRuleSetFileName))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                string targetRuleSetFileName = group.Key;
                string currentRuleSetFilePath = group.First().CurrentRuleSetFilePath;
                Debug.Assert(group.All(i => StringComparer.OrdinalIgnoreCase.Equals(currentRuleSetFilePath, currentRuleSetFilePath)), "Expected all the rulesets to be the same when the target rule set name is the same");
                string newRuleSetFilePath = this.PendWriteProjectLevelRuleSet(this.ProjectFullPath, targetRuleSetFileName, solutionRuleSetPath, currentRuleSetFilePath);

                foreach (PropertyInformation info in group)
                {
                    info.NewRuleSetFilePath = newRuleSetFilePath;
                }
            }
        }

        public void Commit()
        {
            foreach (var keyValue in this.propertyInformationMap)
            {
                Property property = keyValue.Key;
                string ruleSetFullFilePath = keyValue.Value.NewRuleSetFilePath;

                Debug.Assert(!string.IsNullOrWhiteSpace(ruleSetFullFilePath), "Prepare was not called");
                Debug.Assert(this.sourceControlledFileSystem.IsFileExist(ruleSetFullFilePath), "File not written: " + ruleSetFullFilePath);

                string updatedRuleSetValue = PathHelper.CalculateRelativePath(this.ProjectFullPath, ruleSetFullFilePath);
                property.Value = updatedRuleSetValue;

                this.AddFileToProject(this.initializedProject, ruleSetFullFilePath);
            }
        }
        #endregion

        #region Helpers
        private void CaptureProjectInformation()
        {
            Debug.Assert(Integration.ProjectSystemHelper.IsCSharpProject(this.initializedProject) || Integration.ProjectSystemHelper.IsVBProject(this.initializedProject), "Unexpected project kind");
            this.ProjectGroup = Integration.ProjectSystemHelper.IsCSharpProject(this.initializedProject) ? RuleSetGroup.CSharp : RuleSetGroup.VB;
            this.ProjectFullPath = this.initializedProject.FullName;
        }

        private void CalculateRuleSetInformation()
        {
            Property[] properties = VsShellUtils.EnumerateProjectProperties(this.initializedProject, Constants.CodeAnalysisRuleSetPropertyKey).ToArray();
            string currentRuleSetValue = properties.FirstOrDefault()?.Value?.ToString();

            // Special case: if all the values are the same use project name as the target ruleset name
            bool useSameTargetName = false;
            if (properties.All(p=>StringComparer.OrdinalIgnoreCase.Equals(currentRuleSetValue, p.Value as string)))
            {
                useSameTargetName = true;
            }

            string projectBasedRuleSetName = Path.GetFileNameWithoutExtension(this.initializedProject.FullName);
            foreach (Property codeAnalysisRuleProperty in properties)
            {
                if (codeAnalysisRuleProperty == null)
                {
                    VsShellUtils.WriteToGeneralOutputPane(this.serviceProvider, Strings.FailedToSetCodeAnalysisRuleSetMessage, this.initializedProject.UniqueName);
                }
                else
                {
                    string targetRuleSetName = projectBasedRuleSetName;
                    currentRuleSetValue = codeAnalysisRuleProperty.Value as string;
                    if (!useSameTargetName && !ShouldIgnoreConfigureRuleSetValue(currentRuleSetValue))
                    {
                        targetRuleSetName = string.Join(".", targetRuleSetName, TryGetPropertyConfiguration(codeAnalysisRuleProperty)?.ConfigurationName ?? string.Empty);
                    }
                    this.propertyInformationMap[codeAnalysisRuleProperty] = new PropertyInformation(targetRuleSetName, currentRuleSetValue);
                }
            }
        }

        private void AddFileToProject(Project project, string fullFilePath)
        {
            Debug.Assert(Path.IsPathRooted(fullFilePath) && File.Exists(fullFilePath), "Expecting a rooted path to existing file");

            if (!this.projectSystem.IsFileInProject(project, fullFilePath))
            {
                this.projectSystem.AddFileToProject(project, fullFilePath);
            }
        }

        private static Configuration TryGetPropertyConfiguration(Property property)
        {
            Configuration configuration = property.Collection.Parent as Configuration; // Could be null if the one used is the Project level one.
            Debug.Assert(configuration != null || property.Collection.Parent is Project, $"Unexpected property parent type: {property.Collection.Parent.GetType().FullName}");
            return configuration;
        }

        /// <summary>
        /// Data class that exposes simple data that can be accessed from any thread.
        /// The class itself is not thread safe and assumes only one thread accessing it at any given time.
        /// </summary>
        internal class PropertyInformation
        {
            public PropertyInformation(string targetRuleSetName, string currentRuleSetFilePath)
            {
                if (string.IsNullOrWhiteSpace(targetRuleSetName))
                {
                    throw new ArgumentNullException(nameof(targetRuleSetName));
                }

                this.TargetRuleSetFileName = targetRuleSetName;
                this.CurrentRuleSetFilePath = currentRuleSetFilePath;
            }

            public string TargetRuleSetFileName { get; }

            public string CurrentRuleSetFilePath { get; }

            public string NewRuleSetFilePath { get; set; }

        }
        #endregion
    }
}
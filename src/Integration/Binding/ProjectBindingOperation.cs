/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal partial class ProjectBindingOperation : IBindingOperation
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ISourceControlledFileSystem sourceControlledFileSystem;
        private readonly ISolutionRuleStore ruleStore;

        private readonly Dictionary<Property, PropertyInformation> propertyInformationMap = new Dictionary<Property, PropertyInformation>();
        private readonly Project initializedProject;

        public ProjectBindingOperation(IServiceProvider serviceProvider, Project project, ISolutionRuleStore ruleStore)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (ruleStore == null)
            {
                throw new ArgumentNullException(nameof(ruleStore));
            }

            this.serviceProvider = serviceProvider;
            this.initializedProject = project;
            this.ruleStore = ruleStore;

            this.sourceControlledFileSystem = this.serviceProvider.GetService<ISourceControlledFileSystem>();
            this.sourceControlledFileSystem.AssertLocalServiceIsNotNull();

            this.ruleSetSerializer = this.serviceProvider.GetService<IRuleSetSerializer>();
            this.ruleSetSerializer.AssertLocalServiceIsNotNull();
        }

        #region State
        internal /*for testing purposes*/ Language ProjectLanguage { get; private set; }

        internal /*for testing purposes*/ string ProjectFullPath { get; private set; }

        internal /*for testing purposes*/ IReadOnlyDictionary<Property, PropertyInformation> PropertyInformationMap { get { return this.propertyInformationMap; } }
        #endregion

        #region IBindingOperation
        public void Initialize()
        {
            this.CaptureSonarQubeProject();
            this.CalculateRuleSetInformation();
        }

        public void Prepare(CancellationToken token)
        {
            var solutionRuleSet = this.ruleStore.GetRuleSetInformation(this.ProjectLanguage);

            // We want to limit the number of rulesets so for this we use the previously calculated TargetRuleSetFileName
            // and group by it. This handles the special case of all the properties having the same ruleset and also the case
            // in which the user didn't configure anything and we're getting only default value from the properties.
            foreach (IGrouping<string, PropertyInformation> group in this.propertyInformationMap.Values.GroupBy(info => info.TargetRuleSetFileName))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                string targetRuleSetFileName = group.Key;
                string currentRuleSetFilePath = group.First().CurrentRuleSetFilePath;
                Debug.Assert(group.All(i => StringComparer.OrdinalIgnoreCase.Equals(currentRuleSetFilePath, currentRuleSetFilePath)), "Expected all the rulesets to be the same when the target rule set name is the same");
                string newRuleSetFilePath = this.QueueWriteProjectLevelRuleSet(this.ProjectFullPath, targetRuleSetFileName, solutionRuleSet, currentRuleSetFilePath);

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
                Debug.Assert(this.sourceControlledFileSystem.FileExist(ruleSetFullFilePath), "File not written: " + ruleSetFullFilePath);

                string updatedRuleSetValue = PathHelper.CalculateRelativePath(this.ProjectFullPath, ruleSetFullFilePath);
                property.Value = updatedRuleSetValue;

                this.AddFileToProject(this.initializedProject, ruleSetFullFilePath);
            }
        }
        #endregion

        #region Helpers

        private void CaptureSonarQubeProject()
        {
            this.ProjectLanguage = Language.ForProject(this.initializedProject);
            this.ProjectFullPath = this.initializedProject.FullName;
        }

        private void CalculateRuleSetInformation()
        {
            var solutionRuleSetProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            RuleSetDeclaration[] ruleSetsInfo = solutionRuleSetProvider.GetProjectRuleSetsDeclarations(this.initializedProject).ToArray();

            string sameRuleSetCandidate = ruleSetsInfo.FirstOrDefault()?.RuleSetPath;

            // Special case: if all the values are the same use project name as the target ruleset name
            bool useSameTargetName = false;
            if (ruleSetsInfo.All(r => StringComparer.OrdinalIgnoreCase.Equals(sameRuleSetCandidate, r.RuleSetPath)))
            {
                useSameTargetName = true;
            }

            string projectBasedRuleSetName = Path.GetFileNameWithoutExtension(this.initializedProject.FullName);
            foreach (RuleSetDeclaration singleRuleSetInfo in ruleSetsInfo)
            {
                string targetRuleSetName = projectBasedRuleSetName;
                string currentRuleSetValue = useSameTargetName ? sameRuleSetCandidate : singleRuleSetInfo.RuleSetPath;

                if (!useSameTargetName && !ShouldIgnoreConfigureRuleSetValue(currentRuleSetValue))
                {
                    targetRuleSetName = string.Join(".", targetRuleSetName, singleRuleSetInfo.ConfigurationContext);
                }

                this.propertyInformationMap[singleRuleSetInfo.DeclaringProperty] = new PropertyInformation(targetRuleSetName, currentRuleSetValue);
            }
        }

        private void AddFileToProject(Project project, string fullFilePath)
        {
            Debug.Assert(Path.IsPathRooted(fullFilePath) && File.Exists(fullFilePath), "Expecting a rooted path to existing file");

            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            if (!projectSystem.IsFileInProject(project, fullFilePath))
            {
                projectSystem.AddFileToProject(project, fullFilePath);
            }
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
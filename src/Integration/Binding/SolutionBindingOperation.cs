//-----------------------------------------------------------------------
// <copyright file="SolutionBindingOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Solution level binding by delegating some of the work to <see cref="ProjectBindingOperation"/>
    /// </summary>
    internal class SolutionBindingOperation : ISolutionRuleStore
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IProjectSystemHelper projectSystem;
        private readonly SolutionRuleSetWriter solutionRuleSetWriter;
        private readonly List<IBindingOperation> childBinder = new List<IBindingOperation>();
        private readonly Dictionary<RuleSetGroup, RuleSetInformation> ruleSetsInformationMap = new Dictionary<RuleSetGroup, RuleSetInformation>();

        public SolutionBindingOperation(IServiceProvider serviceProvider, IProjectSystemHelper projectSystemHelper, string sonarQubeProjectKey)
            : this(serviceProvider, projectSystemHelper, sonarQubeProjectKey, null)
        {
        }

        internal SolutionBindingOperation(IServiceProvider serviceProvider, IProjectSystemHelper projectSystemHelper, string sonarQubeProjectKey, SolutionRuleSetWriter solutionRuleSetWriter)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (projectSystemHelper == null)
            {
                throw new ArgumentNullException(nameof(projectSystemHelper));
            }

            if (string.IsNullOrWhiteSpace(sonarQubeProjectKey))
            {
                throw new ArgumentNullException(nameof(sonarQubeProjectKey));
            }

            this.serviceProvider = serviceProvider;
            this.projectSystem = projectSystemHelper;
            this.solutionRuleSetWriter = solutionRuleSetWriter ?? new SolutionRuleSetWriter(sonarQubeProjectKey);

        }

        #region State
        internal /*for testing purposes*/ IList<IBindingOperation> Binders
        {
            get { return this.childBinder; }
        }

        internal /*for testing purposes*/ string SolutionFullPath
        {
            get;
            private set;
        }

        internal /*for testing purposes*/ IReadOnlyDictionary<RuleSetGroup, RuleSetInformation> RuleSetsInformationMap
        {
            get { return this.ruleSetsInformationMap; }
        }
        #endregion

        #region ISolutionRuleStore

        public void RegisterKnownRuleSets(IDictionary<RuleSetGroup, RuleSet> ruleSets)
        {
            if (ruleSets == null)
            {
                throw new ArgumentNullException(nameof(ruleSets));
            }

            foreach (var keyValue in ruleSets)
            {
                Debug.Assert(!this.ruleSetsInformationMap.ContainsKey(keyValue.Key), "Attempted to register an already registered rule set. Group:" + keyValue.Key);
                this.ruleSetsInformationMap[keyValue.Key] = new RuleSetInformation(keyValue.Key, keyValue.Value);
            }
        }

        public string GetRuleSetFilePath(RuleSetGroup group)
        {
            RuleSetInformation info;

            if (!this.ruleSetsInformationMap.TryGetValue(group, out info) || info == null)
            {
                Debug.Fail("Expected to be called by the ProjectBinder after the known rulesets were registered");
                return null;
            }

            Debug.Assert(info.NewRuleSetFilePath != null, "Expected to be called after Prepare");

            // At this point we know that the rule set is needed, so we write it to disk (internally will only write the file once)
            return info.NewRuleSetFilePath;
        }

        #endregion

        #region IBindingOperation
        public void Initialize()
        {
            this.SolutionFullPath = this.projectSystem.GetCurrentActiveSolution().FullName;

            foreach (Project project in this.projectSystem.GetSolutionManagedProjects())
            {
                var binder = new ProjectBindingOperation(serviceProvider, project, this.projectSystem, this);
                binder.Initialize();
                this.childBinder.Add(binder);
            }
        }

        public void Prepare(CancellationToken token)
        {
            Debug.Assert(this.SolutionFullPath != null, "Expected to be initialized");

            foreach (var keyValue in this.ruleSetsInformationMap)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                RuleSetInformation info = keyValue.Value;
                RuleSetGroup group = keyValue.Key;
                Debug.Assert(info.NewRuleSetFilePath == null, "Not expected to be called twice");

                info.NewRuleSetFilePath = this.solutionRuleSetWriter.WriteSolutionLevelRuleSet(this.SolutionFullPath, info.RuleSet, fileNameSuffix: group.ToString());
            }

            foreach (IBindingOperation binder in this.childBinder)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                binder.Prepare(token);
            }
        }

        public void Commit()
        {
            this.childBinder.ForEach(b => b.Commit());

            foreach(RuleSetInformation info in ruleSetsInformationMap.Values)
            {
                this.AddFileToSolutionItems(info.NewRuleSetFilePath);
            }
        }
        #endregion

        #region Helpers
        private void AddFileToSolutionItems(string fullFilePath)
        {
            Debug.Assert(Path.IsPathRooted(fullFilePath) && File.Exists(fullFilePath), "Expecting a rooted path to existing file");

            Project solutionItemsProject = this.projectSystem.GetSolutionItemsProject();
            if (solutionItemsProject == null)
            {
                Debug.Fail("Could not find the solution items project");
            }
            else
            {
                if (!this.projectSystem.IsFileInProject(solutionItemsProject, fullFilePath))
                {
                    this.projectSystem.AddFileToProject(solutionItemsProject, fullFilePath);
                }
            }
        }

        /// <summary>
        /// Data class that exposes simple data that can be accessed from any thread.
        /// The class itself is not thread safe and assumes only one thread accessing it at any given time.
        /// </summary>
        internal class RuleSetInformation
        {
            public RuleSetInformation(RuleSetGroup group, RuleSet ruleSet)
            {
                if (ruleSet == null)
                {
                    throw new ArgumentNullException(nameof(ruleSet));
                }

                this.RuleSet = ruleSet;
            }

            public RuleSet RuleSet { get; }

            public string NewRuleSetFilePath { get; set; }
        }
        #endregion
    }
}


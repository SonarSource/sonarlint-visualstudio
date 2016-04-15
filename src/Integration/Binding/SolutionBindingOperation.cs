//-----------------------------------------------------------------------
// <copyright file="SolutionBindingOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Service;
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
        private readonly ISourceControlledFileSystem sourceControlledFileSystem;
        private readonly IProjectSystemHelper projectSystem;
        private readonly List<IBindingOperation> childBinder = new List<IBindingOperation>();
        private readonly Dictionary<RuleSetGroup, RuleSetInformation> ruleSetsInformationMap = new Dictionary<RuleSetGroup, RuleSetInformation>();
        private readonly ConnectionInformation connection;
        private readonly string sonarQubeProjectKey;

        public SolutionBindingOperation(IServiceProvider serviceProvider, ConnectionInformation connection, string sonarQubeProjectKey)
        { 
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(sonarQubeProjectKey))
            {
                throw new ArgumentNullException(nameof(sonarQubeProjectKey));
            }

            this.serviceProvider = serviceProvider;
            this.connection = connection;
            this.sonarQubeProjectKey = sonarQubeProjectKey;

            this.projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.sourceControlledFileSystem = this.serviceProvider.GetService<ISourceControlledFileSystem>();
            this.sourceControlledFileSystem.AssertLocalServiceIsNotNull();
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

            var ruleSetInfo = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfo.AssertLocalServiceIsNotNull();

            foreach (var keyValue in ruleSets)
            {
                Debug.Assert(!this.ruleSetsInformationMap.ContainsKey(keyValue.Key), "Attempted to register an already registered rule set. Group:" + keyValue.Key);

                string solutionRuleSet = ruleSetInfo.CalculateSolutionSonarQubeRuleSetFilePath(this.sonarQubeProjectKey, GetProjectRuleSetSuffix(keyValue.Key));
                this.ruleSetsInformationMap[keyValue.Key] = new RuleSetInformation(keyValue.Key, keyValue.Value) { NewRuleSetFilePath = solutionRuleSet };
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
            return info.NewRuleSetFilePath;
        }

        #endregion

        #region Public API
        public void Initialize(IEnumerable<Project> projects)
        {
            if (projects == null)
            {
                throw new ArgumentNullException(nameof(projects));
            }

            this.SolutionFullPath = this.projectSystem.GetCurrentActiveSolution().FullName;

            foreach (Project project in projects)
            {
                var binder = new ProjectBindingOperation(serviceProvider, project, this);
                binder.Initialize();
                this.childBinder.Add(binder);
            }
        }

        public void Prepare(CancellationToken token)
        {
            Debug.Assert(this.SolutionFullPath != null, "Expected to be initialized");

            var ruleSetSerializer = this.serviceProvider.GetService<IRuleSetSerializer>();
            ruleSetSerializer.AssertLocalServiceIsNotNull();

            foreach (var keyValue in this.ruleSetsInformationMap)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                RuleSetInformation info = keyValue.Value;
                Debug.Assert(!string.IsNullOrWhiteSpace(info.NewRuleSetFilePath), "Expected to be set during registration time");

                this.sourceControlledFileSystem.QueueFileWrite(info.NewRuleSetFilePath, () =>
                {
                    string ruleSetDirectoryPath = Path.GetDirectoryName(info.NewRuleSetFilePath);

                    this.sourceControlledFileSystem.CreateDirectory(ruleSetDirectoryPath); // will no-op if exists

                    // Create or overwrite existing rule set
                    ruleSetSerializer.WriteRuleSetFile(info.RuleSet, info.NewRuleSetFilePath);

                    return true;
                });

                Debug.Assert(this.sourceControlledFileSystem.FileExistOrQueuedToBeWritten(info.NewRuleSetFilePath), "Expected a rule set to pend pended");
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

        public bool CommitSolutionBinding()
        {
            this.PendBindingInformation(this.connection); // This is the last pend, so will be executed last

            if (this.sourceControlledFileSystem.WriteQueuedFiles())
            {
                // No reason to modify VS state if could not write files

                this.childBinder.ForEach(b => b.Commit());

                foreach (RuleSetInformation info in ruleSetsInformationMap.Values)
                {
                    Debug.Assert(this.sourceControlledFileSystem.FileExist(info.NewRuleSetFilePath), "File not written " + info.NewRuleSetFilePath);
                    this.AddFileToSolutionItems(info.NewRuleSetFilePath);
                }

                return true;
            }

            return false;
        }


        #endregion

        #region Helpers
        /// <summary>
        /// Will bend add/edit the binding information for next time usage
        /// </summary>
        private void PendBindingInformation(ConnectionInformation connInfo)
        {
            var binding = this.serviceProvider.GetService<ISolutionBinding>();
            binding.AssertLocalServiceIsNotNull();

            BasicAuthCredentials credentials = connection.UserName == null ? null : new BasicAuthCredentials(connInfo.UserName, connInfo.Password);
            binding.WriteSolutionBinding(new BoundSonarQubeProject(connInfo.ServerUri, this.sonarQubeProjectKey, credentials));
        }

        private void AddFileToSolutionItems(string fullFilePath)
        {
            Debug.Assert(Path.IsPathRooted(fullFilePath) && this.sourceControlledFileSystem.FileExist(fullFilePath), "Expecting a rooted path to existing file");

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

        #region Static helper

        public static string GetProjectRuleSetSuffix(RuleSetGroup group)
        {
            return group.ToString();
        }
        #endregion  
    }
}

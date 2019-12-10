/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Threading;
using EnvDTE;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    // Legacy connected mode:
    // * writes the binding info files to disk and adds them as solution items.
    // * co-ordinates writing project-level changes

    /// <summary>
    /// Solution level binding by delegating some of the work to <see cref="ProjectBindingOperation"/>
    /// </summary>
    internal class SolutionBindingOperation : ISolutionBindingOperation
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ISourceControlledFileSystem sourceControlledFileSystem;
        private readonly IProjectSystemHelper projectSystem;
        private readonly List<IBindingOperation> childBinder = new List<IBindingOperation>();
        private readonly Dictionary<Language, RuleSetInformation> rulesConfigInformationMap = new Dictionary<Language, RuleSetInformation>();
        private Dictionary<Language, SonarQubeQualityProfile> qualityProfileMap;
        private readonly ConnectionInformation connection;
        private readonly string projectKey;
        private readonly string projectName;
        private readonly SonarLintMode bindingMode;
        private readonly ILogger logger;

        public SolutionBindingOperation(IServiceProvider serviceProvider, ConnectionInformation connection, string projectKey, string projectName, SonarLintMode bindingMode,
            ILogger logger)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrWhiteSpace(projectKey))
            {
                throw new ArgumentNullException(nameof(projectKey));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            bindingMode.ThrowIfNotConnected();

            this.serviceProvider = serviceProvider;
            this.connection = connection;
            this.projectKey = projectKey;
            this.projectName = projectName;

            this.projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.sourceControlledFileSystem = this.serviceProvider.GetService<ISourceControlledFileSystem>();
            this.sourceControlledFileSystem.AssertLocalServiceIsNotNull();

            this.bindingMode = bindingMode;
            this.logger = logger;
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

        internal /*for testing purposes*/ IReadOnlyDictionary<Language, RuleSetInformation> RuleSetsInformationMap
        {
            get { return this.rulesConfigInformationMap; }
        }
        #endregion

        #region ISolutionRuleStore

        public void RegisterKnownRuleSets(IDictionary<Language, IRulesConfigurationFile> ruleSets)
        {
            if (ruleSets == null)
            {
                throw new ArgumentNullException(nameof(ruleSets));
            }

            var ruleSetInfo = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfo.AssertLocalServiceIsNotNull();

            foreach (var keyValue in ruleSets)
            {
                Debug.Assert(!this.rulesConfigInformationMap.ContainsKey(keyValue.Key), "Attempted to register an already registered rule set. Group:" + keyValue.Key);

                string solutionRuleSet = ruleSetInfo.CalculateSolutionSonarQubeRuleSetFilePath(this.projectKey, keyValue.Key, this.bindingMode);
                this.rulesConfigInformationMap[keyValue.Key] = new RuleSetInformation(keyValue.Key, keyValue.Value) { NewRuleSetFilePath = solutionRuleSet };
            }
        }

        public RuleSetInformation GetRuleSetInformation(Language language)
        {
            RuleSetInformation info;

            if (!this.rulesConfigInformationMap.TryGetValue(language, out info) || info == null)
            {
                Debug.Fail("Expected to be called by the ProjectBinder after the known rulesets were registered");
                return null;
            }

            Debug.Assert(info.NewRuleSetFilePath != null, "Expected to be called after Prepare");

            return info;
        }

        #endregion

        #region Public API
        public void Initialize(IEnumerable<Project> projects, IDictionary<Language, SonarQubeQualityProfile> profilesMap)
        {
            if (projects == null)
            {
                throw new ArgumentNullException(nameof(projects));
            }

            if (profilesMap == null)
            {
                throw new ArgumentNullException(nameof(profilesMap));
            }

            this.SolutionFullPath = this.projectSystem.GetCurrentActiveSolution().FullName;

            this.qualityProfileMap = new Dictionary<Language, SonarQubeQualityProfile>(profilesMap);

            foreach (Project project in projects)
            {
                if (IsProjectBindingRequired(project))
                {
                    var binder = new ProjectBindingOperation(serviceProvider, project, this, this.logger);
                    binder.Initialize();
                    this.childBinder.Add(binder);
                }
                else
                {
                    this.logger.WriteLine(Strings.Bind_Project_NotRequired, project.FullName);
                }
            }
        }

        public void Prepare(CancellationToken token)
        {
            Debug.Assert(this.SolutionFullPath != null, "Expected to be initialized");

            foreach (var keyValue in this.rulesConfigInformationMap)
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
                    info.RulesConfigurationFile.Save(info.NewRuleSetFilePath);

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

                /* only show the files in the Solution Explorer in legacy mode */
                if (this.bindingMode == SonarLintMode.LegacyConnected)
                {
                    UpdateSolutionFile();
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Helpers

        internal /* for testing */ static bool IsProjectBindingRequired(Project project)
        {
            var language = ProjectToLanguageMapper.GetLanguageForProject(project);
            return language == Language.VBNET || language == Language.CSharp;
        }

        /// <summary>
        /// Will bend add/edit the binding information for next time usage
        /// </summary>
        private void PendBindingInformation(ConnectionInformation connInfo)
        {
            Debug.Assert(this.qualityProfileMap != null, "Initialize was expected to be called first");

            var bindingSerializer = this.serviceProvider.GetService<IConfigurationProvider>();
            bindingSerializer.AssertLocalServiceIsNotNull();

            BasicAuthCredentials credentials = connection.UserName == null ? null : new BasicAuthCredentials(connInfo.UserName, connInfo.Password);

            Dictionary<Language, ApplicableQualityProfile> map = new Dictionary<Language, ApplicableQualityProfile>();

            foreach (var keyValue in this.qualityProfileMap)
            {
                map[keyValue.Key] = new ApplicableQualityProfile
                {
                    ProfileKey = keyValue.Value.Key,
                    ProfileTimestamp = keyValue.Value.TimeStamp
                };
            }

            var bound = new BoundSonarQubeProject(connInfo.ServerUri, this.projectKey, this.projectName,
                credentials, connInfo.Organization);
            bound.Profiles = map;

            var config = new BindingConfiguration(bound, this.bindingMode);
            bindingSerializer.WriteConfiguration(config);
        }

        private void UpdateSolutionFile()
        {
            foreach (RuleSetInformation info in rulesConfigInformationMap.Values)
            {
                Debug.Assert(this.sourceControlledFileSystem.FileExist(info.NewRuleSetFilePath), "File not written " + info.NewRuleSetFilePath);
                this.AddFileToSolutionItems(info.NewRuleSetFilePath);
                this.RemoveFileFromSolutionItems(info.NewRuleSetFilePath);
            }
        }

        private void AddFileToSolutionItems(string fullFilePath)
        {
            Debug.Assert(Path.IsPathRooted(fullFilePath) && this.sourceControlledFileSystem.FileExist(fullFilePath), "Expecting a rooted path to existing file");

            Project solutionItemsProject = this.projectSystem.GetSolutionFolderProject(Constants.LegacySonarQubeManagedFolderName, true);
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

        private void RemoveFileFromSolutionItems(string fullFilePath)
        {
            Debug.Assert(Path.IsPathRooted(fullFilePath) && this.sourceControlledFileSystem.FileExist(fullFilePath), "Expecting a rooted path to existing file");

            Project solutionItemsProject = this.projectSystem.GetSolutionItemsProject(false);
            if (solutionItemsProject != null)
            {
                // Remove file from project and if project is empty, remove project from solution
                var fileName = Path.GetFileName(fullFilePath);
                this.projectSystem.RemoveFileFromProject(solutionItemsProject, fileName);

            }
        }

        #endregion
    }
}

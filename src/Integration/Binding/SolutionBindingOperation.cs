/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using EnvDTE;
using NuGet;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Models;
using IFileSystem = System.IO.Abstractions.IFileSystem;
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
        private readonly List<Action> childBinder = new List<Action>();
        private readonly IDictionary<Language, IBindingConfigFile> bindingConfigInformationMap = new Dictionary<Language, IBindingConfigFile>();
        private IDictionary<Language, SonarQubeQualityProfile> qualityProfileMap;
        private readonly ConnectionInformation connection;
        private readonly string projectKey;
        private readonly string projectName;
        private readonly SonarLintMode bindingMode;
        private readonly IProjectBinderFactory projectBinderFactory;
        private readonly ILegacyConfigFolderItemAdder legacyConfigFolderItemAdder;
        private readonly IFileSystem fileSystem;
        private IEnumerable<Project> projects;

        public SolutionBindingOperation(IServiceProvider serviceProvider,
            ConnectionInformation connection,
            string projectKey,
            string projectName,
            SonarLintMode bindingMode,
            ILogger logger)
            : this(serviceProvider, connection, projectKey, projectName, bindingMode,  new ProjectBinderFactory(serviceProvider, logger), new LegacyConfigFolderItemAdder(serviceProvider), new FileSystem())
        {
        }

        internal SolutionBindingOperation(IServiceProvider serviceProvider,
            ConnectionInformation connection,
            string projectKey,
            string projectName,
            SonarLintMode bindingMode,
            IProjectBinderFactory projectBinderFactory,
            ILegacyConfigFolderItemAdder legacyConfigFolderItemAdder,
            IFileSystem fileSystem)
        {
            if (string.IsNullOrWhiteSpace(projectKey))
            {
                throw new ArgumentNullException(nameof(projectKey));
            }

            bindingMode.ThrowIfNotConnected();

            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.legacyConfigFolderItemAdder = legacyConfigFolderItemAdder ?? throw new ArgumentNullException(nameof(legacyConfigFolderItemAdder));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.projectBinderFactory = projectBinderFactory ?? throw new ArgumentNullException(nameof(projectBinderFactory));

            this.projectKey = projectKey;
            this.projectName = projectName;
            this.bindingMode = bindingMode;

            this.projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.sourceControlledFileSystem = this.serviceProvider.GetService<ISourceControlledFileSystem>();
            this.sourceControlledFileSystem.AssertLocalServiceIsNotNull();

        }

        #region State
        internal /*for testing purposes*/ IList<Action> Binders
        {
            get { return this.childBinder; }
        }

        internal /*for testing purposes*/ string SolutionFullPath
        {
            get;
            private set;
        }

        internal /*for testing purposes*/ IReadOnlyDictionary<Language, IBindingConfigFile> RuleSetsInformationMap => 
            new ReadOnlyDictionary<Language, IBindingConfigFile>(bindingConfigInformationMap);

        #endregion

        #region ISolutionRuleStore

        public void RegisterKnownConfigFiles(IDictionary<Language, IBindingConfigFile> languageToFileMap)
        {
            if (languageToFileMap == null)
            {
                throw new ArgumentNullException(nameof(languageToFileMap));
            }

            bindingConfigInformationMap.Clear();
            bindingConfigInformationMap.AddRange(languageToFileMap);
        }

        public IBindingConfigFile GetBindingConfig(Language language)
        {
            if (!bindingConfigInformationMap.TryGetValue(language, out var info) || info == null)
            {
                Debug.Fail("Expected to be called by the ProjectBinder after the known rulesets were registered");
                return null;
            }
            return info;
        }

        #endregion

        #region Public API
        public void Initialize(IEnumerable<Project> projects, IDictionary<Language, SonarQubeQualityProfile> profilesMap)
        {
            if (profilesMap == null)
            {
                throw new ArgumentNullException(nameof(profilesMap));
            }

            this.SolutionFullPath = this.projectSystem.GetCurrentActiveSolution().FullName;
            this.qualityProfileMap = new Dictionary<Language, SonarQubeQualityProfile>(profilesMap);
            this.projects = projects ?? throw new ArgumentNullException(nameof(projects));
        }

        public void Prepare(CancellationToken token)
        {
            Debug.Assert(this.SolutionFullPath != null, "Expected to be initialized");

            foreach (var keyValue in this.bindingConfigInformationMap)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                var info = keyValue.Value;
                Debug.Assert(!string.IsNullOrWhiteSpace(info.FilePath), "Expected to be set during registration time");

                sourceControlledFileSystem.QueueFileWrite(info.FilePath, () =>
                {
                    var ruleSetDirectoryPath = Path.GetDirectoryName(info.FilePath);

                    fileSystem.Directory.CreateDirectory(ruleSetDirectoryPath); // will no-op if exists

                    // Create or overwrite existing rule set
                    info.Save();

                    return true;
                });

                Debug.Assert(sourceControlledFileSystem.FileExistOrQueuedToBeWritten(info.FilePath), "Expected a rule set to pend pended");
            }

            foreach (var project in projects)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                var languageForProject = ProjectToLanguageMapper.GetLanguageForProject(project);
                var bindingConfigFile = GetBindingConfig(languageForProject);

                var projectBinder = projectBinderFactory.Get(project);
                var bindAction = projectBinder.GetBindAction(bindingConfigFile, project, token);

                childBinder.Add(bindAction);
            }
        }

        public bool CommitSolutionBinding()
        {
            this.PendBindingInformation(this.connection); // This is the last pend, so will be executed last

            if (this.sourceControlledFileSystem.WriteQueuedFiles())
            {
                // No reason to modify VS state if could not write files
                this.childBinder.ForEach(b => b());

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

        /// <summary>
        /// Will bend add/edit the binding information for next time usage
        /// </summary>
        private void PendBindingInformation(ConnectionInformation connInfo)
        {
            Debug.Assert(this.qualityProfileMap != null, "Initialize was expected to be called first");

            var configurationPersister = this.serviceProvider.GetService<IConfigurationPersister>();
            configurationPersister.AssertLocalServiceIsNotNull();

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

            configurationPersister.Persist(bound, bindingMode);
        }

        private void UpdateSolutionFile()
        {
            foreach (var info in bindingConfigInformationMap.Values)
            {
                Debug.Assert(fileSystem.File.Exists(info.FilePath), "File not written " + info.FilePath);
                legacyConfigFolderItemAdder.AddToFolder(info.FilePath);
            }
        }

        #endregion
    }
}

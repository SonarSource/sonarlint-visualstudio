/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using IFileSystem = System.IO.Abstractions.IFileSystem;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    // Legacy connected mode:
    // * writes the binding info files to disk and adds them as solution items.
    // * co-ordinates writing project-level changes

    /// <summary>
    /// Handles writing solution-level files
    /// </summary>
    internal class SolutionBindingOperation : ISolutionBindingOperation
    {
        private readonly ISourceControlledFileSystem sourceControlledFileSystem;
        private readonly IProjectSystemHelper projectSystem;
        private readonly IDictionary<Language, IBindingConfig> bindingConfigInformationMap = new Dictionary<Language, IBindingConfig>();
        private readonly SonarLintMode bindingMode;
        private readonly ILegacyConfigFolderItemAdder legacyConfigFolderItemAdder;
        private readonly IFileSystem fileSystem;
        public SolutionBindingOperation(IServiceProvider serviceProvider,
            SonarLintMode bindingMode,
            ILogger logger)
            : this(serviceProvider, bindingMode,  new LegacyConfigFolderItemAdder(serviceProvider), new FileSystem())
        {
        }

        internal SolutionBindingOperation(IServiceProvider serviceProvider,
            SonarLintMode bindingMode,
            ILegacyConfigFolderItemAdder legacyConfigFolderItemAdder,
            IFileSystem fileSystem)
        {
            bindingMode.ThrowIfNotConnected();

            serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.legacyConfigFolderItemAdder = legacyConfigFolderItemAdder ?? throw new ArgumentNullException(nameof(legacyConfigFolderItemAdder));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            this.bindingMode = bindingMode;

            this.projectSystem = serviceProvider.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.sourceControlledFileSystem = serviceProvider.GetService<ISourceControlledFileSystem>();
            this.sourceControlledFileSystem.AssertLocalServiceIsNotNull();
        }

        #region State

        internal /*for testing purposes*/ string SolutionFullPath
        {
            get;
            private set;
        }

        internal /*for testing purposes*/ IReadOnlyDictionary<Language, IBindingConfig> RuleSetsInformationMap => 
            new ReadOnlyDictionary<Language, IBindingConfig>(bindingConfigInformationMap);

        #endregion

        #region ISolutionRuleStore

        public void RegisterKnownConfigFiles(IDictionary<Language, IBindingConfig> languageToFileMap)
        {
            if (languageToFileMap == null)
            {
                throw new ArgumentNullException(nameof(languageToFileMap));
            }

            bindingConfigInformationMap.Clear();

            foreach (var bindingConfig in languageToFileMap)
            {
                bindingConfigInformationMap.Add(bindingConfig);
            }
        }

        public IBindingConfig GetBindingConfig(Language language)
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

        public void Initialize()
        {
            this.SolutionFullPath = this.projectSystem.GetCurrentActiveSolution().FullName;
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

                sourceControlledFileSystem.QueueFileWrites(info.SolutionLevelFilePaths, () =>
                {
                    foreach (var solutionItem in info.SolutionLevelFilePaths)
                    {
                        var ruleSetDirectoryPath = Path.GetDirectoryName(solutionItem);
                        fileSystem.Directory.CreateDirectory(ruleSetDirectoryPath); // will no-op if exists
                    }

                    info.Save();

                    return true;
                });

                Debug.Assert(sourceControlledFileSystem.FilesExistOrQueuedToBeWritten(info.SolutionLevelFilePaths), "Expected solution items to be queued for writing");
            }
        }

        public bool CommitSolutionBinding()
        {
            if (this.sourceControlledFileSystem.WriteQueuedFiles())
            {
                // TODO - CM cleanup - get rid of LegacyConnected mode code
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

        private void UpdateSolutionFile()
        {
            foreach (var info in bindingConfigInformationMap.Values)
            {
                foreach (var solutionItem in info.SolutionLevelFilePaths)
                {
                    Debug.Assert(fileSystem.File.Exists(solutionItem), "File not written " + solutionItem);
                    legacyConfigFolderItemAdder.AddToFolder(solutionItem);
                }
            }
        }

        #endregion
    }
}

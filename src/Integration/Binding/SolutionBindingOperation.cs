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
using IFileSystem = System.IO.Abstractions.IFileSystem;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Handles writing solution-level files
    /// </summary>
    internal class SolutionBindingOperation : ISolutionBindingOperation
    {
        private readonly IProjectSystemHelper projectSystem;
        private readonly IDictionary<Language, IBindingConfig> bindingConfigInformationMap = new Dictionary<Language, IBindingConfig>();
        private readonly IFileSystem fileSystem;

        public SolutionBindingOperation(IServiceProvider serviceProvider)
            : this(serviceProvider, new FileSystem())
        {
        }

        internal SolutionBindingOperation(IServiceProvider serviceProvider,
            IFileSystem fileSystem)
        {
            serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            this.projectSystem = serviceProvider.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();
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

                foreach (var solutionItem in info.SolutionLevelFilePaths)
                {
                    var ruleSetDirectoryPath = Path.GetDirectoryName(solutionItem);
                    fileSystem.Directory.CreateDirectory(ruleSetDirectoryPath); // will no-op if exists
                }

                info.Save();
            }
        }

        public bool CommitSolutionBinding()
        {
            // TODO - CM cleanup
            // no-op
            return true;
        }
            
        #endregion
    }
}

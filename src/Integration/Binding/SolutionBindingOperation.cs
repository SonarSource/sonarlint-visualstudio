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
        private readonly IDictionary<Language, IBindingConfig> bindingConfigInformationMap = new Dictionary<Language, IBindingConfig>();
        private readonly IFileSystem fileSystem;

        public SolutionBindingOperation()
            : this(new FileSystem())
        {
        }

        internal SolutionBindingOperation(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        #region State

        internal /*for testing purposes*/ IReadOnlyDictionary<Language, IBindingConfig> RuleSetsInformationMap => 
            new ReadOnlyDictionary<Language, IBindingConfig>(bindingConfigInformationMap);

        #endregion

        #region Public API

        public void Initialize()
        {
            // TODO CM cleanup
            // no-op
        }

        public void Prepare(IEnumerable<IBindingConfig> bindingConfigs, CancellationToken token)
        {
            foreach (var config in bindingConfigs)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                foreach (var solutionItem in config.SolutionLevelFilePaths)
                {
                    var ruleSetDirectoryPath = Path.GetDirectoryName(solutionItem);
                    fileSystem.Directory.CreateDirectory(ruleSetDirectoryPath); // will no-op if exists
                }

                config.Save();
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

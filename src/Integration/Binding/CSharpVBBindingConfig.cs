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
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CSharpVB;
using RuleSet = Microsoft.VisualStudio.CodeAnalysis.RuleSets.RuleSet;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class CSharpVBBindingConfig : ICSharpVBBindingConfig
    {
        private readonly IFileSystem fileSystem;

        public FilePathAndContent<SonarLintConfiguration> AdditionalFile { get; }

        public FilePathAndContent<RuleSet> RuleSet { get; }

        public IEnumerable<string> SolutionLevelFilePaths => new List<string> { RuleSet.Path, AdditionalFile.Path };

        public CSharpVBBindingConfig(FilePathAndContent<RuleSet> ruleset, FilePathAndContent<SonarLintConfiguration> additionalFile)
            : this(ruleset, additionalFile, new FileSystem())
        {
        }

        internal CSharpVBBindingConfig(FilePathAndContent<RuleSet> ruleset, FilePathAndContent<SonarLintConfiguration> additionalFile, IFileSystem fileSystem)
        {
            RuleSet = ruleset ?? throw new ArgumentNullException(nameof(ruleset));
            AdditionalFile = additionalFile ?? throw new ArgumentNullException(nameof(additionalFile));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public void Save()
        {
            RuleSet.Content.WriteToFile(RuleSet.Path);

            var serializedContent = Serializer.ToString(AdditionalFile.Content);
            fileSystem.File.WriteAllText(AdditionalFile.Path, serializedContent);
        }
    }
}

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
using System.IO;
using System.IO.Abstractions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class CSharpVBBindingConfig : ICSharpVBBindingConfig
    {
        private readonly IFileSystem fileSystem;

        public string FilePath { get; }
        public RuleSet RuleSet { get; }

        public CSharpVBBindingConfig(RuleSet ruleSet, string filePath)
            : this(ruleSet, filePath, new FileSystem())
        {
        }

        public CSharpVBBindingConfig(RuleSet ruleSet, string filePath, IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            RuleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
        }

        public void Save(ISourceControlledFileSystem sourceControlledFileSystem)
        {
            sourceControlledFileSystem.QueueFileWrite(FilePath, () =>
            {
                var bindingDirectoryPath = Path.GetDirectoryName(FilePath);
                fileSystem.Directory.CreateDirectory(bindingDirectoryPath); // will no-op if exists

                RuleSet.WriteToFile(FilePath);

                return true;
            });
        }
    }
}

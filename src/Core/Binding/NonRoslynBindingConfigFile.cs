﻿/*
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
using System.IO.Abstractions;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.Binding
{
    public class NonRoslynBindingConfigFile : IBindingConfig
    {
        private readonly IFileSystem fileSystem;

        public NonRoslynBindingConfigFile(RulesSettings ruleSettings, string filePath)
            : this(ruleSettings, filePath, new FileSystem())
        {
        }

        public NonRoslynBindingConfigFile(RulesSettings rulesSettings, string filePath, IFileSystem fileSystem)
        {
            RuleSettings = rulesSettings ?? throw new ArgumentNullException(nameof(rulesSettings));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
        }

        internal /* for testing */ RulesSettings RuleSettings { get; }
        internal /* for testing */ string FilePath { get; }

        public IEnumerable<string> SolutionLevelFilePaths => new List<string> { FilePath };

        public void Save()
        {
            var dataAsText = JsonConvert.SerializeObject(RuleSettings, Formatting.Indented);
            fileSystem.File.WriteAllText(FilePath, dataAsText);
        }
    }
}

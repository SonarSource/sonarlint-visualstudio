﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.CSharpVB;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    internal class CSharpVBBindingConfig : ICSharpVBBindingConfig
    {
        private readonly IFileSystem fileSystem;

        public FilePathAndContent<SonarLintConfiguration> AdditionalFile { get; }

        public FilePathAndContent<string> GlobalConfig { get; }

        public CSharpVBBindingConfig(FilePathAndContent<string> globalConfig, FilePathAndContent<SonarLintConfiguration> additionalFile)
            : this(globalConfig, additionalFile, new FileSystem())
        {
        }

        internal CSharpVBBindingConfig(FilePathAndContent<string> globalConfig, FilePathAndContent<SonarLintConfiguration> additionalFile, IFileSystem fileSystem)
        {
            GlobalConfig = globalConfig ?? throw new ArgumentNullException(nameof(globalConfig));
            AdditionalFile = additionalFile ?? throw new ArgumentNullException(nameof(additionalFile));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public void Save()
        {
            EnsureParentDirectoryExists(GlobalConfig.Path);
            fileSystem.File.WriteAllText(GlobalConfig.Path, GlobalConfig.Content);

            var serializedAdditionalFile = Serializer.ToString(AdditionalFile.Content);
            EnsureParentDirectoryExists(AdditionalFile.Path);
            fileSystem.File.WriteAllText(AdditionalFile.Path, serializedAdditionalFile);
        }

        private void EnsureParentDirectoryExists(string filePath)
        {
            var parentDirectory = Path.GetDirectoryName(filePath);
            fileSystem.Directory.CreateDirectory(parentDirectory); // will no-op if exists
        }
    }
}

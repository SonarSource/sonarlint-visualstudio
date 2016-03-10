//-----------------------------------------------------------------------
// <copyright file="RuleSetWriter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal abstract class RuleSetWriter : IRuleSetGenerationFileSystem
    {
        public const string FileExtension = "ruleset";

        private readonly IRuleSetGenerationFileSystem fileSystem;
        private readonly HashSet<string> writtenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        protected IRuleSetGenerationFileSystem FileSystem => this.fileSystem;

        protected RuleSetWriter(IRuleSetGenerationFileSystem fileSystem = null)
        {
            this.fileSystem = fileSystem ?? this;
        }

        #region IRuleSetFileSystem

        bool IRuleSetGenerationFileSystem.FileExists(string path)
        {
            return File.Exists(path);
        }

        void IRuleSetGenerationFileSystem.WriteRuleSetFile(RuleSet ruleSet, string path)
        {
            if (this.writtenFiles.Contains(path))
            {
                Debug.Fail("The same file was attempted to be written more than once: " + path);
            }
            else
            {
                this.writtenFiles.Add(path);
                ruleSet.WriteToFile(path);
            }
        }

        void IRuleSetGenerationFileSystem.CreateDirectory(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
        }

        bool IRuleSetGenerationFileSystem.DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        RuleSet IRuleSetGenerationFileSystem.LoadRuleSet(string path)
        {
            return RuleSet.LoadFromFile(path);
        }

        #endregion
    }
}

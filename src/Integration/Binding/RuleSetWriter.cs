//-----------------------------------------------------------------------
// <copyright file="RuleSetWriter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System.IO;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal abstract class RuleSetWriter : IRuleSetGenerationFileSystem
    {
        internal /* testing purposes */ const string FileExtension = "ruleset";
        private readonly IRuleSetGenerationFileSystem fileSystem;

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
            ruleSet.WriteToFile(path);
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

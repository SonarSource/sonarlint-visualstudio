//-----------------------------------------------------------------------
// <copyright file="ConfigurableRuleSetFileSystem.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Binding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableRuleSetGenerationFileSystem : IRuleSetGenerationFileSystem
    {
        public class File
        {
            public long Timestamp { get; }

            public RuleSet Data { get; }

            public File(RuleSet data, long timestamp = 0)
            {
                this.Timestamp = timestamp;
                this.Data = data;
            }
        }

        private readonly HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, File> files = new Dictionary<string, File>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> Directories => this.directories;

        public IDictionary<string, File> Files => this.files;

        #region Test Helpers

        public Regex ExistingFilesPattern { get; set; }

        public void AssertFileExists(string expectedPath)
        {
            if (!((IRuleSetGenerationFileSystem)this).FileExists(expectedPath))
            {
                string fullFileList = "Actual files:" + Environment.NewLine + string.Join(Environment.NewLine, this.Files.Keys);
                Assert.Fail($"File does not exist at expected path '{expectedPath}'.{Environment.NewLine}{fullFileList}");
            }
        }

        public void AssertDirectoryExists(string expectedPath)
        {
            if (!((IRuleSetGenerationFileSystem)this).DirectoryExists(expectedPath))
            {
                string fullDirList = "Actual directories:" + Environment.NewLine + string.Join(Environment.NewLine, this.Directories);
                Assert.Fail($"Directory does not exist at expected path '{expectedPath}'.{Environment.NewLine}{fullDirList}");
            }
        }

        public void AddRuleSetFile(string path, RuleSet ruleSet)
        {
            this.Files.Add(path, new File(ruleSet));
        }

        public void AssertRuleSetsAreEqual(string path, RuleSet expectedRuleSet)
        {
            this.AssertFileExists(path);

            RuleSet actualRuleSet = this.Files[path]?.Data;

            Assert.IsNotNull(actualRuleSet, "Expected rule set to be written");
            RuleSetAssert.AreEqual(expectedRuleSet, actualRuleSet);
        }

        public long GetFileTimestamp(string path)
        {
            this.AssertFileExists(path);
            return this.Files[path].Timestamp;
        }

        #endregion

        #region IRuleSetFileSystem

        bool IRuleSetGenerationFileSystem.FileExists(string path)
        {
            return (this.ExistingFilesPattern?.IsMatch(path) ?? false) || this.Files.ContainsKey(path);
        }

        void IRuleSetGenerationFileSystem.WriteRuleSetFile(RuleSet ruleSet, string path)
        {
            if (this.Files.ContainsKey(path))
            {
                var newTimestamp = this.Files[path].Timestamp + 1;
                this.Files[path] = new File(ruleSet, newTimestamp); // Overwrite and update timestamp
            }
            else
            {
                this.Files.Add(path, new File(ruleSet)); // New file
            }
        }

        void IRuleSetGenerationFileSystem.CreateDirectory(string directoryPath)
        {
            this.Directories.Add(directoryPath);
        }

        bool IRuleSetGenerationFileSystem.DirectoryExists(string path)
        {
            return this.Directories.Contains(path);
        }

        RuleSet IRuleSetGenerationFileSystem.LoadRuleSet(string path)
        {
            if (this.Files.ContainsKey(path))
            {
                RuleSet rs = this.Files[path]?.Data;
                if (rs == null)
                {
                    throw new XmlException("File is empty in test file system"); // Simulate RuleSet.LoadFromFile()
                }
                rs.Validate(); // Simulate RuleSet.LoadFromFile() (throws InvalidRuleSetException)
                return rs;
            }
            throw new IOException("File does not exist in test file system"); // Simulate RuleSet.LoadFromFile()
        }

        #endregion
    }
}

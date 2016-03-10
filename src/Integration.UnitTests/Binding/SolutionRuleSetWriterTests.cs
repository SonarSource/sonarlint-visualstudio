//-----------------------------------------------------------------------
// <copyright file="SolutionRuleSetWriterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.Globalization;
using System.IO;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionRuleSetWriterTests
    {
        private static readonly IFormatProvider TestCulture = CultureInfo.InvariantCulture;

        #region Tests

        [TestMethod]
        public void SolutionRuleSetWriter_WriteSolutionLevelRuleSet_NullArgumentChecks()
        {
            var testSubject = new SolutionRuleSetWriter("key");
            var ruleSet = TestRuleSetHelper.CreateTestRuleSet(numRules: 1);

            Exceptions.Expect<ArgumentNullException>(()=>
            {
                testSubject.WriteSolutionLevelRuleSet(null, ruleSet, string.Empty);
            });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                testSubject.WriteSolutionLevelRuleSet(string.Empty, ruleSet, string.Empty);
            });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                testSubject.WriteSolutionLevelRuleSet("X:\\bob.sln", null, string.Empty);
            });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                testSubject.WriteSolutionLevelRuleSet("X:\\bob.sln", ruleSet, null);
            });
        }

        [TestMethod]
        public void SolutionRuleSetWriter_Ctor_NullArgumentChecks()
        {
            var testFs = new ConfigurableRuleSetGenerationFileSystem();

            Exceptions.Expect<ArgumentNullException>(() => new SolutionRuleSetWriter(null, testFs));
        }

        [TestMethod]
        public void SolutionRuleSetWriter_GetOrCreateRuleSetDirectory()
        {
            // Setup
            const string solutionRoot = @"X:\myDirectory\mySubDirectory";
            string ruleSetRoot = Path.Combine(solutionRoot, Constants.SonarQubeManagedFolderName);
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new SolutionRuleSetWriter("key", fileSystem);

            // Test case 1: directory chain already exists
            // Setup
            fileSystem.Directories.Clear();
            fileSystem.Directories.Add(ruleSetRoot);

            // Act
            testSubject.GetOrCreateRuleSetDirectory(solutionRoot);

            // Verify
            fileSystem.AssertDirectoryExists(ruleSetRoot);
            Assert.AreEqual(1, fileSystem.Directories.Count, "Expected only 1 already existing directory");

            // Test case 2: directory chain does not exist
            // Setup
            fileSystem.Directories.Clear();

            // Act
            testSubject.GetOrCreateRuleSetDirectory(solutionRoot);

            // Verify
            fileSystem.AssertDirectoryExists(ruleSetRoot);
            Assert.AreEqual(1, fileSystem.Directories.Count, "Expected only 1 newly created directory");
        }

        [TestMethod]
        public void SolutionRuleSetWriter_GenerateSolutionRuleSetPath()
        {
            // Setup
            const string rootPath = @"X:\MyPath\";
          
            const string expectedPath = @"X:\MyPath\SonarQubeProjectKEY" + "." + RuleSetWriter.FileExtension;

            // Act
            string actualPath = SolutionRuleSetWriter.GenerateSolutionRuleSetPath(rootPath, "SonarQubeProjectKEY", "");

            // Verify
            Assert.AreEqual(expectedPath, actualPath);
        }

        [TestMethod]
        public void SolutionRuleSetWriter_WriteSolutionLevelRuleSet_WritesRuleSetToCorrectPath()
        {
            // Setup
            const string sonarProjectName = "SonarAwesome";
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var writer = new SolutionRuleSetWriter(sonarProjectName, fileSystem);
            const string solutionRoot = @"X:\ProjectAwesome\";
            string solutionFullPath = Path.Combine(solutionRoot, "mySolution.sln");
            string ruleSetRoot = Path.Combine(solutionRoot, Constants.SonarQubeManagedFolderName);
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(numRules: 3);

            string expectedOutputPath = SolutionRuleSetWriter.GenerateSolutionRuleSetPath(ruleSetRoot, sonarProjectName, "MySuffix");

            // Act
            writer.WriteSolutionLevelRuleSet(solutionFullPath, ruleSet, "MySuffix");

            // Verify
            fileSystem.AssertRuleSetsAreEqual(expectedOutputPath, ruleSet);
        }

        #endregion
    }
}

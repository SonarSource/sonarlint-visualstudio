//-----------------------------------------------------------------------
// <copyright file="ProjectRuleSetWriterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Binding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectRuleSetWriterTests
    {
        #region Tests

        [TestMethod]
        public void ProjectRuleSetWriter_RemoveAllIncludesUnderRoot()
        {
            // Setup
            const string removalRoot = @"X:\dirA\rootRemoveDir\";

            const string notRemovedAbsolute = @"X:\should not be removed absolute.ruleset";
            const string notRemovedRelative = @"..\..\should not be removed absolute.ruleset";

            const string removedAbsolute = @"X:\dirA\rootRemoveDir\sonarqube1.ruleset";
            const string removedRelative = @"..\rootRemoveDir\sonarqube2.ruleset";

            var ruleSet = new RuleSet("Test");
            ruleSet.RuleSetIncludes.Add(new RuleSetInclude(notRemovedAbsolute, RuleAction.Default));
            ruleSet.RuleSetIncludes.Add(new RuleSetInclude(notRemovedRelative, RuleAction.Default));
            ruleSet.RuleSetIncludes.Add(new RuleSetInclude(removedAbsolute, RuleAction.Default));
            ruleSet.RuleSetIncludes.Add(new RuleSetInclude(removedRelative, RuleAction.Default));

            var expectedRuleSet = new RuleSet("Test");
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(notRemovedAbsolute, RuleAction.Default));
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(notRemovedRelative, RuleAction.Default));

            // Act
            ProjectRuleSetWriter.RemoveAllIncludesUnderRoot(ruleSet, removalRoot);

            // Verify
            RuleSetAssert.AreEqual(expectedRuleSet, ruleSet);
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_NullArgumentChecks()
        {
            var testSubject = new ProjectRuleSetWriter();

            Exceptions.Expect<ArgumentNullException>(() => 
            {
                testSubject.WriteProjectLevelRuleSet(null, "config", @"X:\MySln\RuleSets\rs.ruleset", @"Y:\existing.ruleset");
            });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                testSubject.WriteProjectLevelRuleSet(@"X:\MySln\Project1\proj1.proj", "config", null, @"Y:\existing.ruleset");
            });
        }

        [TestMethod]
        public void ProjectRuleSetWriter_SafeLoadRuleSet()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            // Test case 1: load existing, well formed rule set
            const string goodRuleSetPath = @"X:\good.ruleset";
            RuleSet goodRuleSet = TestRuleSetHelper.CreateTestRuleSet(numRules: 3);
            fileSystem.AddRuleSetFile(goodRuleSetPath, goodRuleSet);

            // Act
            RuleSet loadedGood = testSubject.SafeLoadRuleSet(goodRuleSetPath);

            // Verify
            Assert.IsNotNull(loadedGood, "Expected existing well formed ruleset to be loaded");
            RuleSetAssert.AreEqual(goodRuleSet, loadedGood);


            // Test case 2: load existing, badly formed rule set (empty)
            const string badRuleSetPath = @"X:\bad.ruleset";
            fileSystem.AddRuleSetFile(badRuleSetPath, null);

            // Act
            RuleSet loadedBad = testSubject.SafeLoadRuleSet(badRuleSetPath);

            // Verify
            Assert.IsNull(loadedBad, "Expected no ruleset to be loaded");


            // Test case 3: load non-existent rule set
            const string doesNotExistRuleSetPath = @"X:\doesnotexist.ruleset";

            // Act
            RuleSet loadedNotExists = testSubject.SafeLoadRuleSet(doesNotExistRuleSetPath);

            // Verify
            Assert.IsNull(loadedNotExists, "Expected no ruleset to be loaded");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_GenerateNewProjectRuleSetPath_ProjectNameHasDot_AppendsExtension()
        {
            // Setup
            const string ruleSetRootPath = @"X:\";
            const string projectName = "My.File.With.Dots";
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);
            string expected = $"X:\\My.File.With.Dots.{RuleSetWriter.FileExtension}";

            // Act
            string actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                projectName,
                null
            );

            // Verify
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ProjectRuleSetWriter_GenerateNewProjectRuleSetPath_DesiredFileNameExists_AppendsNumberToFileName()
        {
            // General setup
            const string ruleSetRootPath = @"X:\";
            const string projectName = "NameTaken";
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            // Test case 1: desired name exists
            // Setup
            fileSystem.Files.Add($"X:\\NameTaken.{RuleSetWriter.FileExtension}", null);

            // Act
            string actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                projectName,
                configuration: null
            );

            // Verify
            Assert.AreEqual($"X:\\{projectName}-1.{RuleSetWriter.FileExtension}", actual, "Expected to append running number to desired file name");

            // Test case 2: desired name + 1 + 2 exists
            // Setup
            fileSystem.Files.Add($"X:\\{projectName}-1.{RuleSetWriter.FileExtension}", null);
            fileSystem.Files.Add($"X:\\{projectName}-2.{RuleSetWriter.FileExtension}", null);

            // Act
            actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                projectName,
                configuration: null
            );

            // Verify
            Assert.AreEqual($"X:\\{projectName}-3.{RuleSetWriter.FileExtension}", actual, "Expected to append running number to desired file name");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_GenerateNewProjectRuleSetPath_NoAvailableFileNames_AppendsGuid()
        {
            // Setup
            const string ruleSetRootPath = @"X:\";
            const string projectName = "NameTaken";
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);
            fileSystem.ExistingFilesPattern = new Regex($"X:\\\\{projectName}-?[0-9]*\\.{RuleSetWriter.FileExtension}", RegexOptions.IgnoreCase); // all integer appended files exists
            
            var expectedGuidFileNamePattern = new Regex($"X:\\\\{projectName}-[A-Z0-9]{{32}}\\.{RuleSetWriter.FileExtension}", RegexOptions.IgnoreCase);

            // Act
            string actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                projectName,
                configuration: null
            );

            // Verify
            StringAssert.Matches(actual, expectedGuidFileNamePattern, "Expected to append GUID to desired file name");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_IsIgnoredRuleSet()
        {
            // Test case 1: not ignored
            // Act
            bool goodIgnored = ProjectRuleSetWriter.IsIgnoredRuleSet("My awesome rule set.ruleset");

            // Verify
            Assert.IsFalse(goodIgnored);

            // Test case 2: empty string
            // Act
            bool ignoredNull = ProjectRuleSetWriter.IsIgnoredRuleSet(null);
            bool ignoredEmpty = ProjectRuleSetWriter.IsIgnoredRuleSet(string.Empty);
            bool ignoredDefault = ProjectRuleSetWriter.IsIgnoredRuleSet(ProjectRuleSetWriter.DefaultProjectRuleSet.ToLower());

            // Verify
            Assert.IsTrue(ignoredNull);
            Assert.IsTrue(ignoredEmpty);
            Assert.IsTrue(ignoredDefault);
        }

        [TestMethod]
        public void ProjectRuleSetWriter_GenerateNewProjectRuleSet()
        {
            // Setup
            const string solutionIncludePath = @"..\..\solution.ruleset";
            const string currentRuleSetPath = @"X:\MyOriginal.ruleset";
            var expectedRuleSet = new RuleSet(Constants.RuleSetName);
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(solutionIncludePath, RuleAction.Default));
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(currentRuleSetPath, RuleAction.Default));

            // Act
            RuleSet actualRuleSet = ProjectRuleSetWriter.GenerateNewProjectRuleSet(solutionIncludePath, currentRuleSetPath);

            // Verify
            RuleSetAssert.AreEqual(expectedRuleSet, actualRuleSet);
        }

        [TestMethod]
        public void ProjectRuleSetWriter_UpdateExistingProjectRuleSet()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string existingProjectRuleSetPath = @"X:\MySolution\ProjectOne\proj1.ruleset";
            const string existingInclude = @"..\SolutionRuleSets\sonarqube1.ruleset";

            const string newSolutionRuleSetPath = @"X:\MySolution\SolutionRuleSets\sonarqube2.ruleset";
            const string expectedInclude = @"..\SolutionRuleSets\sonarqube2.ruleset";

            var existingProjectRuleSet = new RuleSet("existing name");
            existingProjectRuleSet.RuleSetIncludes.Add(new RuleSetInclude(existingInclude, RuleAction.Default));

            fileSystem.AddRuleSetFile(existingProjectRuleSetPath, existingProjectRuleSet);
            long initalWriteTimestamp = fileSystem.GetFileTimestamp(existingProjectRuleSetPath);

            var expectedRuleSet = new RuleSet("existing name");
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(expectedInclude, RuleAction.Default));

            // Act
            testSubject.UpdateExistingProjectRuleSet(existingProjectRuleSet, existingProjectRuleSetPath, newSolutionRuleSetPath);

            // Verify
            fileSystem.AssertRuleSetsAreEqual(existingProjectRuleSetPath, expectedRuleSet);

            long newWriteTimestamp = fileSystem.GetFileTimestamp(existingProjectRuleSetPath);
            Assert.IsTrue(newWriteTimestamp > initalWriteTimestamp, $"Expected updated rule set to be written out. File timestamps: initial {initalWriteTimestamp}, final {newWriteTimestamp}");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RuleSetIsFound()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string projectName = "My Project";
            const string projectConfig = "Happy";

            const string solutionRoot = @"X:\SolutionDir";
            string projectRoot = Path.Combine(solutionRoot, "ProjectDir");
            string projectFullPath = Path.Combine(projectRoot, $"{projectName}.proj");
            string projectRuleSetPath = Path.Combine(projectRoot, "myexistingproject.ruleset");

            string existingSolutionRuleSetPath = Path.Combine(solutionRoot, "RuleSets", "sonar1.ruleset");
            string existingSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectRuleSetPath, existingSolutionRuleSetPath);
            RuleSet existingRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { existingSolutionRuleSetInclude }
            );
            fileSystem.AddRuleSetFile(projectRuleSetPath, existingRuleSet);

            string newSolutionRuleSetPath = Path.Combine(solutionRoot, "RuleSets", "sonar2.ruleset");
            string newSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectRuleSetPath, newSolutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { newSolutionRuleSetInclude }
            );

            // Act
            string actualPath = testSubject.WriteProjectLevelRuleSet(projectRoot, projectConfig, newSolutionRuleSetPath, projectRuleSetPath);

            // Verify
            fileSystem.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RuleSetIsNotFound()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string projectName = "My Project";
            const string projectConfig = "Happy";

            const string solutionRoot = @"X:\SolutionDir";
            string projectRoot = Path.Combine(solutionRoot, "ProjectDir");
            string projectFullPath = Path.Combine(projectRoot, $"{projectName}.proj");
            string currentNonExistingRuleSet ="my-non-existingproject.ruleset";

            string existingSolutionRuleSetPath = Path.Combine(solutionRoot, "RuleSets", "sonar1.ruleset");
            string existingSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, existingSolutionRuleSetPath);
            RuleSet existingRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { existingSolutionRuleSetInclude }
            );

            string newSolutionRuleSetPath = Path.Combine(solutionRoot, "RuleSets", "sonar2.ruleset");
            string newSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, newSolutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { currentNonExistingRuleSet, newSolutionRuleSetInclude }
            );

            // Act
            string actualPath = testSubject.WriteProjectLevelRuleSet(projectFullPath, projectConfig, newSolutionRuleSetPath, currentNonExistingRuleSet);

            // Verify
            fileSystem.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_NewBinding()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string projectName = "My Project";
            const string projectConfig = "Happy";

            const string solutionRoot = @"X:\SolutionDir";
            string projectRoot = Path.Combine(solutionRoot, "ProjectDir");
            string projectFullPath = Path.Combine(projectRoot, $"{projectName}.proj");

            string solutionRuleSetPath = Path.Combine(solutionRoot, "RuleSets", "sonar1.ruleset");
            string expectedSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectRoot, solutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { expectedSolutionRuleSetInclude }
            );

            // Act
            string actualPath = testSubject.WriteProjectLevelRuleSet(projectRoot, projectConfig, solutionRuleSetPath, null);

            // Verify
            fileSystem.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        #endregion
    }
}

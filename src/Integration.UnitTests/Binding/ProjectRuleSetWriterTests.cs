//-----------------------------------------------------------------------
// <copyright file="ProjectRuleSetWriterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Binding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

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
            const string slnRoot = @"X:\SolutionDir\";
            string projectRoot = Path.Combine(slnRoot, @"Project\");
            string sonarRoot = Path.Combine(slnRoot, @"Sonar\");
            string commonRoot = Path.Combine(slnRoot, @"Common\");

            const string sonarRs1FileName = "Sonar1.ruleset";
            const string sonarRs2FileName = "Sonar2.ruleset";
            const string projectRsBaseFileName = "ProjectBase.ruleset";
            const string commonRs1FileName = "SolutionCommon1.ruleset";
            const string commonRs2FileName = "SolutionCommon2.ruleset";

            var sonarRs1 = TestRuleSetHelper.CreateTestRuleSet(sonarRoot, sonarRs1FileName);
            var sonarRs2 = TestRuleSetHelper.CreateTestRuleSet(sonarRoot, sonarRs2FileName);
            var projectBaseRs = TestRuleSetHelper.CreateTestRuleSet(projectRoot, projectRsBaseFileName);
            var commonRs1 = TestRuleSetHelper.CreateTestRuleSet(commonRoot, commonRs1FileName);
            var commonRs2 = TestRuleSetHelper.CreateTestRuleSet(commonRoot, commonRs2FileName);

            var fs = new ConfigurableRuleSetGenerationFileSystem();
            fs.AddRuleSetFile(sonarRs1.FilePath, sonarRs1);
            fs.AddRuleSetFile(sonarRs2.FilePath, sonarRs2);
            fs.AddRuleSetFile(projectBaseRs.FilePath, projectBaseRs);
            fs.AddRuleSetFile(commonRs1.FilePath, commonRs1);
            fs.AddRuleSetFile(commonRs2.FilePath, commonRs2);

            var inputRuleSet = TestRuleSetHelper.CreateTestRuleSet(projectRoot, "test.ruleset");
            AddRuleSetInclusion(inputRuleSet, projectBaseRs, useRelativePath: true);
            AddRuleSetInclusion(inputRuleSet, commonRs1, useRelativePath: true);
            AddRuleSetInclusion(inputRuleSet, commonRs2, useRelativePath: false);
            AddRuleSetInclusion(inputRuleSet, sonarRs1, useRelativePath: true);
            AddRuleSetInclusion(inputRuleSet, sonarRs2, useRelativePath: false);

            var expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet(projectRoot, "test.ruleset");
            AddRuleSetInclusion(expectedRuleSet, projectBaseRs, useRelativePath: true);
            AddRuleSetInclusion(expectedRuleSet, commonRs1, useRelativePath: true);
            AddRuleSetInclusion(expectedRuleSet, commonRs2, useRelativePath: false);

            // Act
            ProjectRuleSetWriter.RemoveAllIncludesUnderRoot(inputRuleSet, sonarRoot);

            // Verify
            RuleSetAssert.AreEqual(expectedRuleSet, inputRuleSet);
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
        public void ProjectRuleSetWriter_GenerateNewProjectRuleSetPath_FileNameHasDot_AppendsExtension()
        {
            // Setup
            const string ruleSetRootPath = @"X:\";
            const string fileName = "My.File.With.Dots";
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);
            string expected = $"X:\\My.File.With.Dots.{RuleSetWriter.FileExtension}";

            // Act
            string actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Verify
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ProjectRuleSetWriter_GenerateNewProjectRuleSetPath_DesiredFileNameExists_AppendsNumberToFileName()
        {
            // General setup
            const string ruleSetRootPath = @"X:\";
            const string fileName = "NameTaken";
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            // Test case 1: desired name exists
            // Setup
            fileSystem.Files.Add($"X:\\NameTaken.{RuleSetWriter.FileExtension}", null);

            // Act
            string actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Verify
            Assert.AreEqual($"X:\\{fileName}-1.{RuleSetWriter.FileExtension}", actual, "Expected to append running number to desired file name");

            // Test case 2: desired name + 1 + 2 exists
            // Setup
            fileSystem.Files.Add($"X:\\{fileName}-1.{RuleSetWriter.FileExtension}", null);
            fileSystem.Files.Add($"X:\\{fileName}-2.{RuleSetWriter.FileExtension}", null);

            // Act
            actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Verify
            Assert.AreEqual($"X:\\{fileName}-3.{RuleSetWriter.FileExtension}", actual, "Expected to append running number to desired file name");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_GenerateNewProjectRuleSetPath_NoAvailableFileNames_AppendsGuid()
        {
            // Setup
            const string ruleSetRootPath = @"X:\";
            const string fileName = "NameTaken";
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);
            fileSystem.ExistingFilesPattern = new Regex($"X:\\\\{fileName}-?[0-9]*\\.{RuleSetWriter.FileExtension}", RegexOptions.IgnoreCase); // all integer appended files exists

            var expectedGuidFileNamePattern = new Regex($"X:\\\\{fileName}-[A-Z0-9]{{32}}\\.{RuleSetWriter.FileExtension}", RegexOptions.IgnoreCase);

            // Act
            string actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Verify
            StringAssert.Matches(actual, expectedGuidFileNamePattern, "Expected to append GUID to desired file name");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_ShouldIgnoreConfigureRuleSetValue()
        {
            // Test case 1: not ignored
            Assert.IsFalse(ProjectRuleSetWriter.ShouldIgnoreConfigureRuleSetValue("My awesome rule set.ruleset"));

            // Test case 2: ignored
            // Act
            Assert.IsTrue(ProjectRuleSetWriter.ShouldIgnoreConfigureRuleSetValue(null));
            Assert.IsTrue(ProjectRuleSetWriter.ShouldIgnoreConfigureRuleSetValue(" "));
            Assert.IsTrue(ProjectRuleSetWriter.ShouldIgnoreConfigureRuleSetValue("\t"));
            Assert.IsTrue(ProjectRuleSetWriter.ShouldIgnoreConfigureRuleSetValue(ProjectRuleSetWriter.DefaultProjectRuleSet.ToLower(CultureInfo.CurrentCulture)));
            Assert.IsTrue(ProjectRuleSetWriter.ShouldIgnoreConfigureRuleSetValue(ProjectRuleSetWriter.DefaultProjectRuleSet.ToUpper(CultureInfo.CurrentCulture)));
            Assert.IsTrue(ProjectRuleSetWriter.ShouldIgnoreConfigureRuleSetValue(ProjectRuleSetWriter.DefaultProjectRuleSet));
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

            var existingProjectRuleSet = TestRuleSetHelper.CreateTestRuleSet(existingProjectRuleSetPath);
            existingProjectRuleSet.RuleSetIncludes.Add(new RuleSetInclude(existingInclude, RuleAction.Default));

            fileSystem.AddRuleSetFile(existingProjectRuleSetPath, existingProjectRuleSet);
            long initalWriteTimestamp = fileSystem.GetFileTimestamp(existingProjectRuleSetPath);

            fileSystem.AddRuleSetFile(@"X:\MySolution\SolutionRuleSets\sonarqube1.ruleset", new RuleSet("sonar1"));

            var expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet(existingProjectRuleSetPath);
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(expectedInclude, RuleAction.Default));

            // Act
            testSubject.UpdateExistingProjectRuleSet(existingProjectRuleSet, existingProjectRuleSetPath, newSolutionRuleSetPath);

            // Verify
            fileSystem.AssertRuleSetsAreEqual(existingProjectRuleSetPath, expectedRuleSet);

            long newWriteTimestamp = fileSystem.GetFileTimestamp(existingProjectRuleSetPath);
            Assert.IsTrue(newWriteTimestamp > initalWriteTimestamp, $"Expected updated rule set to be written out. File timestamps: initial {initalWriteTimestamp}, final {newWriteTimestamp}");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_ProjectHasExistingRuleSet_AbsolutePathRuleSetIsFound_UnderTheProject()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string ruleSetName = "Happy";
            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";
            const string existingProjectRuleSetPath = @"X:\SolutionDir\ProjectDir\ExistingRuleSet.ruleset";

            RuleSet existingRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { solutionRuleSetPath }
            );
            existingRuleSet.FilePath = existingProjectRuleSetPath;

            fileSystem.AddRuleSetFile(existingProjectRuleSetPath, existingRuleSet);
            fileSystem.AddRuleSetFile(solutionRuleSetPath, new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });


            string newSolutionRuleSetPath = Path.Combine(Path.GetDirectoryName(solutionRuleSetPath), "sonar2.ruleset");
            string newSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, newSolutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { newSolutionRuleSetInclude }
            );

            // Act
            string actualPath = testSubject.WriteProjectLevelRuleSet(projectFullPath, ruleSetName, newSolutionRuleSetPath, existingProjectRuleSetPath);

            // Verify
            fileSystem.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            Assert.AreEqual(existingProjectRuleSetPath, actualPath, "Expecting the rule set to be updated");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RelativePathRuleSetIsFound_UnderTheProject()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string ruleSetName = "Happy";
            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";
            const string existingProjectRuleSetPath = @"X:\SolutionDir\ProjectDir\ExistingRuleSet.ruleset";

            RuleSet existingRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { PathHelper.CalculateRelativePath(projectFullPath, solutionRuleSetPath) }
            );
            existingRuleSet.FilePath = existingProjectRuleSetPath;

            fileSystem.AddRuleSetFile(existingProjectRuleSetPath, existingRuleSet);
            fileSystem.AddRuleSetFile(solutionRuleSetPath, new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });


            string newSolutionRuleSetPath = Path.Combine(Path.GetDirectoryName(solutionRuleSetPath), "sonar2.ruleset");
            string newSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, newSolutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { newSolutionRuleSetInclude }
            );

            // Act
            string actualPath = testSubject.WriteProjectLevelRuleSet(projectFullPath, ruleSetName, newSolutionRuleSetPath, PathHelper.CalculateRelativePath(projectFullPath, existingProjectRuleSetPath));

            // Verify
            fileSystem.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            Assert.AreEqual(existingProjectRuleSetPath, actualPath, "Expecting the rule set to be updated");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_ProjectHasExistingRuleSet_AbsolutePathRuleSetIsFound_ButNotUnderTheProject()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string ruleSetName = "Happy";

            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";
            const string existingProjectRuleSetPath = @"x:\myexistingproject.ruleset";

            fileSystem.AddRuleSetFile(existingProjectRuleSetPath, new RuleSet("NotOurRuleSet") { FilePath = existingProjectRuleSetPath });
            fileSystem.AddRuleSetFile(solutionRuleSetPath, new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[]
                {
                    existingProjectRuleSetPath, // The project exists, but not ours so we should keep it as it was previously specified
                    PathHelper.CalculateRelativePath(projectFullPath, solutionRuleSetPath)
                }
            );

            // Act
            string actualPath = testSubject.WriteProjectLevelRuleSet(projectFullPath, ruleSetName, solutionRuleSetPath, existingProjectRuleSetPath);

            // Verify
            fileSystem.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            Assert.AreNotEqual(existingProjectRuleSetPath, actualPath, "Expecting a new rule set to be created");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RelativePathRuleSetIsFound_ButNotUnderTheSProject()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string ruleSetName = "Happy";

            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";
            const string existingProjectRuleSetPath = @"x:\SolutionDir\myexistingproject.ruleset";

            fileSystem.AddRuleSetFile(existingProjectRuleSetPath, new RuleSet("NotOurRuleSet") { FilePath = existingProjectRuleSetPath });
            fileSystem.AddRuleSetFile(solutionRuleSetPath, new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });

            string relativePathToExistingProjectRuleSet = PathHelper.CalculateRelativePath(existingProjectRuleSetPath, projectFullPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[]
                {
                    relativePathToExistingProjectRuleSet, // The project exists, but not ours so we should keep it as it was previously specified
                    PathHelper.CalculateRelativePath(projectFullPath, solutionRuleSetPath)
                }
            );

            // Act
            string actualPath = testSubject.WriteProjectLevelRuleSet(projectFullPath, ruleSetName, solutionRuleSetPath, relativePathToExistingProjectRuleSet);

            // Verify
            fileSystem.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            Assert.AreNotEqual(existingProjectRuleSetPath, actualPath, "Expecting a new rule set to be created");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RuleSetIsNotFound()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string projectName = "My Project";
            const string ruleSetFileName = "Happy";

            const string solutionRoot = @"X:\SolutionDir";
            string projectRoot = Path.Combine(solutionRoot, "ProjectDir");
            string projectFullPath = Path.Combine(projectRoot, $"{projectName}.proj");
            string currentNonExistingRuleSet = "my-non-existingproject.ruleset";

            string newSolutionRuleSetPath = Path.Combine(solutionRoot, "RuleSets", "sonar2.ruleset");
            string newSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, newSolutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { currentNonExistingRuleSet, newSolutionRuleSetInclude }
            );

            // Act
            string actualPath = testSubject.WriteProjectLevelRuleSet(projectFullPath, ruleSetFileName, newSolutionRuleSetPath, currentNonExistingRuleSet);

            // Verify
            fileSystem.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            Assert.AreNotEqual(currentNonExistingRuleSet, actualPath, "Expecting a new rule set to be created");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_WriteProjectLevelRuleSet_NewBinding()
        {
            // Setup
            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fileSystem);

            const string ruleSetFileName = "Happy";

            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";

            string expectedSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, solutionRuleSetPath);
            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { expectedSolutionRuleSetInclude }
            );

            foreach (var currentRuleSet in new[] { null, string.Empty, ProjectRuleSetWriter.DefaultProjectRuleSet })
            {
                // Act
                string actualPath = testSubject.WriteProjectLevelRuleSet(projectFullPath, ruleSetFileName, solutionRuleSetPath, currentRuleSet);

                // Verify
                fileSystem.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
                Assert.AreNotEqual(solutionRuleSetPath, actualPath, "Expecting a new rule set to be created");
            }
        }

        [TestMethod]
        public void ProjectRuleSetWriter_TryUpdateExistingProjectRuleSet_RuleSetNotAlreadyWritten_WritesFile()
        {
            // Setup
            var fs = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fs);

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";
            string existingRuleSetFullPath = @"X:\SolutionDir\Project\ExistingSharedRuleSet.ruleset";

            string existingRuleSetPropValue = PathHelper.CalculateRelativePath(projectRuleSetRoot, existingRuleSetFullPath);

            fs.AddRuleSetFile(existingRuleSetFullPath, TestRuleSetHelper.CreateTestRuleSet(existingRuleSetFullPath));
            long beforeTimestamp = fs.GetFileTimestamp(existingRuleSetFullPath);

            // Act
            string pathOutResult;
            bool result = testSubject.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, projectRuleSetRoot, existingRuleSetPropValue, out pathOutResult);

            // Verify
            Assert.IsTrue(result, "Expected to return true when trying to update existing rule set");
            Assert.AreEqual(existingRuleSetFullPath, pathOutResult, "Unexpected rule set path was returned");

            long afterTimestamp = fs.GetFileTimestamp(existingRuleSetFullPath);
            Assert.IsTrue(beforeTimestamp < afterTimestamp, "Rule set timestamp has not changed; expected file to be written to.");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_TryUpdateExistingProjectRuleSet_RuleSetAlreadyWritten_DoesNotWriteAgain()
        {
            // Setup
            var fs = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fs);

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";
            string existingRuleSetFullPath = @"X:\SolutionDir\Project\ExistingSharedRuleSet.ruleset";

            string existingRuleSetPropValue = PathHelper.CalculateRelativePath(projectRuleSetRoot, existingRuleSetFullPath);

            testSubject.AlreadyUpdatedExistingRuleSetPaths.Add(existingRuleSetFullPath);
            fs.AddRuleSetFile(existingRuleSetFullPath, new RuleSet("test"));
            long beforeTimestamp = fs.GetFileTimestamp(existingRuleSetFullPath);

            // Act
            string pathOutResult;
            bool result = testSubject.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, projectRuleSetRoot, existingRuleSetPropValue, out pathOutResult);

            // Verify
            Assert.IsTrue(result, "Expected to return true when trying to update already updated existing rule set");
            Assert.AreEqual(existingRuleSetFullPath, pathOutResult, "Unexpected rule set path was returned");

            long afterTimestamp = fs.GetFileTimestamp(existingRuleSetFullPath);
            Assert.AreEqual(beforeTimestamp, afterTimestamp, "Rule set timestamp has changed; file was unexpectedly written to.");
        }

        [TestMethod]
        public void ProjectRuleSetWriter_TryUpdateExistingProjectRuleSet_ExistingRuleSetIsNotAtTheProjectLevel()
        {
            // Setup
            var fs = new ConfigurableRuleSetGenerationFileSystem();
            var testSubject = new ProjectRuleSetWriter(fs);

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";

            string[] cases = new[]
            {
                "../relativeSolutionLevel.ruleset",
                @"..\..\relativeSolutionLevel.ruleset",
                @"X:\SolutionDir\Sonar\absolutionSolutionRooted.ruleset",
                @"c:\OtherPlaceEntirey\rules.ruleset",
                ProjectRuleSetWriter.DefaultProjectRuleSet,
                null,
                string.Empty
            };

            foreach (var currentRuleSet in cases)
            {
                // Act
                string pathOutResult;
                bool result = testSubject.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, projectRuleSetRoot, currentRuleSet, out pathOutResult);

                // Verify
                string testCase = currentRuleSet ?? "NULL";
                Assert.IsNull(pathOutResult, "Unexpected rule set path was returned: {0}. Case: {1}", pathOutResult, testCase);
                Assert.IsFalse(result, "Not expecting to update a non project rooted rulesets. Case: {0}", testCase);
            }
        }

        #endregion

        #region Helpers

        private static void AddRuleSetInclusion(RuleSet parent, RuleSet child, bool useRelativePath)
        {
            string include = useRelativePath
                ? PathHelper.CalculateRelativePath(parent.FilePath, child.FilePath)
                : child.FilePath;
            parent.RuleSetIncludes.Add(new RuleSetInclude(include, RuleAction.Default));
        }

        #endregion
    }
}
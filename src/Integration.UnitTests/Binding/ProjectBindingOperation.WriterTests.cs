//-----------------------------------------------------------------------
// <copyright file="ProjectBindingOperation.WriterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    public partial class ProjectBindingOperationTests
    {
        #region Tests

        [TestMethod]
        public void ProjectBindingOperation_SafeLoadRuleSet()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

            // Test case 1: load existing, well formed rule set
            const string goodRuleSetPath = @"X:\good.ruleset";
            RuleSet goodRuleSet = TestRuleSetHelper.CreateTestRuleSet(numRules: 3);
            rsFS.RegisterRuleSet(goodRuleSet, goodRuleSetPath);

            // Act
            RuleSet loadedGood = testSubject.SafeLoadRuleSet(goodRuleSetPath);

            // Verify
            Assert.IsNotNull(loadedGood, "Expected existing well formed ruleset to be loaded");
            RuleSetAssert.AreEqual(goodRuleSet, loadedGood);

            // Test case 2: load existing, badly formed rule set (empty)
            const string badRuleSetPath = @"X:\bad.ruleset";
            rsFS.RegisterRuleSet(null, badRuleSetPath);

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
        public void ProjectBindingOperation_GenerateNewProjectRuleSetPath_FileNameHasDot_AppendsExtension()
        {
            // Setup
            const string ruleSetRootPath = @"X:\";
            const string fileName = "My.File.With.Dots";
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            string expected = $"X:\\My.File.With.Dots.ruleset";

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
        public void ProjectBindingOperation_GenerateNewProjectRuleSetPath_DesiredFileNameExists_AppendsNumberToFileName()
        {
            // General setup
            const string ruleSetRootPath = @"X:\";
            const string fileName = "NameTaken";
            ProjectBindingOperation testSubject = this.CreateTestSubject();

            // Test case 1: desired name exists
            // Setup
            this.sccFileSystem.RegisterFile($"X:\\NameTaken.ruleset");

            // Act
            string actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Verify
            Assert.AreEqual($"X:\\{fileName}-1.ruleset", actual, "Expected to append running number to desired file name, skipping the default one since exists");

            // Test case 2: desired name + 1 + 2 exists
            // Setup
            this.sccFileSystem.RegisterFile(@"X:\NameTaken-1.ruleset");
            this.sccFileSystem.RegisterFile(@"X:\NameTaken-2.ruleset");

            // Act
            actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Verify
            Assert.AreEqual(@"X:\NameTaken-3.ruleset", actual, "Expected to append running number to desired file name");

            // Test case 3: has a pending write (not exists yet)
            ((ISourceControlledFileSystem)this.sccFileSystem).QueueFileWrite(@"X:\NameTaken-3.ruleset", () => true);

            // Act
            actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Verify
            Assert.AreEqual(@"X:\NameTaken-4.ruleset", actual, "Expected to append running number to desired file name, skipping 3 since pending write");

        }

        [TestMethod]
        public void ProjectBindingOperation_GenerateNewProjectRuleSetPath_NoAvailableFileNames_AppendsGuid()
        {
            // Setup
            const string fileName = "fileTaken";
            const string ruleSetRootPath = @"X:\";
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            string[] existingFiles = Enumerable.Range(0, 10).Select(i => $@"X:\{fileName}-{i}.ruleset").ToArray();
            this.sccFileSystem.RegisterFile($@"X:\{fileName}.ruleset");
            this.sccFileSystem.RegisterFiles(existingFiles);


            // Act
            string actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Verify
            string actualFileName = Path.GetFileNameWithoutExtension(actual);
            Assert.AreEqual(fileName.Length + 32 + 1, actualFileName.Length, "Expected to append GUID to desired file name, actual: " + actualFileName);
        }

        [TestMethod]
        public void ProjectBindingOperation_ShouldIgnoreConfigureRuleSetValue()
        {
            // Test case 1: not ignored
            Assert.IsFalse(ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue("My awesome rule set.ruleset"));

            // Test case 2: ignored
            // Act
            Assert.IsTrue(ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(null));
            Assert.IsTrue(ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(" "));
            Assert.IsTrue(ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue("\t"));
            Assert.IsTrue(ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(ProjectBindingOperation.DefaultProjectRuleSet.ToLower(CultureInfo.CurrentCulture)));
            Assert.IsTrue(ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(ProjectBindingOperation.DefaultProjectRuleSet.ToUpper(CultureInfo.CurrentCulture)));
            Assert.IsTrue(ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(ProjectBindingOperation.DefaultProjectRuleSet));
        }

        [TestMethod]
        public void ProjectBindingOperation_GenerateNewProjectRuleSet()
        {
            // Setup
            const string solutionIncludePath = @"..\..\solution.ruleset";
            const string currentRuleSetPath = @"X:\MyOriginal.ruleset";
            var expectedRuleSet = new RuleSet(Constants.RuleSetName);
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(solutionIncludePath, RuleAction.Default));
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(currentRuleSetPath, RuleAction.Default));

            // Act
            RuleSet actualRuleSet = ProjectBindingOperation.GenerateNewProjectRuleSet(solutionIncludePath, currentRuleSetPath);

            // Verify
            RuleSetAssert.AreEqual(expectedRuleSet, actualRuleSet);
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_AbsolutePathRuleSetIsFound_UnderTheProject()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

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

            rsFS.RegisterRuleSet(existingRuleSet);
            rsFS.RegisterRuleSet(new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });


            string newSolutionRuleSetPath = Path.Combine(Path.GetDirectoryName(solutionRuleSetPath), "sonar2.ruleset");
            string newSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, newSolutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { newSolutionRuleSetInclude }
            );

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, newSolutionRuleSetPath, existingProjectRuleSetPath);

            // Verify
            rsFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            Assert.AreEqual(existingProjectRuleSetPath, actualPath, "Expecting the rule set to be updated");
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RelativePathRuleSetIsFound_UnderTheProject()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

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

            rsFS.RegisterRuleSet(existingRuleSet);
            rsFS.RegisterRuleSet(new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });


            string newSolutionRuleSetPath = Path.Combine(Path.GetDirectoryName(solutionRuleSetPath), "sonar2.ruleset");
            string newSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, newSolutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { newSolutionRuleSetInclude }
            );

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, newSolutionRuleSetPath, PathHelper.CalculateRelativePath(projectFullPath, existingProjectRuleSetPath));

            // Verify
            rsFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            Assert.AreEqual(existingProjectRuleSetPath, actualPath, "Expecting the rule set to be updated");
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_AbsolutePathRuleSetIsFound_ButNotUnderTheProject()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

            const string ruleSetName = "Happy";

            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";
            const string existingProjectRuleSetPath = @"x:\myexistingproject.ruleset";

            rsFS.RegisterRuleSet(new RuleSet("NotOurRuleSet") { FilePath = existingProjectRuleSetPath });
            rsFS.RegisterRuleSet(new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });

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
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, solutionRuleSetPath, existingProjectRuleSetPath);

            // Verify
            rsFS.AssertRuleSetNotExists(actualPath);
            Assert.AreNotEqual(existingProjectRuleSetPath, actualPath, "Expecting a new rule set to be created once written pending");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify
            rsFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RelativePathRuleSetIsFound_ButNotUnderTheSProject()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

            const string ruleSetName = "Happy";

            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";
            const string existingProjectRuleSetPath = @"x:\SolutionDir\myexistingproject.ruleset";

            rsFS.RegisterRuleSet(new RuleSet("NotOurRuleSet") { FilePath = existingProjectRuleSetPath });
            rsFS.RegisterRuleSet(new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });

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
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, solutionRuleSetPath, relativePathToExistingProjectRuleSet);

            // Verify
            rsFS.AssertRuleSetNotExists(actualPath);
            Assert.AreNotEqual(existingProjectRuleSetPath, actualPath, "Expecting a new rule set to be created once written pending");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify
            rsFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RuleSetIsNotFound()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

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
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetFileName, newSolutionRuleSetPath, currentNonExistingRuleSet);

            // Verify
            rsFS.AssertRuleSetNotExists(actualPath);
            Assert.AreNotEqual(currentNonExistingRuleSet, actualPath, "Expecting a new rule set to be created once written pending");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify
            rsFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_NewBinding()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

            const string ruleSetFileName = "Happy";
            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";

            string expectedSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, solutionRuleSetPath);
            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { expectedSolutionRuleSetInclude }
            );

            List<string> filesPending = new List<string>();
            foreach (var currentRuleSet in new[] { null, string.Empty, ProjectBindingOperation.DefaultProjectRuleSet })
            {
                // Act
                string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetFileName, solutionRuleSetPath, currentRuleSet);
                filesPending.Add(actualPath);

                // Verify
                rsFS.AssertRuleSetNotExists(actualPath);
                Assert.AreNotEqual(solutionRuleSetPath, actualPath, "Expecting a new rule set to be created once pending were written");
            }

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

                // Verify
            foreach (var pending in filesPending)
            {
                // Verify
                rsFS.AssertRuleSetsAreEqual(pending, expectedRuleSet);
            }
        }

        [TestMethod]
        public void ProjectBindingOperation_TryUpdateExistingProjectRuleSet_RuleSetNotAlreadyWritten_WritesFile()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";
            string existingRuleSetFullPath = @"X:\SolutionDir\Project\ExistingSharedRuleSet.ruleset";

            string existingRuleSetPropValue = PathHelper.CalculateRelativePath(projectRuleSetRoot, existingRuleSetFullPath);

            var existingRuleSet = TestRuleSetHelper.CreateTestRuleSet(existingRuleSetFullPath);
            rsFS.RegisterRuleSet(existingRuleSet, existingRuleSetFullPath);
            long beforeTimestamp = this.sccFileSystem.GetFileTimestamp(existingRuleSetFullPath);

            // Act
            string pathOutResult;
            RuleSet rsOutput;
            bool result = testSubject.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, projectRuleSetRoot, existingRuleSetPropValue, out pathOutResult, out rsOutput);

            // Verify
            Assert.IsTrue(result, "Expected to return true when trying to update existing rule set");
            Assert.AreSame(existingRuleSet, rsOutput, "Same RuleSet instance expected");
            Assert.AreEqual(existingRuleSetFullPath, pathOutResult, "Unexpected rule set path was returned");
            this.sccFileSystem.AssertFileTimestamp(existingRuleSetFullPath, beforeTimestamp);
        }

        [TestMethod]
        public void ProjectBindingOperation_TryUpdateExistingProjectRuleSet_RuleSetAlreadyWritten_DoesNotWriteAgain()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";
            string existingRuleSetFullPath = @"X:\SolutionDir\Project\ExistingSharedRuleSet.ruleset";

            string existingRuleSetPropValue = PathHelper.CalculateRelativePath(projectRuleSetRoot, existingRuleSetFullPath);

            var existingRuleSet = new RuleSet("test") { FilePath = existingRuleSetFullPath };
            testSubject.AlreadyUpdatedExistingRuleSetPaths.Add(existingRuleSet.FilePath, existingRuleSet);
            rsFS.RegisterRuleSet(existingRuleSet);
            long beforeTimestamp = this.sccFileSystem.GetFileTimestamp(existingRuleSetFullPath);

            // Act
            string pathOutResult;
            RuleSet rsOutput;
            bool result = testSubject.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, projectRuleSetRoot, existingRuleSetPropValue, out pathOutResult, out rsOutput);

            // Verify
            Assert.IsTrue(result, "Expected to return true when trying to update already updated existing rule set");
            Assert.AreSame(existingRuleSet, rsOutput, "Same RuleSet instance is expected");
            Assert.AreEqual(existingRuleSetFullPath, pathOutResult, "Unexpected rule set path was returned");
            this.sccFileSystem.AssertFileTimestamp(existingRuleSetFullPath, beforeTimestamp);
        }

        [TestMethod]
        public void ProjectBindingOperation_TryUpdateExistingProjectRuleSet_ExistingRuleSetIsNotAtTheProjectLevel()
        {
            // Setup
            var rsFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            ProjectBindingOperation testSubject = this.CreateTestSubject(rsFS);

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";

            string[] cases = new[]
            {
                "../relativeSolutionLevel.ruleset",
                @"..\..\relativeSolutionLevel.ruleset",
                @"X:\SolutionDir\Sonar\absolutionSolutionRooted.ruleset",
                @"c:\OtherPlaceEntirey\rules.ruleset",
                ProjectBindingOperation.DefaultProjectRuleSet,
                null,
                string.Empty
            };

            foreach (var currentRuleSet in cases)
            {
                // Act
                string pathOutResult;
                RuleSet rsOutput;
                bool result = testSubject.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, projectRuleSetRoot, currentRuleSet, out pathOutResult, out rsOutput);

                // Verify
                string testCase = currentRuleSet ?? "NULL";
                Assert.IsNull(pathOutResult, "Unexpected rule set path was returned: {0}. Case: {1}", pathOutResult, testCase);
                Assert.IsNull(rsOutput, "Unexpected rule set was returned. Case: {0}", testCase);
                Assert.IsFalse(result, "Not expecting to update a non project rooted rulesets. Case: {0}", testCase);
            }
        }

        #endregion
    }
}
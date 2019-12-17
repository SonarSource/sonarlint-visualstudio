/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    public partial class ProjectBindingOperationTests
    {
        #region Tests

        [TestMethod]
        public void ProjectBindingOperation_GenerateNewProjectRuleSetPath_FileNameHasDot_AppendsExtension()
        {
            // Arrange
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

            // Assert
            actual.Should().Be(expected);
        }

        [TestMethod]
        public void ProjectBindingOperation_GenerateNewProjectRuleSetPath_DesiredFileNameExists_AppendsNumberToFileName()
        {
            // General setup
            const string ruleSetRootPath = @"X:\";
            const string fileName = "NameTaken";
            ProjectBindingOperation testSubject = this.CreateTestSubject();

            // Test case 1: desired name exists
            // Arrange
            this.sccFileSystem.RegisterFile($"X:\\NameTaken.ruleset");

            // Act
            string actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Assert
            actual.Should().Be($"X:\\{fileName}-1.ruleset", "Expected to append running number to desired file name, skipping the default one since exists");

            // Test case 2: desired name + 1 + 2 exists
            // Arrange
            this.sccFileSystem.RegisterFile(@"X:\NameTaken-1.ruleset");
            this.sccFileSystem.RegisterFile(@"X:\NameTaken-2.ruleset");

            // Act
            actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Assert
            actual.Should().Be(@"X:\NameTaken-3.ruleset", "Expected to append running number to desired file name");

            // Test case 3: has a pending write (not exists yet)
            ((ISourceControlledFileSystem)this.sccFileSystem).QueueFileWrite(@"X:\NameTaken-3.ruleset", () => true);

            // Act
            actual = testSubject.GenerateNewProjectRuleSetPath
            (
                ruleSetRootPath,
                fileName
            );

            // Assert
            actual.Should().Be(@"X:\NameTaken-4.ruleset", "Expected to append running number to desired file name, skipping 3 since pending write");
        }

        [TestMethod]
        public void ProjectBindingOperation_GenerateNewProjectRuleSetPath_NoAvailableFileNames_AppendsGuid()
        {
            // Arrange
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

            // Assert
            string actualFileName = Path.GetFileNameWithoutExtension(actual);
            actualFileName.Should().HaveLength(fileName.Length + 32 + 1, "Expected to append GUID to desired file name, actual: " + actualFileName);
        }

        [TestMethod]
        public void ProjectBindingOperation_ShouldIgnoreConfigureRuleSetValue()
        {
            // Test case 1: not ignored
            ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue("My awesome rule set.ruleset").Should().BeFalse();

            // Test case 2: ignored
            // Act
            ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(null).Should().BeTrue();
            ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(" ").Should().BeTrue();
            ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue("\t").Should().BeTrue();
            ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(ProjectBindingOperation.DefaultProjectRuleSet.ToLower(CultureInfo.CurrentCulture)).Should().BeTrue();
            ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(ProjectBindingOperation.DefaultProjectRuleSet.ToUpper(CultureInfo.CurrentCulture)).Should().BeTrue();
            ProjectBindingOperation.ShouldIgnoreConfigureRuleSetValue(ProjectBindingOperation.DefaultProjectRuleSet).Should().BeTrue();
        }

        [TestMethod]
        public void ProjectBindingOperation_GenerateNewProjectRuleSet()
        {
            // Arrange
            const string solutionIncludePath = @"..\..\solution.ruleset";
            const string currentRuleSetPath = @"X:\MyOriginal.ruleset";
            var expectedRuleSet = new RuleSet(Constants.RuleSetName);
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(solutionIncludePath, RuleAction.Default));
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(currentRuleSetPath, RuleAction.Default));

            // Act
            RuleSet actualRuleSet = ProjectBindingOperation.GenerateNewProjectRuleSet(solutionIncludePath, currentRuleSetPath, Constants.RuleSetName);

            // Assert
            RuleSetAssert.AreEqual(expectedRuleSet, actualRuleSet);
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_AbsolutePathRuleSetIsFound_UnderTheProject()
        {
            // Arrange
            ProjectBindingOperation testSubject = this.CreateTestSubject();

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

            this.ruleSetFS.RegisterRuleSet(existingRuleSet);
            this.ruleSetFS.RegisterRuleSet(new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });

            string newSolutionRuleSetPath = Path.Combine(Path.GetDirectoryName(solutionRuleSetPath), "sonar2.ruleset");
            string newSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, newSolutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { newSolutionRuleSetInclude }
            );
            var dotNetConfig = new DotNetBindingConfigFile(expectedRuleSet);

            var ruleSetInfo = new RuleSetInformation(Language.CSharp, dotNetConfig) { NewRuleSetFilePath = newSolutionRuleSetPath };

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, ruleSetInfo, existingProjectRuleSetPath);

            // Assert
            this.ruleSetFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            actualPath.Should().Be(existingProjectRuleSetPath, "Expecting the rule set to be updated");
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RelativePathRuleSetIsFound_UnderTheProject()
        {
            // Arrange
            ProjectBindingOperation testSubject = this.CreateTestSubject();

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

            this.ruleSetFS.RegisterRuleSet(existingRuleSet);
            this.ruleSetFS.RegisterRuleSet(new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });

            string newSolutionRuleSetPath = Path.Combine(Path.GetDirectoryName(solutionRuleSetPath), "sonar2.ruleset");
            string newSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, newSolutionRuleSetPath);

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { newSolutionRuleSetInclude }
            );
            var dotNetRuleSet = new DotNetBindingConfigFile(expectedRuleSet);

            var ruleSetInfo = new RuleSetInformation(Language.CSharp, dotNetRuleSet) { NewRuleSetFilePath = newSolutionRuleSetPath };

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, ruleSetInfo, PathHelper.CalculateRelativePath(projectFullPath, existingProjectRuleSetPath));

            // Assert
            this.ruleSetFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            actualPath.Should().Be(existingProjectRuleSetPath, "Expecting the rule set to be updated");
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_AbsolutePathRuleSetIsFound_ButNotUnderTheProject()
        {
            // Arrange
            ProjectBindingOperation testSubject = this.CreateTestSubject();

            const string ruleSetName = "Happy";

            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";
            const string existingProjectRuleSetPath = @"x:\myexistingproject.ruleset";

            this.ruleSetFS.RegisterRuleSet(new RuleSet("NotOurRuleSet") { FilePath = existingProjectRuleSetPath });
            this.ruleSetFS.RegisterRuleSet(new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[]
                {
                    existingProjectRuleSetPath, // The project exists, but not ours so we should keep it as it was previously specified
                    PathHelper.CalculateRelativePath(projectFullPath, solutionRuleSetPath)
                }
            );
            var dotNetRuleSet = new DotNetBindingConfigFile(expectedRuleSet);

            var ruleSetInfo = new RuleSetInformation(Language.CSharp, dotNetRuleSet) { NewRuleSetFilePath = solutionRuleSetPath };

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, ruleSetInfo, existingProjectRuleSetPath);

            // Assert
            this.ruleSetFS.AssertRuleSetNotExists(actualPath);
            actualPath.Should().NotBe(existingProjectRuleSetPath, "Expecting a new rule set to be created once written pending");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            this.ruleSetFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RelativePathRuleSetIsFound_ButNotUnderTheSProject()
        {
            // Arrange
            ProjectBindingOperation testSubject = this.CreateTestSubject();

            const string ruleSetName = "Happy";

            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";
            const string existingProjectRuleSetPath = @"x:\SolutionDir\myexistingproject.ruleset";

            this.ruleSetFS.RegisterRuleSet(new RuleSet("NotOurRuleSet") { FilePath = existingProjectRuleSetPath });
            this.ruleSetFS.RegisterRuleSet(new RuleSet("SolutionRuleSet") { FilePath = solutionRuleSetPath });

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
            var dotNetRuleSet = new DotNetBindingConfigFile(expectedRuleSet);

            var ruleSetInfo = new RuleSetInformation(Language.CSharp, dotNetRuleSet) { NewRuleSetFilePath = solutionRuleSetPath };

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, ruleSetInfo, relativePathToExistingProjectRuleSet);

            // Assert
            this.ruleSetFS.AssertRuleSetNotExists(actualPath);
            actualPath.Should().NotBe(existingProjectRuleSetPath, "Expecting a new rule set to be created once written pending");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            this.ruleSetFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RuleSetIsNotFound()
        {
            // Arrange
            ProjectBindingOperation testSubject = this.CreateTestSubject();

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
            var dotNetRuleSet = new DotNetBindingConfigFile(expectedRuleSet);

            var ruleSetInfo = new RuleSetInformation(Language.CSharp, dotNetRuleSet) { NewRuleSetFilePath = newSolutionRuleSetPath };

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetFileName, ruleSetInfo, currentNonExistingRuleSet);

            // Assert
            this.ruleSetFS.AssertRuleSetNotExists(actualPath);
            actualPath.Should().NotBe(currentNonExistingRuleSet, "Expecting a new rule set to be created once written pending");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            this.ruleSetFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_NewBinding()
        {
            // Arrange
            ProjectBindingOperation testSubject = this.CreateTestSubject();

            const string ruleSetFileName = "Happy";
            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";

            string expectedSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, solutionRuleSetPath);
            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { expectedSolutionRuleSetInclude }
            );
            var dotNetRuleSet = new DotNetBindingConfigFile(expectedRuleSet);

            var ruleSetInfo = new RuleSetInformation(Language.CSharp, dotNetRuleSet) { NewRuleSetFilePath = solutionRuleSetPath };

            List<string> filesPending = new List<string>();
            foreach (var currentRuleSet in new[] { null, string.Empty, ProjectBindingOperation.DefaultProjectRuleSet })
            {
                // Act
                string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetFileName, ruleSetInfo, currentRuleSet);
                filesPending.Add(actualPath);

                // Assert
                this.ruleSetFS.AssertRuleSetNotExists(actualPath);
                actualPath.Should().NotBe(solutionRuleSetPath, "Expecting a new rule set to be created once pending were written");
            }

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            foreach (var pending in filesPending)
            {
                // Assert
                this.ruleSetFS.AssertRuleSetsAreEqual(pending, expectedRuleSet);
            }
        }

        [TestMethod]
        public void ProjectBindingOperation_TryUpdateExistingProjectRuleSet_RuleSetNotAlreadyWritten_WritesFile()
        {
            // Arrange
            ProjectBindingOperation testSubject = this.CreateTestSubject();

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";
            string existingRuleSetFullPath = @"X:\SolutionDir\Project\ExistingSharedRuleSet.ruleset";

            string existingRuleSetPropValue = PathHelper.CalculateRelativePath(projectRuleSetRoot, existingRuleSetFullPath);

            var existingRuleSet = TestRuleSetHelper.CreateTestRuleSet(existingRuleSetFullPath);
            this.ruleSetFS.RegisterRuleSet(existingRuleSet, existingRuleSetFullPath);
            long beforeTimestamp = this.sccFileSystem.GetFileTimestamp(existingRuleSetFullPath);

            // Act
            string pathOutResult;
            RuleSet rsOutput;
            bool result = testSubject.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, projectRuleSetRoot, existingRuleSetPropValue, out pathOutResult, out rsOutput);

            // Assert
            result.Should().BeTrue("Expected to return true when trying to update existing rule set");
            rsOutput.Should().Be(existingRuleSet, "Same RuleSet instance expected");
            pathOutResult.Should().Be(existingRuleSetFullPath, "Unexpected rule set path was returned");
            this.sccFileSystem.AssertFileTimestamp(existingRuleSetFullPath, beforeTimestamp);
        }

        [TestMethod]
        public void ProjectBindingOperation_TryUpdateExistingProjectRuleSet_RuleSetAlreadyWritten_DoesNotWriteAgain()
        {
            // Arrange
            ProjectBindingOperation testSubject = this.CreateTestSubject();

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";
            string existingRuleSetFullPath = @"X:\SolutionDir\Project\ExistingSharedRuleSet.ruleset";

            string existingRuleSetPropValue = PathHelper.CalculateRelativePath(projectRuleSetRoot, existingRuleSetFullPath);

            var existingRuleSet = new RuleSet("test") { FilePath = existingRuleSetFullPath };
            testSubject.AlreadyUpdatedExistingRuleSetPaths.Add(existingRuleSet.FilePath, existingRuleSet);
            this.ruleSetFS.RegisterRuleSet(existingRuleSet);
            long beforeTimestamp = this.sccFileSystem.GetFileTimestamp(existingRuleSetFullPath);

            // Act
            string pathOutResult;
            RuleSet rsOutput;
            bool result = testSubject.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, projectRuleSetRoot, existingRuleSetPropValue, out pathOutResult, out rsOutput);

            // Assert
            result.Should().BeTrue("Expected to return true when trying to update already updated existing rule set");
            rsOutput.Should().Be(existingRuleSet, "Same RuleSet instance is expected");
            pathOutResult.Should().Be(existingRuleSetFullPath, "Unexpected rule set path was returned");
            this.sccFileSystem.AssertFileTimestamp(existingRuleSetFullPath, beforeTimestamp);
        }

        [TestMethod]
        public void ProjectBindingOperation_TryUpdateExistingProjectRuleSet_ExistingRuleSetIsNotAtTheProjectLevel()
        {
            // Arrange
            ProjectBindingOperation testSubject = this.CreateTestSubject();

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

                // Assert
                string testCase = currentRuleSet ?? "NULL";
                pathOutResult.Should().BeNull("Unexpected rule set path was returned: {0}. Case: {1}", pathOutResult, testCase);
                rsOutput.Should().BeNull("Unexpected rule set was returned. Case: {0}", testCase);
                result.Should().BeFalse("Not expecting to update a non project rooted rulesets. Case: {0}", testCase);
            }
        }

        #endregion Tests
    }
}

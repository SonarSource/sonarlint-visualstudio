/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration.Binding;
using RuleAction = Microsoft.VisualStudio.CodeAnalysis.RuleSets.RuleAction;
using RuleSet = Microsoft.VisualStudio.CodeAnalysis.RuleSets.RuleSet;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    public partial class CSharpVBBindingOperationTests
    {
        #region Tests

        [TestMethod]
        public void ProjectBindingOperation_GenerateNewProjectRuleSetPath_FileNameHasDot_AppendsExtension()
        {
            // Arrange
            const string ruleSetRootPath = @"X:\";
            const string fileName = "My.File.With.Dots";
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
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
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();

            // Test case 1: desired name exists
            // Arrange
            fileSystem.AddFile($"X:\\NameTaken.ruleset", new MockFileData(""));

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
            fileSystem.AddFile(@"X:\NameTaken-1.ruleset", new MockFileData(""));
            fileSystem.AddFile(@"X:\NameTaken-2.ruleset", new MockFileData(""));

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
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            string[] existingFiles = Enumerable.Range(0, 10).Select(i => $@"X:\{fileName}-{i}.ruleset").ToArray();

            fileSystem.AddFile($@"X:\{fileName}.ruleset", new MockFileData(""));

            foreach (var existingFile in existingFiles)
            {
                fileSystem.AddFile(existingFile, new MockFileData(""));
            }

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
        public void ProjectBindingOperation_IsDefaultRuleSet()
        {
            // Test case 1: not ignored
            CSharpVBBindingOperation.IsDefaultRuleSet("My awesome rule set.ruleset").Should().BeFalse();

            // Test case 2: ignored
            // Act
            CSharpVBBindingOperation.IsDefaultRuleSet(null).Should().BeTrue();
            CSharpVBBindingOperation.IsDefaultRuleSet(" ").Should().BeTrue();
            CSharpVBBindingOperation.IsDefaultRuleSet("\t").Should().BeTrue();
            CSharpVBBindingOperation.IsDefaultRuleSet(CSharpVBBindingOperation.DefaultProjectRuleSet.ToLower(CultureInfo.CurrentCulture)).Should().BeTrue();
            CSharpVBBindingOperation.IsDefaultRuleSet(CSharpVBBindingOperation.DefaultProjectRuleSet.ToUpper(CultureInfo.CurrentCulture)).Should().BeTrue();
            CSharpVBBindingOperation.IsDefaultRuleSet(CSharpVBBindingOperation.DefaultProjectRuleSet).Should().BeTrue();
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
            RuleSet actualRuleSet = CSharpVBBindingOperation.GenerateNewProjectRuleSet(solutionIncludePath, currentRuleSetPath, Constants.RuleSetName);

            // Assert
            RuleSetAssert.AreEqual(expectedRuleSet, actualRuleSet);
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_AbsolutePathRuleSetIsFound_UnderTheProject()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();

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

            var csharpVbConfig = CreateCSharpVbBindingConfig(newSolutionRuleSetPath, expectedRuleSet);

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, csharpVbConfig, existingProjectRuleSetPath);

            // Assert
            this.ruleSetFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            actualPath.Should().Be(existingProjectRuleSetPath, "Expecting the rule set to be updated");
        }

        private static CSharpVBBindingConfig CreateCSharpVbBindingConfig(string newSolutionRuleSetPath, RuleSet expectedRuleSet)
        {
            var ruleset = new FilePathAndContent<RuleSet>(newSolutionRuleSetPath, expectedRuleSet);
            var additionalFile = new FilePathAndContent<SonarLintConfiguration>("dummy.txt", new SonarLintConfiguration());
            var csharpVbConfig = new CSharpVBBindingConfig(ruleset, additionalFile);
            return csharpVbConfig;
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_RelativePathRuleSetIsFound_UnderTheProject()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();

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
            var csharpVbConfig = CreateCSharpVbBindingConfig(newSolutionRuleSetPath, expectedRuleSet);

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, csharpVbConfig, PathHelper.CalculateRelativePath(projectFullPath, existingProjectRuleSetPath));

            // Assert
            this.ruleSetFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
            actualPath.Should().Be(existingProjectRuleSetPath, "Expecting the rule set to be updated");
        }

        [TestMethod]
        public void ProjectBindingOperation_QueueWriteProjectLevelRuleSet_ProjectHasExistingRuleSet_AbsolutePathRuleSetIsFound_ButNotUnderTheProject()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();

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
            var csharpVbConfig = CreateCSharpVbBindingConfig(solutionRuleSetPath, expectedRuleSet);

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, csharpVbConfig, existingProjectRuleSetPath);

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
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();

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
            var csharpVbConfig = CreateCSharpVbBindingConfig(solutionRuleSetPath, expectedRuleSet);

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetName, csharpVbConfig, relativePathToExistingProjectRuleSet);

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
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();

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
            var csharpVbConfig = CreateCSharpVbBindingConfig(newSolutionRuleSetPath, expectedRuleSet);

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetFileName, csharpVbConfig, currentNonExistingRuleSet);

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
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();

            const string ruleSetFileName = "Happy";
            const string projectFullPath = @"X:\SolutionDir\ProjectDir\My Project.proj";
            const string solutionRuleSetPath = @"X:\SolutionDir\RuleSets\sonar1.ruleset";

            string expectedSolutionRuleSetInclude = PathHelper.CalculateRelativePath(projectFullPath, solutionRuleSetPath);
            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet
            (
                numRules: 0,
                includes: new[] { expectedSolutionRuleSetInclude, "MyCustomRuleSet.ruleset" }
            );
            var csharpVbConfig = CreateCSharpVbBindingConfig(solutionRuleSetPath, expectedRuleSet);

            // Act
            string actualPath = testSubject.QueueWriteProjectLevelRuleSet(projectFullPath, ruleSetFileName, csharpVbConfig, "MyCustomRuleSet.ruleset");

            // Assert
            this.ruleSetFS.AssertRuleSetNotExists(actualPath);
            actualPath.Should().NotBe(solutionRuleSetPath, "Expecting a new rule set to be created once pending were written");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            this.ruleSetFS.AssertRuleSetsAreEqual(actualPath, expectedRuleSet);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ProjectBindingOperation_TryUpdateExistingProjectRuleSet_RuleSetSaved(bool doesAlreadyExist)
        {
            // Arrange
            CSharpVBBindingOperation testSubject = CreateTestSubject();

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";
            string existingRuleSetFullPath = @"X:\SolutionDir\Project\ExistingSharedRuleSet.ruleset";
            string existingRuleSetPropValue = PathHelper.CalculateRelativePath(projectRuleSetRoot, existingRuleSetFullPath);

            var existingRuleSet = TestRuleSetHelper.CreateTestRuleSet(existingRuleSetFullPath);

            if (doesAlreadyExist)
            {
                testSubject.AlreadyUpdatedExistingRuleSetPaths.Add(existingRuleSet.FilePath, existingRuleSet);

            }
            this.ruleSetFS.RegisterRuleSet(existingRuleSet);

            // Act
            string pathOutResult;
            RuleSet rsOutput;
            bool result = testSubject.TryUpdateExistingProjectRuleSet(solutionRuleSetPath, projectRuleSetRoot, existingRuleSetPropValue, out pathOutResult, out rsOutput);

            // Assert
            result.Should().BeTrue("Expected to return true when trying to update existing rule set");
            rsOutput.Should().Be(existingRuleSet, "Same RuleSet instance is expected");
            pathOutResult.Should().Be(existingRuleSetFullPath, "Unexpected rule set path was returned");
        }

        [TestMethod]
        public void ProjectBindingOperation_TryUpdateExistingProjectRuleSet_ExistingRuleSetIsNotAtTheProjectLevel()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();

            string solutionRuleSetPath = @"X:\SolutionDir\Sonar\Sonar1.ruleset";
            string projectRuleSetRoot = @"X:\SolutionDir\Project\";

            string[] cases = new[]
            {
                "../relativeSolutionLevel.ruleset",
                @"..\..\relativeSolutionLevel.ruleset",
                @"X:\SolutionDir\Sonar\absolutionSolutionRooted.ruleset",
                @"c:\OtherPlaceEntirey\rules.ruleset",
                "MyCustomRuleSet.ruleset"
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

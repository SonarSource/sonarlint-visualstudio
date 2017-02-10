/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class RuleSetHelperTests
    {
        #region Tests

        [TestMethod]
        public void RuleSetHelper_RemoveAllIncludesUnderRoot()
        {
            // Arrange
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
            RuleSetHelper.RemoveAllIncludesUnderRoot(inputRuleSet, sonarRoot);

            // Assert
            RuleSetAssert.AreEqual(expectedRuleSet, inputRuleSet);
        }

        [TestMethod]
        public void RuleSetHelper_FindAllIncludesUnderRoot()
        {
            // Arrange
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

            var inputRuleSet = TestRuleSetHelper.CreateTestRuleSet(projectRoot, "test.ruleset");
            AddRuleSetInclusion(inputRuleSet, projectBaseRs, useRelativePath: true);
            AddRuleSetInclusion(inputRuleSet, commonRs1, useRelativePath: true);
            AddRuleSetInclusion(inputRuleSet, commonRs2, useRelativePath: false);
            var expected1 = AddRuleSetInclusion(inputRuleSet, sonarRs1, useRelativePath: true);
            var expected2 = AddRuleSetInclusion(inputRuleSet, sonarRs2, useRelativePath: false);

            // Act
            RuleSetInclude[] actual = RuleSetHelper.FindAllIncludesUnderRoot(inputRuleSet, sonarRoot).ToArray();

            // Assert
            CollectionAssert.AreEquivalent(new[] { expected1, expected2 }, actual);
        }

        [TestMethod]
        public void RuleSetHelper_UpdateExistingProjectRuleSet()
        {
            // Arrange
            const string existingProjectRuleSetPath = @"X:\MySolution\ProjectOne\proj1.ruleset";
            const string existingInclude = @"..\SolutionRuleSets\sonarqube1.ruleset";

            const string newSolutionRuleSetPath = @"X:\MySolution\SolutionRuleSets\sonarqube2.ruleset";
            const string expectedInclude = @"..\SolutionRuleSets\sonarqube2.ruleset";

            var existingProjectRuleSet = TestRuleSetHelper.CreateTestRuleSet(existingProjectRuleSetPath);
            existingProjectRuleSet.RuleSetIncludes.Add(new RuleSetInclude(existingInclude, RuleAction.Default));

            var expectedRuleSet = TestRuleSetHelper.CreateTestRuleSet(existingProjectRuleSetPath);
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(expectedInclude, RuleAction.Default));

            // Act
            RuleSetHelper.UpdateExistingProjectRuleSet(existingProjectRuleSet, newSolutionRuleSetPath);

            // Assert
            RuleSetAssert.AreEqual(expectedRuleSet, existingProjectRuleSet, "Update should delete previous solution rulesets, and replace them with a new one provide");
        }

        [TestMethod]
        public void RuleSetHelper_FindInclude_ArgChecks()
        {
            RuleSet rs = new RuleSet("Name", @"c:\path.ruleset");

            Exceptions.Expect<ArgumentNullException>(() => RuleSetHelper.FindInclude(null, rs));
            Exceptions.Expect<ArgumentNullException>(() => RuleSetHelper.FindInclude(rs, null));
        }

        [TestMethod]
        public void RuleSetHelper_FindInclude()
        {
            // Arrange
            RuleSet target = TestRuleSetHelper.CreateTestRuleSet(@"c:\Solution\SomeFolder\fullFilePath.ruleset");
            RuleSet sourceWithRelativeInclude = TestRuleSetHelper.CreateTestRuleSet(@"c:\fullFilePath.ruleset");
            string relativeInclude = @"Solution\SomeFolder\fullFilePath.ruleset".ToLowerInvariant(); // Catch casing errors
            sourceWithRelativeInclude.RuleSetIncludes.Add(new RuleSetInclude(relativeInclude, RuleAction.Error));
            RuleSet sourceWithAbsoluteInclude = TestRuleSetHelper.CreateTestRuleSet(@"c:\fullFilePath.ruleset");
            string absoluteInclude = target.FilePath.ToUpperInvariant(); // Catch casing errors
            sourceWithAbsoluteInclude.RuleSetIncludes.Add(new RuleSetInclude(absoluteInclude, RuleAction.Warning));
            RuleSetInclude include;

            // Case 1: Relative include
            // Act
            include = RuleSetHelper.FindInclude(sourceWithRelativeInclude, target);

            // Assert
            StringComparer.OrdinalIgnoreCase.Equals(include.FilePath, relativeInclude).Should().BeTrue($"Unexpected include {include.FilePath} instead of {relativeInclude}");

            // Case 2: Absolute include
            // Act
            include = RuleSetHelper.FindInclude(sourceWithAbsoluteInclude, target);

            // Assert
            StringComparer.OrdinalIgnoreCase.Equals(include.FilePath, absoluteInclude).Should().BeTrue($"Unexpected include {include.FilePath} instead of {absoluteInclude}");

            // Case 3: No includes at all
            // Act
            include = RuleSetHelper.FindInclude(target, target);
            // Assert
            include.Should().BeNull("No includes at all");

            // Case 4: No includes from source to target
            // Act
            include = RuleSetHelper.FindInclude(sourceWithRelativeInclude, sourceWithAbsoluteInclude);
            // Assert
            include.Should().BeNull("No includes from source to target");
        }

        #endregion Tests

        #region Helpers

        private static RuleSetInclude AddRuleSetInclusion(RuleSet parent, RuleSet child, bool useRelativePath)
        {
            string include = useRelativePath
                ? PathHelper.CalculateRelativePath(parent.FilePath, child.FilePath)
                : child.FilePath;
            var ruleSetInclude = new RuleSetInclude(include, RuleAction.Default);
            parent.RuleSetIncludes.Add(ruleSetInclude);
            return ruleSetInclude;
        }

        #endregion Helpers
    }
}
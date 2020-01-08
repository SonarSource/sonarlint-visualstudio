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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class RulesetIncludeCheckerTests
    {
        [TestMethod]
        public void HasInclude_ArgChecks()
        {
            var rs = new RuleSet("Name", @"c:\path.ruleset");

            Exceptions.Expect<ArgumentNullException>(() => RuleSetIncludeChecker.HasInclude(null, rs));
            Exceptions.Expect<ArgumentNullException>(() => RuleSetIncludeChecker.HasInclude(rs, null));
        }

        [TestMethod]
        public void HasInclude_RelativePaths()
        {
            // Arrange
            var target = TestRuleSetHelper.CreateTestRuleSet(@"c:\aaa\Solution\SomeFolder\fullFilePath.ruleset");

            var relativeInclude = @"Solution\SomeFolder\fullFilePath.ruleset".ToLowerInvariant(); // Catch casing errors
            var sourceWithRelativeInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(@"c:\aaa\fullFilePath.ruleset",
                relativeInclude, "otherInclude.ruleset");

            // Alternative directory separator, different relative path format
            var relativeInclude2 = @"./Solution/SomeFolder/fullFilePath.ruleset";
            var sourceWithRelativeInclude2 = TestRuleSetHelper.CreateTestRuleSetWithIncludes(@"c:\aaa\fullFilePath.ruleset",
                "c://XXX/Solution/SomeFolder/another.ruleset", relativeInclude2);

            // Case 1: Relative include
            // Act
            var hasInclude = RuleSetIncludeChecker.HasInclude(sourceWithRelativeInclude, target);

            // Assert
            hasInclude.Should().BeTrue();

            // Case 2: Relative include, alternative path separators
            // Act
            hasInclude = RuleSetIncludeChecker.HasInclude(sourceWithRelativeInclude2, target);

            // Assert
            hasInclude.Should().BeTrue();
        }

        [TestMethod]
        public void HasInclude_RelativePaths_Complex()
        {
            // Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/658
            // "SonarLint for Visual Studio 2017 plugin does not respect shared imports "

            // Arrange
            var target = TestRuleSetHelper.CreateTestRuleSet(@"c:\Solution\SomeFolder\fullFilePath.ruleset");

            var relativeInclude = @".\..\..\Solution\SomeFolder\fullFilePath.ruleset";
            var sourceWithRelativeInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(@"c:\aaa\bbb\fullFilePath.ruleset",
                relativeInclude);

            // Act
            var hasInclude = RuleSetIncludeChecker.HasInclude(sourceWithRelativeInclude, target);

            // Assert
            hasInclude.Should().BeTrue();
        }

        [TestMethod]
        public void HasInclude_RelativePaths_Complex2()
        {
            // Arrange
            var target = TestRuleSetHelper.CreateTestRuleSet(@"c:\Solution\SomeFolder\fullFilePath.ruleset");

            var relativeInclude = @"./.\..\..\Dummy1\Dummy2\..\.././Solution\SomeFolder\fullFilePath.ruleset";
            var sourceWithRelativeInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(@"c:\aaa\bbb\fullFilePath.ruleset",
                relativeInclude);

            // Act
            var hasInclude = RuleSetIncludeChecker.HasInclude(sourceWithRelativeInclude, target);

            // Assert
            hasInclude.Should().BeTrue();
        }

        [TestMethod]
        public void HasInclude_AbsolutePaths()
        {
            // Arrange
            var target = TestRuleSetHelper.CreateTestRuleSet(@"c:\Solution\SomeFolder\fullFilePath.ruleset");

            var absoluteInclude = target.FilePath.ToUpperInvariant(); // Catch casing errors
            var sourceWithAbsoluteInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(@"c:\fullFilePath.ruleset",
                ".\\include1.ruleset", absoluteInclude, "c:\\dummy\\include2.ruleset");

            // Act
            var hasInclude = RuleSetIncludeChecker.HasInclude(sourceWithAbsoluteInclude, target);

            // Assert
            hasInclude.Should().BeTrue();
        }

        [TestMethod]
        public void HasInclude_NoIncludes()
        {
            // Arrange
            var ruleSet = TestRuleSetHelper.CreateTestRuleSet(@"c:\Solution\SomeFolder\fullFilePath.ruleset");
            var unreferencedRuleset = TestRuleSetHelper.CreateTestRuleSet(@"c:\unreferenced.ruleset");

            // Act No includes at all
            var include = RuleSetIncludeChecker.HasInclude(ruleSet, unreferencedRuleset);

            // Assert
            include.Should().BeFalse();
        }

        [TestMethod]
        public void HasInclude_NoIncludesFromSourceToTarget()
        {
            // Arrange
            var target = TestRuleSetHelper.CreateTestRuleSet(@"c:\Solution\SomeFolder\fullFilePath.ruleset");

            var sourceWithInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(@"c:\fullFilePath.ruleset",
                "include1", "c:\\foo\\include2", "fullFilePath.ruleset");
            
            // Act - No includes from source to target
            var include = RuleSetIncludeChecker.HasInclude(sourceWithInclude, target);

            // Assert
            include.Should().BeFalse();
        }

        [TestMethod]
        public void HasInclude_SourceIsTarget_ReturnsTrue()
        {
            // Covers the case where the ruleset is included directly in the project, rather
            // than indirectly as a RuleSetInclude in another ruleset.

            // Arrange
            var source = TestRuleSetHelper.CreateTestRuleSet(@"c:\Solution\SomeFolder\fullFilePath.ruleset");
            var target = TestRuleSetHelper.CreateTestRuleSet(@"C:/SOLUTION\./SomeFolder\fullFilePath.ruleset");

            // Act
            var include = RuleSetIncludeChecker.HasInclude(source, target);

            // Assert
            include.Should().BeTrue();
        }
    }
}

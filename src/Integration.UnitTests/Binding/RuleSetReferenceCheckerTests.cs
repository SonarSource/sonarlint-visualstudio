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
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class RuleSetReferenceCheckerTests
    {
        private Mock<ISolutionRuleSetsInformationProvider> solutionRuleSetsInformationProviderMock;
        private Mock<IRuleSetSerializer> ruleSetSerializerMock;
        private RuleSetReferenceChecker testSubject;

        private string solutionRuleSetFilePath;
        private RuleSet projectRuleSetThatIncludesSolutionRuleSet;
        private RuleSet projectRuleSetThatDoesNotIncludeSolutionRuleSet;

        [TestInitialize]
        public void TestInitialize()
        {
            solutionRuleSetsInformationProviderMock = new Mock<ISolutionRuleSetsInformationProvider>();
            ruleSetSerializerMock = new Mock<IRuleSetSerializer>();

            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(ISolutionRuleSetsInformationProvider)))
                .Returns(solutionRuleSetsInformationProviderMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IRuleSetSerializer)))
                .Returns(ruleSetSerializerMock.Object);

            testSubject = new RuleSetReferenceChecker(serviceProviderMock.Object);

            solutionRuleSetFilePath = @"c:\aaa\Solution\SomeFolder\fullFilePath.ruleset";
            projectRuleSetThatDoesNotIncludeSolutionRuleSet = TestRuleSetHelper.CreateTestRuleSet(@"c:\foo\dummy.ruleset"); ;

            var relativeInclude = @"Solution\SomeFolder\fullFilePath.ruleset".ToLowerInvariant(); // Catch casing errors
            projectRuleSetThatIncludesSolutionRuleSet = TestRuleSetHelper.CreateTestRuleSetWithIncludes(@"c:\aaa\fullFilePath.ruleset", relativeInclude, "otherInclude.ruleset");
        }

        [TestMethod]
        public void IsReferenced_ProjectHasNoRuleSets_False()
        {
            var projectMock = CreateMockProject(0, out _);

            var result = testSubject.IsReferenced(projectMock, solutionRuleSetFilePath);
            result.Should().BeFalse();

            ruleSetSerializerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsReferenced_ProjectHasOneRuleSet_CantLoadProjectRuleSetFile_False()
        {
            var projectMock = CreateMockProject(1, out var declarations);

            var filePath = "";
            solutionRuleSetsInformationProviderMock
                .Setup(x => x.TryGetProjectRuleSetFilePath(projectMock, declarations.First(), out filePath))
                .Returns(false);

            var result = testSubject.IsReferenced(projectMock, solutionRuleSetFilePath);
            result.Should().BeFalse();

            ruleSetSerializerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsReferenced_ProjectHasOneRuleSet_ReturnIfReferencesSolutionRuleSet(bool referencesSolutionRuleSet)
        {
            var projectMock = CreateMockProject(1, out var declarations);

            var projectRuleSet = referencesSolutionRuleSet
                ? projectRuleSetThatIncludesSolutionRuleSet
                : projectRuleSetThatDoesNotIncludeSolutionRuleSet;

            SetupProjectRuleSet(projectMock, declarations.First(), projectRuleSet);

            var result = testSubject.IsReferenced(projectMock, solutionRuleSetFilePath);

            result.Should().Be(referencesSolutionRuleSet);
        }

        [TestMethod]
        [DataRow(true, true, true)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(false, false, false)]
        public void IsReferenced_ProjectHasTwoRuleSets_ReturnIfAllReferenceSolutionRuleSet(bool firstReferencesSolutionRuleSet, bool secondReferencesSolutionRuleSet, bool expectedResult)
        {
            var projectMock = CreateMockProject(2, out var declarations);

            var firstProjectRuleSet = firstReferencesSolutionRuleSet
                ? projectRuleSetThatIncludesSolutionRuleSet
                : projectRuleSetThatDoesNotIncludeSolutionRuleSet;

            SetupProjectRuleSet(projectMock, declarations.First(), firstProjectRuleSet);

            var secondProjectRuleSet = secondReferencesSolutionRuleSet
                ? projectRuleSetThatIncludesSolutionRuleSet
                : projectRuleSetThatDoesNotIncludeSolutionRuleSet;

            SetupProjectRuleSet(projectMock, declarations.Last(), secondProjectRuleSet);

            var result = testSubject.IsReferenced(projectMock, solutionRuleSetFilePath);

            result.Should().Be(expectedResult);
        }

        [TestMethod]
        public void IsReferenced_RelativePaths()
        {
            // Arrange
            var relativeInclude = @"Solution\SomeFolder\fullFilePath.ruleset".ToLowerInvariant(); // Catch casing errors
            var sourceWithRelativeInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(
                @"c:\aaa\fullFilePath.ruleset",
                relativeInclude, "otherInclude.ruleset");

            // Alternative directory separator, different relative path format
            var relativeInclude2 = @"./Solution/SomeFolder/fullFilePath.ruleset";
            var sourceWithRelativeInclude2 = TestRuleSetHelper.CreateTestRuleSetWithIncludes(
                @"c:\aaa\fullFilePath.ruleset",
                "c://XXX/Solution/SomeFolder/another.ruleset", relativeInclude2);

            // Case 1: Relative include
            // Act
            var project = CreateMockProjectWithRuleSet(sourceWithRelativeInclude);
            var isReferenced = testSubject.IsReferenced(project, solutionRuleSetFilePath);

            // Assert
            isReferenced.Should().BeTrue();

            // Case 2: Relative include, alternative path separators
            // Act
            project = CreateMockProjectWithRuleSet(sourceWithRelativeInclude2);
            isReferenced = testSubject.IsReferenced(project, solutionRuleSetFilePath);

            // Assert
            isReferenced.Should().BeTrue();
        }

        [TestMethod]
        public void IsReferenced_RelativePaths_Complex()
        {
            // Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/658
            // "SonarLint for Visual Studio 2017 plugin does not respect shared imports "

            // Arrange
            var targetRuleSetFilePath = @"c:\Solution\SomeFolder\fullFilePath.ruleset";

            var relativeInclude = @".\..\..\Solution\SomeFolder\fullFilePath.ruleset";
            var sourceWithRelativeInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(
                @"c:\aaa\bbb\fullFilePath.ruleset",
                relativeInclude);

            // Act
            var project = CreateMockProjectWithRuleSet(sourceWithRelativeInclude);
            var isReferenced = testSubject.IsReferenced(project, targetRuleSetFilePath);

            // Assert
            isReferenced.Should().BeTrue();
        }

        [TestMethod]
        public void IsReferenced_RelativePaths_Complex2()
        {
            // Arrange
            var targetRuleSetFilePath = @"c:\Solution\SomeFolder\fullFilePath.ruleset";

            var relativeInclude = @"./.\..\..\Dummy1\Dummy2\..\.././Solution\SomeFolder\fullFilePath.ruleset";
            var sourceWithRelativeInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(
                @"c:\aaa\bbb\fullFilePath.ruleset",
                relativeInclude);

            // Act
            var project = CreateMockProjectWithRuleSet(sourceWithRelativeInclude);
            var isReferenced = testSubject.IsReferenced(project, targetRuleSetFilePath);

            // Assert
            isReferenced.Should().BeTrue();
        }

        [TestMethod]
        public void IsReferenced_AbsolutePaths()
        {
            // Arrange
            var absoluteInclude = solutionRuleSetFilePath.ToUpperInvariant(); // Catch casing errors
            var sourceWithAbsoluteInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(@"c:\fullFilePath.ruleset",
                ".\\include1.ruleset", absoluteInclude, "c:\\dummy\\include2.ruleset");

            // Act
            var project = CreateMockProjectWithRuleSet(sourceWithAbsoluteInclude);
            var isReferenced = testSubject.IsReferenced(project, solutionRuleSetFilePath);

            // Assert
            isReferenced.Should().BeTrue();
        }

        [TestMethod]
        public void IsReferenced_NoIncludes()
        {
            // Arrange
            var unreferencedRuleSet = TestRuleSetHelper.CreateTestRuleSet(@"c:\unreferenced.ruleset");

            // Act No includes at all
            var project = CreateMockProjectWithRuleSet(unreferencedRuleSet);
            var isReferenced = testSubject.IsReferenced(project, solutionRuleSetFilePath);

            // Assert
            isReferenced.Should().BeFalse();
        }

        [TestMethod]
        public void IsReferenced_NoIncludesFromSourceToTarget()
        {
            // Arrange
            var sourceWithInclude = TestRuleSetHelper.CreateTestRuleSetWithIncludes(@"c:\fullFilePath.ruleset",
                "include1", "c:\\foo\\include2", "fullFilePath.ruleset");

            // Act - No includes from source to target
            var project = CreateMockProjectWithRuleSet(sourceWithInclude);
            var isReferenced = testSubject.IsReferenced(project, solutionRuleSetFilePath);

            // Assert
            isReferenced.Should().BeFalse();
        }

        [TestMethod]
        public void IsReferenced_SourceIsTarget_ReturnsTrue()
        {
            // Covers the case where the ruleset is included directly in the project, rather
            // than indirectly as a RuleSetInclude in another ruleset.

            // Arrange
            var targetRuleSetFilePath = @"c:\Solution\SomeFolder\fullFilePath.ruleset";
            var projectRuleSet = TestRuleSetHelper.CreateTestRuleSet(@"C:/SOLUTION\./SomeFolder\fullFilePath.ruleset");

            // Act
            var project = CreateMockProjectWithRuleSet(projectRuleSet);
            var isReferenced = testSubject.IsReferenced(project, targetRuleSetFilePath);

            // Assert
            isReferenced.Should().BeTrue();
        }

        private RuleSetDeclaration CreateRuleSetDeclaration(Project projectMock) =>
            new RuleSetDeclaration(projectMock, new PropertyMock("name", null), Guid.NewGuid().ToString(), null);

        private ProjectMock CreateMockProject(int numberOfRuleSetDeclarations, out List<RuleSetDeclaration> declarations)
        {
            var projectMock = new ProjectMock("c:\\test.csproj");
            declarations = new List<RuleSetDeclaration>();

            for (var i = 0; i < numberOfRuleSetDeclarations; i++)
            {
                declarations.Add(CreateRuleSetDeclaration(projectMock));
            }

            solutionRuleSetsInformationProviderMock
                .Setup(x => x.GetProjectRuleSetsDeclarations(projectMock))
                .Returns(declarations);

            return projectMock;
        }

        private void SetupProjectRuleSet(Project project, RuleSetDeclaration declaration, RuleSet projectRuleSet)
        {
            var projectRuleSetPath = Guid.NewGuid().ToString();
            solutionRuleSetsInformationProviderMock
                .Setup(x => x.TryGetProjectRuleSetFilePath(project, declaration, out projectRuleSetPath))
                .Returns(true);

            ruleSetSerializerMock
                .Setup(x => x.LoadRuleSet(projectRuleSetPath))
                .Returns(projectRuleSet);
        }

        private Project CreateMockProjectWithRuleSet(RuleSet projectRuleSet)
        {
            var projectMock = CreateMockProject(1, out var declarations);

            SetupProjectRuleSet(projectMock, declarations.First(), projectRuleSet);

            return projectMock;
        }
    }
}

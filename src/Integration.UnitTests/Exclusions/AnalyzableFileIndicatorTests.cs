/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Exclusions;
using SonarQube.Client.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Exclusions
{
    [TestClass]
    public class AnalyzableFileIndicatorTests
    {
        private const string TestedFilePath = "some path";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<AnalyzableFileIndicator, IAnalyzableFileIndicator>(
                MefTestHelpers.CreateExport<IExclusionSettingsStorage>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void ShouldAnalyze_NoExclusionSettings_True()
        {
            var patternMatcher = new Mock<IGlobPatternMatcher>();

            var testSubject = CreateTestSubject(null, patternMatcher.Object);

            var result = testSubject.ShouldAnalyze(TestedFilePath);

            result.Should().Be(true);
            patternMatcher.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(true, true, false)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(false, false, true)]
        public void ShouldAnalyze_NoInclusions_HasExclusions_ReturnsIfExcluded(
            bool projectExclusionsApply,
            bool globalExclusionsApply,
            bool expectedResult)
        {
            var projectExclusions = new[] { "**/exclusion1", "**/exclusion2" };
            var globalExclusions = new[] { "**/exclusion3", "**/exclusion4" };
            var exclusionConfig = CreateServerExclusions(
                inclusions: null,
                exclusions: projectExclusions,
                globalExclusions: globalExclusions);

            var patternMatcher = new Mock<IGlobPatternMatcher>();
            patternMatcher.Setup(x => x.IsMatch(projectExclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(projectExclusions[1], TestedFilePath)).Returns(projectExclusionsApply);
            patternMatcher.Setup(x => x.IsMatch(globalExclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(globalExclusions[1], TestedFilePath)).Returns(globalExclusionsApply);

            var testSubject = CreateTestSubject(exclusionConfig, patternMatcher.Object);

            var result = testSubject.ShouldAnalyze(TestedFilePath);

            result.Should().Be(expectedResult);
        }

        [TestMethod]
        [DataRow(true, true)]
        [DataRow(false, false)]
        public void ShouldAnalyze_HasInclusions_NoExclusions_ReturnsIfIncluded(bool inclusionsApply, bool expectedResult)
        {
            var inclusions = new[] { "**/inclusion1", "**/inclusion2" };
            var exclusionConfig = CreateServerExclusions(
                inclusions: inclusions, 
                exclusions: null,
                globalExclusions: null);

            var patternMatcher = new Mock<IGlobPatternMatcher>();
            patternMatcher.Setup(x => x.IsMatch(inclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(inclusions[1], TestedFilePath)).Returns(inclusionsApply);

            var testSubject = CreateTestSubject(exclusionConfig, patternMatcher.Object);

            var result = testSubject.ShouldAnalyze(TestedFilePath);

            result.Should().Be(expectedResult);
            patternMatcher.VerifyAll();
            patternMatcher.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true, true, false)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(false, false, true)]
        public void ShouldAnalyze_FileIncluded_ReturnsIfExcluded(
            bool projectExclusionsApply, 
            bool globalExclusionsApply,
            bool expectedResult)
        {
            var inclusions = new[] { "**/inclusion1", "**/inclusion2" };
            var projectExclusions = new[] { "**/exclusion1", "**/exclusion2" };
            var globalExclusions = new[] { "**/exclusion3", "**/exclusion4" };
            var exclusionConfig = CreateServerExclusions(
                inclusions: inclusions,
                exclusions: projectExclusions,
                globalExclusions: globalExclusions);

            var patternMatcher = new Mock<IGlobPatternMatcher>();
            patternMatcher.Setup(x => x.IsMatch(inclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(inclusions[1], TestedFilePath)).Returns(true);
            patternMatcher.Setup(x => x.IsMatch(projectExclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(projectExclusions[1], TestedFilePath)).Returns(projectExclusionsApply);
            patternMatcher.Setup(x => x.IsMatch(globalExclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(globalExclusions[1], TestedFilePath)).Returns(globalExclusionsApply);

            var testSubject = CreateTestSubject(exclusionConfig, patternMatcher.Object);

            var result = testSubject.ShouldAnalyze(TestedFilePath);

            result.Should().Be(expectedResult);
        }

        [TestMethod, Description("Regression test for #3075")]
        public void ShouldAnalyze_HasWindowsPathWithBackSlash_ReplacesWithForwardSlash()
        {
            var filePath = "C:\\FooBar\\foo.bar";

            var projectExclusions = new[] { "**/exclusion" };
            var exclusionConfig = CreateServerExclusions(
                inclusions: null,
                exclusions: projectExclusions,
                globalExclusions: null);

            var patternMatcher = new Mock<IGlobPatternMatcher>();

            var testSubject = CreateTestSubject(exclusionConfig, patternMatcher.Object);

            _ = testSubject.ShouldAnalyze(filePath);

            patternMatcher.Verify(x => x.IsMatch(projectExclusions[0], "C:\\FooBar\\foo.bar"), Times.Never);
            patternMatcher.Verify(x => x.IsMatch(projectExclusions[0], "C:/FooBar/foo.bar"), Times.Once);

        }

        [TestMethod]
        public void Perf_ShouldAnalyze_HasInclusions_FileNotIncluded_ExclusionsAreNotChecked()
        {
            var inclusions = new[] { "**/inclusion1" };
            var exclusions = new[] { "**/exclusion1" };
            var exclusionConfig = CreateServerExclusions(
                inclusions: inclusions, 
                exclusions: exclusions, 
                globalExclusions: exclusions);

            var patternMatcher = new Mock<IGlobPatternMatcher>();
            patternMatcher.Setup(x => x.IsMatch(inclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(exclusions[0], TestedFilePath)).Returns(false);

            var testSubject = CreateTestSubject(exclusionConfig, patternMatcher.Object);

            var result = testSubject.ShouldAnalyze(TestedFilePath);

            result.Should().Be(false);
            patternMatcher.Verify(x=> x.IsMatch(inclusions[0], TestedFilePath), Times.Once);
            patternMatcher.Verify(x=> x.IsMatch(exclusions[0], TestedFilePath), Times.Never);
            patternMatcher.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Perf_ShouldAnalyze_FileExcludedInProjectSettings_GlobalExclusionsAreNotChecked()
        {
            var projectExclusions = new[] { "**/exclusion1" };
            var globalExclusions = new[] { "**/exclusion2" };
            var exclusionConfig = CreateServerExclusions(
                inclusions: null,
                exclusions: projectExclusions,
                globalExclusions: globalExclusions);

            var patternMatcher = new Mock<IGlobPatternMatcher>();
            patternMatcher.Setup(x => x.IsMatch(projectExclusions[0], TestedFilePath)).Returns(true);
            patternMatcher.Setup(x => x.IsMatch(globalExclusions[0], TestedFilePath)).Returns(true);

            var testSubject = CreateTestSubject(exclusionConfig, patternMatcher.Object);

            var result = testSubject.ShouldAnalyze(TestedFilePath);

            result.Should().Be(false);
            patternMatcher.Verify(x => x.IsMatch(projectExclusions[0], TestedFilePath), Times.Once);
            patternMatcher.Verify(x => x.IsMatch(globalExclusions[0], TestedFilePath), Times.Never);
            patternMatcher.VerifyNoOtherCalls();
        }

        private AnalyzableFileIndicator CreateTestSubject(ServerExclusions exclusions, IGlobPatternMatcher patternMatcher)
        {
            var exclusionsSettingsStorage = new Mock<IExclusionSettingsStorage>();
            exclusionsSettingsStorage.Setup(x => x.GetSettings()).Returns(exclusions);

            return new AnalyzableFileIndicator(exclusionsSettingsStorage.Object, patternMatcher, Mock.Of<ILogger>());
        }

        private ServerExclusions CreateServerExclusions(
            string[] inclusions, 
            string[] exclusions,
            string[] globalExclusions)
        {
            return new ServerExclusions(exclusions: exclusions,
                globalExclusions: globalExclusions,
                inclusions: inclusions);
        }
    }
}

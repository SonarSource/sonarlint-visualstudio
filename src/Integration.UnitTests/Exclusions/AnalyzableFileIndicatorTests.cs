/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.UnitTests.Exclusions
{
    [TestClass]
    public class AnalyzableFileIndicatorTests
    {
        private const string TestedFilePath = "some path";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<AnalyzableFileIndicator, IAnalyzableFileIndicator>(null, new []
            {
                MefTestHelpers.CreateExport<IExclusionSettingsFileStorage>(Mock.Of<IExclusionSettingsFileStorage>()),
            });
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
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void ShouldAnalyze_NoInclusions_HasExclusions_ReturnsIfExcluded(bool exclusionsApply, bool expectedResult)
        {
            var exclusions = new[] { "exclusion1", "exclusion2" };
            var exclusionConfig = CreateServerExclusions(inclusions: null, exclusions: exclusions);

            var patternMatcher = new Mock<IGlobPatternMatcher>();
            patternMatcher.Setup(x => x.IsMatch(exclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(exclusions[1], TestedFilePath)).Returns(exclusionsApply);

            var testSubject = CreateTestSubject(exclusionConfig, patternMatcher.Object);

            var result = testSubject.ShouldAnalyze(TestedFilePath);

            result.Should().Be(expectedResult);
            patternMatcher.VerifyAll();
            patternMatcher.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true, true)]
        [DataRow(false, false)]
        public void ShouldAnalyze_HasInclusions_NoExclusions_ReturnsIfIncluded(bool inclusionsApply, bool expectedResult)
        {
            var inclusions = new[] { "inclusion1", "inclusion2" };
            var exclusionConfig = CreateServerExclusions(inclusions: inclusions, exclusions: null);

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
        [DataRow(true, false)]
        [DataRow(false, true)]

        public void ShouldAnalyze_FileIncluded_ReturnsIfExcluded(bool exclusionsApply, bool expectedResult)
        {
            var inclusions = new[] { "inclusion1", "inclusion2" };
            var exclusions = new[] { "exclusion1", "exclusion2" };
            var exclusionConfig = CreateServerExclusions(inclusions: inclusions, exclusions: exclusions);

            var patternMatcher = new Mock<IGlobPatternMatcher>();
            patternMatcher.Setup(x => x.IsMatch(inclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(inclusions[1], TestedFilePath)).Returns(true);
            patternMatcher.Setup(x => x.IsMatch(exclusions[0], TestedFilePath)).Returns(false);
            patternMatcher.Setup(x => x.IsMatch(exclusions[1], TestedFilePath)).Returns(exclusionsApply);

            var testSubject = CreateTestSubject(exclusionConfig, patternMatcher.Object);

            var result = testSubject.ShouldAnalyze(TestedFilePath);

            result.Should().Be(expectedResult);
            patternMatcher.VerifyAll();
            patternMatcher.VerifyNoOtherCalls();
        }

        [TestMethod]

        public void ShouldAnalyze_HasInclusions_FileNotIncluded_ExclusionsAreNotChecked()
        {
            var inclusions = new[] { "inclusion1" };
            var exclusions = new[] { "exclusion1" };
            var exclusionConfig = CreateServerExclusions(inclusions: inclusions, exclusions: exclusions);

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

        private AnalyzableFileIndicator CreateTestSubject(ServerExclusions exclusions, IGlobPatternMatcher patternMatcher)
        {
            var exclusionsFileStorage = new Mock<IExclusionSettingsFileStorage>();
            exclusionsFileStorage.Setup(x => x.GetSettings()).Returns(exclusions);

            return new AnalyzableFileIndicator(exclusionsFileStorage.Object, patternMatcher);
        }

        private ServerExclusions CreateServerExclusions(string[] inclusions, string[] exclusions)
        {
            return new ServerExclusions(exclusions: exclusions,
                globalExclusions: null,
                inclusions: inclusions,
                globalInclusions: null);
        }
    }
}

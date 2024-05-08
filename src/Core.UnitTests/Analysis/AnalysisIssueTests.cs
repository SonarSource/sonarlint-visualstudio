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

using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Core.UnitTests.Analysis
{
    [TestClass]
    public class AnalysisIssueTests
    {
        [TestMethod]
        public void IsFileLevel_PrimaryLocationHasTextRange_ReturnsFalse()
        {
            var analysisIssue = CreateTestSubject(true);

            analysisIssue.IsFileLevel().Should().BeFalse();
        }

        [TestMethod]
        public void IsFileLevel_PrimaryLocationHasNoTextRange_ReturnsTrue()
        {
            var analysisIssue = CreateTestSubject(false);

            analysisIssue.IsFileLevel().Should().BeTrue();
        }

        private IAnalysisIssueBase CreateTestSubject(bool primaryLocationHasTextRange)
        {
            var analysisIssue = new Mock<IAnalysisIssueBase>();
            var primaryLocation = new Mock<IAnalysisIssueLocation>();

            if(primaryLocationHasTextRange)
            {
                primaryLocation.SetupGet(p => p.TextRange).Returns(new DummyTextRange());
            }
            analysisIssue.SetupGet(a => a.PrimaryLocation).Returns(primaryLocation.Object);
            return analysisIssue.Object;
        }
    }
}

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
using SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging
{
    [TestClass]
    public class SonarErrorTagUnitTests
    {
        [TestMethod]
        public void Ctor_SimplePropertySet_TooltipNotCreated()
        {
            var tooltipProvider = new Mock<IErrorTagTooltipProvider>();

            var testSubject = CreateTestSubject("my error type", tooltipProvider.Object);

            testSubject.ErrorType.Should().Be("my error type");
            tooltipProvider.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public void ToolTipContent_CreatedOnDemandAndCreatedOnlyOnce()
        {
            var analysisIssue = Mock.Of<IAnalysisIssueBase>();

            var expectedTooltipObject = new object();
            var tooltipProvider = new Mock<IErrorTagTooltipProvider>();
            tooltipProvider.Setup(x => x.Create(analysisIssue)).Returns(expectedTooltipObject);

            var testSubject = CreateTestSubject(tooltipProvider: tooltipProvider.Object,
                analysisIssue: analysisIssue);

            // Sanity check before accessing the property
            tooltipProvider.Invocations.Should().HaveCount(0);

            // 1. First access - should create the tooltip
            var actual1 = testSubject.ToolTipContent;

            actual1.Should().BeSameAs(expectedTooltipObject);
            tooltipProvider.Invocations.Should().HaveCount(1);

            // 2. Subsequent access - should return the original object
            var actual2 = testSubject.ToolTipContent;

            actual2.Should().BeSameAs(expectedTooltipObject);
            tooltipProvider.Invocations.Should().HaveCount(1);
        }

        private static SonarErrorTag CreateTestSubject(string errorType = null,
            IErrorTagTooltipProvider tooltipProvider = null,
            IAnalysisIssueBase analysisIssue = null)
        {
            errorType ??= "any";
            analysisIssue ??= Mock.Of<IAnalysisIssueBase>();
            tooltipProvider ??= Mock.Of<IErrorTagTooltipProvider>();

            return new SonarErrorTag(errorType, analysisIssue, tooltipProvider);
        }
    }
}

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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests
{
    [TestClass]
    public class AnalysisSeverityToPriorityConverterTests
    {
        [TestMethod]
        [DataRow(AnalysisIssueSeverity.Blocker, "high")]
        [DataRow(AnalysisIssueSeverity.Major, "medium")]
        [DataRow(AnalysisIssueSeverity.Minor, "low")]
        public void Convert_SeverityInRange_ReturnsCorrectPriority(AnalysisIssueSeverity severity, string expectedPriority)
        {
            var testSubject = new AnalysisSeverityToPriorityConverter();

            var result = testSubject.Convert(severity);
            result.Should().Be(expectedPriority);
        }

        [TestMethod]
        [DataRow(AnalysisIssueSeverity.Critical)]
        [DataRow(AnalysisIssueSeverity.Info)]

        public void Convert_SeverityNotInRange_ArgumentOutOfRangeException(AnalysisIssueSeverity severity)
        {
            var testSubject = new AnalysisSeverityToPriorityConverter();

            Action act = () => testSubject.Convert(severity);
            act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("severity");
        }

        [TestMethod]
        public void Convert_NullPriority_ArgumentNullException()
        {
            var testSubject = new AnalysisSeverityToPriorityConverter();
            
            Action act = () => testSubject.Convert(null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("priority");
        }

        [TestMethod]
        [DataRow("high", AnalysisIssueSeverity.Blocker)]
        [DataRow("medium", AnalysisIssueSeverity.Major)]
        [DataRow("low", AnalysisIssueSeverity.Minor)]
        public void Convert_PriorityInRange_ReturnsCorrectSeverity(string priority, AnalysisIssueSeverity expectedSeverity)
        {
            var testSubject = new AnalysisSeverityToPriorityConverter();

            var result = testSubject.Convert(priority);
            result.Should().Be(expectedSeverity);
        }

        [TestMethod]
        [DataRow("high")]
        [DataRow("High")]
        [DataRow("HIGH")]
        public void Convert_PriorityCaseSensitivity_PriorityIsNotCaseSensitive(string priority)
        {
            const AnalysisIssueSeverity expectedSeverity = AnalysisIssueSeverity.Blocker;

            var testSubject = new AnalysisSeverityToPriorityConverter();

            var result = testSubject.Convert(priority);
            result.Should().Be(expectedSeverity);
        }

        [TestMethod]
        public void Convert_PriorityNotInRange_ArgumentOutOfRangeException()
        {
            var testSubject = new AnalysisSeverityToPriorityConverter();

            Action act = () => testSubject.Convert("unknown priority");
            act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("priority");
        }
    }
}

/*
 * SonarQube Client
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
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.IssueVisualization.TableControls;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.TableControls
{
    [TestClass]
    public class IssueTablesSelectionMonitorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var selectionService = Mock.Of<IAnalysisIssueSelectionService>();
            var selectionServiceExport = MefTestHelpers.CreateExport<IAnalysisIssueSelectionService>(selectionService);

            var converter = Mock.Of<IAnalysisIssueVisualizationConverter>();
            var converterExport = MefTestHelpers.CreateExport<IAnalysisIssueVisualizationConverter>(converter);

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<IssueTablesSelectionMonitor, IIssueTablesSelectionMonitor>(null, new[] { selectionServiceExport, converterExport });
        }

        [TestMethod]
        public void SelectionChanged_NullIssue_SelectedIssueIsSetToNull()
        {
            var mockSelectionService = new Mock<IAnalysisIssueSelectionService>();
            var mockConverter = new Mock<IAnalysisIssueVisualizationConverter>();

            IIssueTablesSelectionMonitor testSubject = new IssueTablesSelectionMonitor(mockSelectionService.Object, mockConverter.Object);
            testSubject.SelectionChanged(null);

            CheckExpectedValuePassedToService(mockSelectionService, expectedValue: null);
            mockConverter.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void SelectionChanged_ValidIssue_SelectedIssueIsSetCorrectly()
        {
            var mockSelectionService = new Mock<IAnalysisIssueSelectionService>();
            var mockConverter = new Mock<IAnalysisIssueVisualizationConverter>();

            var originalAnalysisIssue = Mock.Of<IAnalysisIssue>();
            var visIssue = Mock.Of<IAnalysisIssueVisualization>();
            mockConverter.Setup(x => x.Convert(originalAnalysisIssue)).Returns(visIssue);

            IIssueTablesSelectionMonitor testSubject = new IssueTablesSelectionMonitor(mockSelectionService.Object, mockConverter.Object);
            testSubject.SelectionChanged(originalAnalysisIssue);

            CheckExpectedValuePassedToService(mockSelectionService, expectedValue: visIssue);
            mockConverter.Invocations.Count.Should().Be(1);
        }

        [TestMethod]
        public void SelectionChanged_NullIssue_ExceptionsArePropagated()
        {
            var mockConverter = new Mock<IAnalysisIssueVisualizationConverter>();
            mockConverter.Setup(x => x.Convert(It.IsAny<IAnalysisIssue>())).Throws<ArgumentOutOfRangeException>();
            IIssueTablesSelectionMonitor testSubject = new IssueTablesSelectionMonitor(Mock.Of<IAnalysisIssueSelectionService>(), mockConverter.Object);

            Action act = () => testSubject.SelectionChanged(Mock.Of<IAnalysisIssue>());

            act.Should().ThrowExactly<ArgumentOutOfRangeException>();
        }

        private static void CheckExpectedValuePassedToService(Mock<IAnalysisIssueSelectionService> selectionService, IAnalysisIssueVisualization expectedValue)
        {
            selectionService.Verify(x => x.Select(expectedValue), Times.Once);
            selectionService.VerifyNoOtherCalls();
        }
    }
}

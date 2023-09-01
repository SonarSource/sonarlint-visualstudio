/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList.ViewModels;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.TaintList
{
    [TestClass]
    public class CommonIssueViewModelTests
    {
        [TestMethod]
        public void Ctor_RegisterToIssueVizPropertyChangedEvent()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.SetupAdd(x => x.PropertyChanged += null);

            CreateTestSubject(issueViz.Object);

            issueViz.VerifyAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>(), Times.Once);
        }
        
        [DataRow(SoftwareQualitySeverity.High)]
        [DataRow(SoftwareQualitySeverity.Medium)]
        [DataRow(SoftwareQualitySeverity.Low)]
        [DataTestMethod]
        public void DisplaySeverity_NewSeverity_DisplayedCorrectly(SoftwareQualitySeverity severity)
        {
            var taintIssue = new Mock<ITaintIssue>();
            taintIssue.Setup(x => x.HighestSoftwareQualitySeverity).Returns(severity);
            taintIssue.Setup(x => x.Severity).Returns(AnalysisIssueSeverity.Major);
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(taintIssue.Object);

            var testSubject = CreateTestSubject(issueViz.Object);

            testSubject.DisplaySeverity.Should().Be(severity.ToString());
            testSubject.DisplaySeveritySortOrder.Should().Be((int)severity);
        }

        [DataRow(AnalysisIssueSeverity.Blocker)]
        [DataRow(AnalysisIssueSeverity.Critical)]
        [DataRow(AnalysisIssueSeverity.Major)]
        [DataRow(AnalysisIssueSeverity.Minor)]
        [DataRow(AnalysisIssueSeverity.Info)]
        [DataTestMethod]
        public void DisplaySeverity_OldSeverity_DisplayedCorrectly(AnalysisIssueSeverity severity)
        {
            var taintIssue = new Mock<ITaintIssue>();
            taintIssue.Setup(x => x.Severity).Returns(severity);
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(taintIssue.Object);

            var testSubject = CreateTestSubject(issueViz.Object);

            testSubject.DisplaySeverity.Should().Be(severity.ToString());
            testSubject.DisplaySeveritySortOrder.Should().Be((int)severity);
        }
        
        [TestMethod]
        public void Dispose_UnregisterFromIssueVizPropertyChangedEvent()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.SetupRemove(x => x.PropertyChanged -= null);

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.Dispose();

            issueViz.VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Once);
        }

        [TestMethod]
        public void IssueVizPropertyChanged_UnknownProperty_NoPropertyChangedEvent()
        {
            var eventHandler = new Mock<PropertyChangedEventHandler>();
            var issueViz = new Mock<IAnalysisIssueVisualization>();

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            issueViz.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs("some dummy property"));

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IssueVizPropertyChanged_SpanProperty_RaisesPropertyChangedForLineAndColumn()
        {
            var eventHandler = new Mock<PropertyChangedEventHandler>();
            var issueViz = new Mock<IAnalysisIssueVisualization>();

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            issueViz.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IAnalysisIssueVisualization.Span)));

            eventHandler.Verify(x => x(testSubject,
                    It.Is((PropertyChangedEventArgs args) => args.PropertyName == nameof(ITaintIssueViewModel.Line))),
                Times.Once);

            eventHandler.Verify(x => x(testSubject,
                    It.Is((PropertyChangedEventArgs args) => args.PropertyName == nameof(ITaintIssueViewModel.Column))),
                Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Line_ReturnsDisplayPosition()
        {
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();

            var positionCalculatorMock = new Mock<IIssueVizDisplayPositionCalculator>();
            positionCalculatorMock.Setup(x => x.GetLine(issueViz)).Returns(123);

            // Act
            var testSubject = CreateTestSubject(issueViz, positionCalculator: positionCalculatorMock.Object);

            testSubject.Line.Should().Be(123);
            positionCalculatorMock.VerifyAll();
        }

        [TestMethod]
        public void Column_ReturnsDisplayPosition()
        {
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();

            var positionCalculatorMock = new Mock<IIssueVizDisplayPositionCalculator>();
            positionCalculatorMock.Setup(x => x.GetColumn(issueViz)).Returns(999);

            // Act
            var testSubject = CreateTestSubject(issueViz, positionCalculator: positionCalculatorMock.Object);

            testSubject.Column.Should().Be(999);
        }

        [TestMethod]
        public void DisplayPath_IssueVizHasNoFilePath_ReturnsServerPath()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.Setup(x => x.PrimaryLocation.FilePath).Returns("\\some\\server\\path.cs");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.CurrentFilePath).Returns((string)null);
            issueViz.Setup(x => x.Issue).Returns(issue.Object);

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.DisplayPath.Should().Be("path.cs");
        }

        [TestMethod]
        public void DisplayPath_IssueVizHasFilePath_ReturnsFilePath()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.CurrentFilePath).Returns("c:\\some\\local\\path.cs");

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.DisplayPath.Should().Be("path.cs");
        }

        [TestMethod]
        public void IssueVizPropertyChanged_CurrentFilePathProperty_RaisesPropertyChangedForDisplayPath()
        {
            var eventHandler = new Mock<PropertyChangedEventHandler>();
            var issueViz = new Mock<IAnalysisIssueVisualization>();

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            issueViz.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IAnalysisIssueVisualization.CurrentFilePath)));

            eventHandler.Verify(x => x(testSubject,
                    It.Is((PropertyChangedEventArgs args) => args.PropertyName == nameof(ITaintIssueViewModel.DisplayPath))),
                Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        private static TaintIssueViewModel CreateTestSubject(IAnalysisIssueVisualization issueViz, IIssueVizDisplayPositionCalculator positionCalculator = null)
        {
            positionCalculator ??= new IssueVizDisplayPositionCalculator();
            return new TaintIssueViewModel(issueViz, positionCalculator);
        }
    }
}

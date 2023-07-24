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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE_Hotspots.HotspotsList.ViewModels;
using SharedProvider = SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE_Hotspots.HotspotsList
{
    [TestClass]
    public class OpenInIDEHotspotViewModelTests
    {
        [TestMethod]
        public void Ctor_RegisterToHotspotPropertyChangedEvent()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.SetupAdd(x => x.PropertyChanged += null);

            CreateTestSubject(issueViz.Object);

            issueViz.VerifyAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromHotspotPropertyChangedEvent()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.SetupRemove(x => x.PropertyChanged -= null);

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.Dispose();

            issueViz.VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Once);
        }

        [TestMethod]
        public void HotspotPropertyChanged_UnknownProperty_NoPropertyChangedEvent()
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
        public void HotspotPropertyChanged_SpanProperty_RaisesPropertyChangedForLineAndColumn()
        {
            var eventHandler = new Mock<PropertyChangedEventHandler>();
            var issueViz = new Mock<IAnalysisIssueVisualization>();

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            issueViz.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IAnalysisIssueVisualization.Span)));

            eventHandler.Verify(x => x(testSubject,
                    It.Is((PropertyChangedEventArgs args) => args.PropertyName == nameof(IOpenInIDEHotspotViewModel.Line))),
                Times.Once);

            eventHandler.Verify(x => x(testSubject,
                    It.Is((PropertyChangedEventArgs args) => args.PropertyName == nameof(IOpenInIDEHotspotViewModel.Column))),
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
            positionCalculatorMock.VerifyAll();
        }

        [TestMethod]
        public void DisplayPath_HotspotHasNoFilePath_ReturnsServerPath()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.ServerFilePath).Returns("\\some\\server\\path.cs");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.CurrentFilePath).Returns((string)null);
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.DisplayPath.Should().Be("path.cs");
        }

        [TestMethod]
        public void DisplayPath_HotspotHasFilePath_ReturnsFilePath()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.CurrentFilePath).Returns("c:\\some\\local\\path.cs");

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.DisplayPath.Should().Be("path.cs");
        }

        [TestMethod]
        public void CategoryDisplayName_ReturnsFriendlyName()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.Rule.SecurityCategory).Returns("some category");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);

            var categoryDisplayNameProvider = new Mock<SharedProvider.ISecurityCategoryDisplayNameProvider>();
            categoryDisplayNameProvider.Setup(x => x.Get("some category")).Returns("some display name");

            var testSubject = CreateTestSubject(issueViz.Object, categoryDisplayNameProvider.Object);
            testSubject.CategoryDisplayName.Should().Be("some display name");
        }

        [TestMethod]
        public void HotspotPropertyChanged_CurrentFilePathProperty_RaisesPropertyChangedForDisplayPath()
        {
            var eventHandler = new Mock<PropertyChangedEventHandler>();
            var issueViz = new Mock<IAnalysisIssueVisualization>();

            var testSubject = CreateTestSubject(issueViz.Object);
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            issueViz.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IAnalysisIssueVisualization.CurrentFilePath)));

            eventHandler.Verify(x => x(testSubject,
                    It.Is((PropertyChangedEventArgs args) => args.PropertyName == nameof(IOpenInIDEHotspotViewModel.DisplayPath))),
                Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        private static OpenInIDEHotspotViewModel CreateTestSubject(IAnalysisIssueVisualization issueViz,
            SharedProvider.ISecurityCategoryDisplayNameProvider securityCategoryDisplayNameProvider = null,
            IIssueVizDisplayPositionCalculator positionCalculator = null)
        {
            securityCategoryDisplayNameProvider ??= Mock.Of<SharedProvider.ISecurityCategoryDisplayNameProvider>();
            positionCalculator ??= Mock.Of<IIssueVizDisplayPositionCalculator>();
            return new OpenInIDEHotspotViewModel(issueViz, securityCategoryDisplayNameProvider, positionCalculator);
        }
    }
}

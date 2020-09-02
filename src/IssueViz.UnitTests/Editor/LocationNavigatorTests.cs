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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class LocationNavigatorTests
    {
        private Mock<IDocumentNavigator> documentOpenerMock;
        private Mock<IIssueSpanCalculator> spanCalculatorMock;
        private TestLogger logger;

        private Mock<ITextView> textViewMock;
        private ITextSnapshot textViewCurrentSnapshotMock;

        private LocationNavigator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            documentOpenerMock = new Mock<IDocumentNavigator>();
            spanCalculatorMock = new Mock<IIssueSpanCalculator>();
            logger = new TestLogger();

            testSubject = new LocationNavigator(documentOpenerMock.Object, spanCalculatorMock.Object, logger);

            textViewCurrentSnapshotMock = Mock.Of<ITextSnapshot>();
            textViewMock = new Mock<ITextView>();
            textViewMock.SetupGet(x => x.TextBuffer.CurrentSnapshot).Returns(textViewCurrentSnapshotMock);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var documentOpenerExport = MefTestHelpers.CreateExport<IDocumentNavigator>(documentOpenerMock.Object);
            var spanCalculatorExport = MefTestHelpers.CreateExport<IIssueSpanCalculator>(spanCalculatorMock.Object);
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(logger);

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<LocationNavigator, ILocationNavigator>(null, new[]
            {
                documentOpenerExport, 
                spanCalculatorExport, 
                loggerExport
            });
        }

        [TestMethod]
        public void TryNavigate_LocationIsNull_ArgumentNullException()
        {
            Action act = () => testSubject.TryNavigate(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("locationVisualization");

            documentOpenerMock.VerifyNoOtherCalls();
            spanCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TryNavigate_FailedToOpenDocument_NoException()
        {
            var location = CreateLocation("c:\\test.cpp");

            var openDocumentException = new NotImplementedException("this is a test");
            documentOpenerMock.Setup(x => x.Open("c:\\test.cpp")).Throws(openDocumentException);

            VerifyExceptionCaughtAndLogged(openDocumentException, location);
            VerifyNoNavigation();
        }

        [TestMethod]
        public void TryNavigate_FailedToCalculateSpan_NoException()
        {
            var location = CreateLocation("c:\\test.cpp");
            SetupDocumentCanBeOpened("c:\\test.cpp");

            var calculateSpanException = new NotImplementedException("this is a test");
            spanCalculatorMock.Setup(x => x.CalculateSpan(location.Location, textViewCurrentSnapshotMock)).Throws(calculateSpanException);

            VerifyExceptionCaughtAndLogged(calculateSpanException, location);
            VerifyNoNavigation();
        }

        [TestMethod]
        public void TryNavigate_FailedToNavigate_NoException()
        {
            var location = CreateLocation("c:\\test.cpp");
            SetupDocumentCanBeOpened("c:\\test.cpp");

            var nonEmptySpan = CreateNonEmptySpan();
            SetupCalculatedSpan(location.Location, nonEmptySpan);

            var navigateException = new NotImplementedException("this is a test");
            documentOpenerMock.Setup(x => x.Navigate(textViewMock.Object, nonEmptySpan)).Throws(navigateException);

            VerifyExceptionCaughtAndLogged(navigateException, location);
        }

        [TestMethod]
        public void TryNavigate_LocationHasValidSpan_NavigateToExistingSpan()
        {
            var nonEmptySpan = CreateNonEmptySpan();
            var location = CreateLocation("c:\\test.cpp", nonEmptySpan);

            SetupDocumentCanBeOpened(location.CurrentFilePath);

            var result = testSubject.TryNavigate(location);
            result.Should().BeTrue();

            spanCalculatorMock.VerifyNoOtherCalls();
            VerifyNavigation(nonEmptySpan);
        }

        [TestMethod]
        [DataRow(LocationSpanInvalidState.Null)]
        [DataRow(LocationSpanInvalidState.Empty)]
        public void TryNavigate_LocationHasNoValidSpan_CalculatedSpanIsEmpty_NoNavigation(LocationSpanInvalidState state)
        {
            var invalidSpan = state == LocationSpanInvalidState.Null ? (SnapshotSpan?) null: new SnapshotSpan();
            var location = CreateLocation("c:\\test.cpp", invalidSpan);
            SetupDocumentCanBeOpened("c:\\test.cpp");

            SetupCalculatedSpan(location.Location, new SnapshotSpan());

            var result = testSubject.TryNavigate(location);
            result.Should().BeFalse();

            spanCalculatorMock.VerifyAll();
            VerifyNoNavigation();
        }

        [TestMethod]
        [DataRow(LocationSpanInvalidState.Null)]
        [DataRow(LocationSpanInvalidState.Empty)]
        public void TryNavigate_LocationHasNoValidSpan_CalculatedSpanIsNotEmpty_NavigateToCalculatedSpan(LocationSpanInvalidState state)
        {
            var invalidSpan = state == LocationSpanInvalidState.Null ? (SnapshotSpan?)null : new SnapshotSpan();
            var location = CreateLocation("c:\\test.cpp", invalidSpan);
            SetupDocumentCanBeOpened("c:\\test.cpp");

            var nonEmptySpan = CreateNonEmptySpan();
            SetupCalculatedSpan(location.Location, nonEmptySpan);

            var result = testSubject.TryNavigate(location);
            result.Should().BeTrue();

            spanCalculatorMock.VerifyAll();
            VerifyNavigation(nonEmptySpan);
        }

        [TestMethod]
        [DataRow(LocationSpanInvalidState.Null)]
        [DataRow(LocationSpanInvalidState.Empty)]
        public void TryNavigate_LocationHasNoValidSpan_LocationSpanIsUpdatedToCalculatedSpan(LocationSpanInvalidState state)
        {
            var invalidSpan = state == LocationSpanInvalidState.Null ? (SnapshotSpan?)null : new SnapshotSpan();
            var location = CreateLocation("c:\\test.cpp", invalidSpan);
            SetupDocumentCanBeOpened("c:\\test.cpp");

            var nonEmptySpan = CreateNonEmptySpan();
            SetupCalculatedSpan(location.Location, nonEmptySpan);

            testSubject.TryNavigate(location);

            location.Span.Should().Be(nonEmptySpan);
        }

        public enum LocationSpanInvalidState
        {
            Null,
            Empty
        }

        private SnapshotSpan CreateNonEmptySpan()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(x => x.Length).Returns(20);

            return new SnapshotSpan(mockTextSnapshot.Object, new Span(0, 10));
        }

        private static IAnalysisIssueLocationVisualization CreateLocation(string filePath, SnapshotSpan? span = null)
        {
            var location = new Mock<IAnalysisIssueLocationVisualization>();
            location.Setup(x => x.Location).Returns(Mock.Of<IAnalysisIssueLocation>());
            location.Setup(x => x.CurrentFilePath).Returns(filePath);
            location.SetupProperty(x => x.Span);
            location.Object.Span = span;

            return location.Object;
        }

        private void SetupDocumentCanBeOpened(string filePath)
        {
            documentOpenerMock.Setup(x => x.Open(filePath)).Returns(textViewMock.Object);
        }

        private void SetupCalculatedSpan(IAnalysisIssueLocation analysisIssueLocation, SnapshotSpan span)
        {
            spanCalculatorMock.Setup(x => x.CalculateSpan(analysisIssueLocation, textViewCurrentSnapshotMock)).Returns(span);
        }

        private void VerifyExceptionCaughtAndLogged(Exception setupException, IAnalysisIssueLocationVisualization location)
        {
            var result = true;
            Action act = () => result = testSubject.TryNavigate(location);
            act.Should().NotThrow();

            result.Should().BeFalse();

            logger.AssertPartialOutputStringExists(setupException.Message);
        }

        private void VerifyNoNavigation()
        {
            documentOpenerMock.Verify(x => x.Navigate(It.IsAny<ITextView>(), It.IsAny<SnapshotSpan>()), Times.Never());
        }

        private void VerifyNavigation(SnapshotSpan span)
        {
            documentOpenerMock.Verify(x => x.Navigate(textViewMock.Object, span), Times.Once);
        }
    }
}

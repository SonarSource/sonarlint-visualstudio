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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;
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
            MefTestHelpers.CheckTypeCanBeImported<LocationNavigator, ILocationNavigator>(
                MefTestHelpers.CreateExport<IDocumentNavigator>(),
                MefTestHelpers.CreateExport<IIssueSpanCalculator>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void TryNavigatePartial_LocationIsNull_ArgumentNullException()
        {
            Action act = () => testSubject.TryNavigatePartial(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("locationVisualization");

            documentOpenerMock.VerifyNoOtherCalls();
            spanCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TryNavigatePartial_ErrorOpeningDocument_NoException()
        {
            var location = CreateLocation("c:\\test.cpp");

            var openDocumentException = new NotImplementedException("this is a test");
            documentOpenerMock.Setup(x => x.Open("c:\\test.cpp")).Throws(openDocumentException);

            VerifyExceptionCaughtAndLogged(openDocumentException, location, NavigationResult.Failed);
            VerifyNoNavigation();
        }

        [TestMethod]
        public void TryNavigatePartial_ErrorOpeningDocument_CriticalException_NotCaught()
        {
            var location = CreateLocation("c:\\test.cpp");

            documentOpenerMock.Setup(x => x.Open("c:\\test.cpp")).Throws<DivideByZeroException>();

            Action act = () => testSubject.TryNavigatePartial(location);
            act.Should().ThrowExactly<DivideByZeroException>();
        }

        [TestMethod]
        public void TryNavigatePartial_ErrorOpeningDocument_LocationSpanIsInvalidated()
        {
            var previousSpan = CreateNonEmptySpan();
            var location = CreateLocation("c:\\test.cpp", previousSpan);

            documentOpenerMock.Setup(x => x.Open("c:\\test.cpp")).Throws<NotImplementedException>();

            testSubject.TryNavigatePartial(location);

            location.Span.Value.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void TryNavigatePartial_ErrorCalculatingSpan_NoException()
        {
            var location = CreateLocation("c:\\test.cpp");
            SetupDocumentCanBeOpened("c:\\test.cpp");

            var calculateSpanException = new NotImplementedException("this is a test");
            spanCalculatorMock.Setup(x => x.CalculateSpan(location.Location.TextRange, textViewCurrentSnapshotMock))
                .Throws(calculateSpanException);

            VerifyExceptionCaughtAndLogged(calculateSpanException, location, NavigationResult.OpenedFile);
            VerifyNoNavigation();
        }

        [TestMethod]
        public void TryNavigatePartial_ErrorCalculatingSpan_CriticalException_NotCaught()
        {
            var location = CreateLocation("c:\\test.cpp");
            SetupDocumentCanBeOpened("c:\\test.cpp");

            spanCalculatorMock.Setup(x => x.CalculateSpan(location.Location.TextRange, textViewCurrentSnapshotMock))
                .Throws<DivideByZeroException>();

            Action act = () => testSubject.TryNavigatePartial(location);
            act.Should().ThrowExactly<DivideByZeroException>();
        }

        [TestMethod]
        public void TryNavigatePartial_ErrorCalculatingSpan_LocationSpanIsInvalidated()
        {
            var location = CreateLocation("c:\\test.cpp");
            SetupDocumentCanBeOpened("c:\\test.cpp");

            spanCalculatorMock.Setup(x => x.CalculateSpan(location.Location.TextRange, textViewCurrentSnapshotMock))
                .Throws<NotImplementedException>();

            testSubject.TryNavigatePartial(location);

            location.Span.Value.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void TryNavigatePartial_ErrorNavigating_NoException()
        {
            var nonEmptySpan = CreateNonEmptySpan();
            var location = CreateLocation("c:\\test.cpp", nonEmptySpan);
            SetupDocumentCanBeOpened("c:\\test.cpp");

            var navigateException = new NotImplementedException("this is a test");
            documentOpenerMock.Setup(x => x.Navigate(textViewMock.Object, nonEmptySpan)).Throws(navigateException);

            VerifyExceptionCaughtAndLogged(navigateException, location, NavigationResult.OpenedFile);
        }

        [TestMethod]
        public void TryNavigatePartial_ErrorNavigating_CriticalException_NotCaught()
        {
            var nonEmptySpan = CreateNonEmptySpan();
            var location = CreateLocation("c:\\test.cpp", nonEmptySpan);
            SetupDocumentCanBeOpened("c:\\test.cpp");

            documentOpenerMock.Setup(x => x.Navigate(textViewMock.Object, nonEmptySpan))
                .Throws<DivideByZeroException>();

            Action act = () => testSubject.TryNavigatePartial(location);
            act.Should().ThrowExactly<DivideByZeroException>();
        }

        [TestMethod]
        public void TryNavigatePartial_ErrorNavigating_LocationSpanIsInvalidated()
        {
            var previousSpan = CreateNonEmptySpan();
            var location = CreateLocation("c:\\test.cpp", previousSpan);
            SetupDocumentCanBeOpened("c:\\test.cpp");

            documentOpenerMock.Setup(x => x.Navigate(textViewMock.Object, previousSpan))
                .Throws<NotImplementedException>();

            testSubject.TryNavigatePartial(location);

            location.Span.Value.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void TryNavigatePartial_LocationHasValidSpan_NavigateToExistingSpan()
        {
            var nonEmptySpan = CreateNonEmptySpan();
            var location = CreateLocation("c:\\test.cpp", nonEmptySpan);

            SetupDocumentCanBeOpened(location.CurrentFilePath);

            var result = testSubject.TryNavigatePartial(location);
            result.Should().Be(NavigationResult.OpenedLocation);

            spanCalculatorMock.VerifyNoOtherCalls();
            VerifyNavigation(nonEmptySpan);
        }

        [TestMethod]
        public void TryNavigatePartial_LocationHasEmptySpan_NoNavigation()
        {
            var emptySpan = new SnapshotSpan();
            var location = CreateLocation("c:\\test.cpp", emptySpan);

            SetupDocumentCanBeOpened(location.CurrentFilePath);

            var result = testSubject.TryNavigatePartial(location);
            result.Should().Be(NavigationResult.OpenedFile);

            spanCalculatorMock.VerifyNoOtherCalls();
            VerifyNoNavigation();
        }

        [TestMethod]
        public void TryNavigatePartial_LocationHasNoSpan_CalculatedSpanIsEmpty_NoNavigation()
        {
            var location = CreateLocation("c:\\test.cpp");
            SetupDocumentCanBeOpened("c:\\test.cpp");

            SetupCalculatedSpan(location.Location, new SnapshotSpan());

            var result = testSubject.TryNavigatePartial(location);
            result.Should().Be(NavigationResult.OpenedFile);

            spanCalculatorMock.VerifyAll();
            VerifyNoNavigation();
        }

        [TestMethod]
        public void TryNavigatePartial_LocationHasNoSpan_CalculatedSpanIsNotEmpty_NavigateToCalculatedSpan()
        {
            var location = CreateLocation("c:\\test.cpp");
            SetupDocumentCanBeOpened("c:\\test.cpp");

            var nonEmptySpan = CreateNonEmptySpan();
            SetupCalculatedSpan(location.Location, nonEmptySpan);

            var result = testSubject.TryNavigatePartial(location);
            result.Should().Be(NavigationResult.OpenedLocation);

            spanCalculatorMock.VerifyAll();
            VerifyNavigation(nonEmptySpan);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TryNavigatePartial_LocationHasNoSpan_LocationSpanIsUpdatedToCalculatedSpan(bool newSpanIsEmpty)
        {
            var location = CreateLocation("c:\\test.cpp");
            SetupDocumentCanBeOpened("c:\\test.cpp");

            var calculatedSpan = newSpanIsEmpty ? new SnapshotSpan() : CreateNonEmptySpan();
            SetupCalculatedSpan(location.Location, calculatedSpan);

            testSubject.TryNavigatePartial(location);

            location.Span.Should().Be(calculatedSpan);
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
            spanCalculatorMock.Setup(x => x.CalculateSpan(analysisIssueLocation.TextRange, textViewCurrentSnapshotMock))
                .Returns(span);
        }

        private void VerifyExceptionCaughtAndLogged(Exception setupException,
            IAnalysisIssueLocationVisualization location, 
            NavigationResult expectedResult)
        { 
            testSubject.TryNavigatePartial(location).Should().Be(expectedResult);
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

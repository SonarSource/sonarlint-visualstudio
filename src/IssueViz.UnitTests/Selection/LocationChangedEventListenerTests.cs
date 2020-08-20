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
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Selection
{
    [TestClass]
    public class LocationChangedEventListenerTests
    {
        private Mock<IDocumentNavigator> documentOpenerMock;
        private Mock<IAnalysisIssueSelectionService> selectionServiceMock;
        private Mock<IIssueSpanCalculator> spanCalculatorMock;
        private TestLogger logger;

        private LocationChangedEventListener testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            documentOpenerMock = new Mock<IDocumentNavigator>();
            selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            spanCalculatorMock = new Mock<IIssueSpanCalculator>();
            logger = new TestLogger();

            testSubject = new LocationChangedEventListener(documentOpenerMock.Object, selectionServiceMock.Object, spanCalculatorMock.Object, logger);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var documentOpenerExport = MefTestHelpers.CreateExport<IDocumentNavigator>(documentOpenerMock.Object);
            var selectionServiceExport = MefTestHelpers.CreateExport<IAnalysisIssueSelectionService>(selectionServiceMock.Object);
            var spanCalculatorExport = MefTestHelpers.CreateExport<IIssueSpanCalculator>(spanCalculatorMock.Object);
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(logger);

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<LocationChangedEventListener, ILocationChangedEventListener>(null, new[] { documentOpenerExport, selectionServiceExport, spanCalculatorExport, loggerExport });
        }

        [TestMethod]
        [DataRow(SelectionChangeLevel.Location)]
        [DataRow(SelectionChangeLevel.Flow)]
        [DataRow(SelectionChangeLevel.Issue)]
        public void OnSelectionChanged_NewLocationIsNull_DocumentNotOpened(SelectionChangeLevel changeLevel)
        {
            RaiseSelectionChangedEvent(changeLevel, null);

            documentOpenerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(SelectionChangeLevel.Location)]
        [DataRow(SelectionChangeLevel.Flow)]
        [DataRow(SelectionChangeLevel.Issue)]
        public void OnSelectionChanged_NewLocationVizHasNoUnderlyingLocation_DocumentNotOpened(SelectionChangeLevel changeLevel)
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.Location).Returns((IAnalysisIssueLocation) null);

            RaiseSelectionChangedEvent(changeLevel, locationViz.Object);

            documentOpenerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(SelectionChangeLevel.Location)]
        [DataRow(SelectionChangeLevel.Flow)]
        [DataRow(SelectionChangeLevel.Issue)]
        public void OnSelectionChanged_NewLocationHasNoFilePath_DocumentNotOpened(SelectionChangeLevel changeLevel)
        {
            var locationViz = CreateLocationWithFilePath("");

            RaiseSelectionChangedEvent(changeLevel, locationViz);

            documentOpenerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(SelectionChangeLevel.Location)]
        [DataRow(SelectionChangeLevel.Flow)]
        [DataRow(SelectionChangeLevel.Issue)]
        public void OnSelectionChanged_NewLocationHasFilePath_DocumentOpenedAndNavigated(SelectionChangeLevel changeLevel)
        {
            var locationViz = CreateLocationWithFilePath("c:\\test.cpp");
            var mockSnapshotSpan = new SnapshotSpan();
            
            SetupMocks(locationViz, mockSnapshotSpan);

            RaiseSelectionChangedEvent(changeLevel, locationViz);

            documentOpenerMock.VerifyAll();
            documentOpenerMock.VerifyNoOtherCalls();

            spanCalculatorMock.VerifyAll();
            spanCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnSelectionChanged_FailedToOpenDocument_NoException()
        {
            var locationViz = CreateLocationWithFilePath("c:\\test.cpp");
            var mockSnapshotSpan = new SnapshotSpan();
            var setupException = new NotImplementedException("this is a test");

            SetupMocks(locationViz, mockSnapshotSpan, openDocumentException: setupException);

            VerifyExceptionCaughtAndLogged(setupException, locationViz);
        }

        [TestMethod]
        public void OnSelectionChanged_FailedToCalculateSpan_NoException()
        {
            var locationViz = CreateLocationWithFilePath("c:\\test.cpp");
            var mockSnapshotSpan = new SnapshotSpan();
            var setupException = new NotImplementedException("this is a test");

            SetupMocks(locationViz, mockSnapshotSpan, calculateSpanException: setupException);

            VerifyExceptionCaughtAndLogged(setupException, locationViz);
        }

        [TestMethod]
        public void OnSelectionChanged_FailedToNavigate_NoException()
        {
            var locationViz = CreateLocationWithFilePath("c:\\test.cpp");
            var mockSnapshotSpan = new SnapshotSpan();
            var setupException = new NotImplementedException("this is a test");

            SetupMocks(locationViz, mockSnapshotSpan, navigateException: setupException);

            VerifyExceptionCaughtAndLogged(setupException, locationViz);
        }

        [TestMethod]
        public void Dispose_UnregisterFromSelectionServiceEvents()
        {
            selectionServiceMock.SetupRemove(m => m.SelectionChanged -= (sender, args) => { });

            testSubject.Dispose();

            selectionServiceMock.VerifyRemove(x => x.SelectionChanged -= It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once);
            selectionServiceMock.VerifyNoOtherCalls();
        }

        private void RaiseSelectionChangedEvent(SelectionChangeLevel changeLevel, IAnalysisIssueLocationVisualization location)
        {
            selectionServiceMock.Raise(x => x.SelectionChanged += null,
                new SelectionChangedEventArgs(changeLevel,
                    Mock.Of<IAnalysisIssueVisualization>(),
                    Mock.Of<IAnalysisIssueFlowVisualization>(),
                    location));
        }

        private static IAnalysisIssueLocationVisualization CreateLocationWithFilePath(string filePath)
        {
            var location = new Mock<IAnalysisIssueLocation>();
            location.Setup(x => x.FilePath).Returns(filePath);

            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.Location).Returns(location.Object);

            return locationViz.Object;
        }

        private void VerifyExceptionCaughtAndLogged(Exception setupException, IAnalysisIssueLocationVisualization locationViz)
        {
            Action act = () => RaiseSelectionChangedEvent(SelectionChangeLevel.Location, locationViz);
            act.Should().NotThrow();

            logger.AssertPartialOutputStringExists(setupException.Message);
        }

        private void SetupMocks(IAnalysisIssueLocationVisualization locationViz,
            SnapshotSpan mockSnapshotSpan,
            Exception openDocumentException = null,
            Exception calculateSpanException = null,
            Exception navigateException = null)
        {
            var mockSnapshot = Mock.Of<ITextSnapshot>();

            var mockTextView = new Mock<ITextView>();
            mockTextView.Setup(x => x.TextBuffer.CurrentSnapshot).Returns(mockSnapshot);

            var setupOpenDocument = documentOpenerMock.Setup(x => x.Open("c:\\test.cpp"));

            if (openDocumentException != null)
            {
                setupOpenDocument.Throws(openDocumentException);
            }
            else
            {
                setupOpenDocument.Returns(mockTextView.Object);
            }

            var setupCalculateSpan = spanCalculatorMock.Setup(x => x.CalculateSpan(locationViz.Location, mockSnapshot));

            if (calculateSpanException != null)
            {
                setupCalculateSpan.Throws(calculateSpanException);
            }
            else
            {
                setupCalculateSpan.Returns(mockSnapshotSpan);
            }

            var setupNavigate = documentOpenerMock.Setup(x => x.Navigate(mockTextView.Object, mockSnapshotSpan));

            if (navigateException != null)
            {
                setupNavigate.Throws(navigateException);
            }
        }
    }
}

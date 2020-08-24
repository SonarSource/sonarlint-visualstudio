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

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class LocationNavigatorTests
    {
        private Mock<IDocumentNavigator> documentOpenerMock;
        private Mock<IIssueSpanCalculator> spanCalculatorMock;
        private TestLogger logger;

        private LocationNavigator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            documentOpenerMock = new Mock<IDocumentNavigator>();
            spanCalculatorMock = new Mock<IIssueSpanCalculator>();
            logger = new TestLogger();

            testSubject = new LocationNavigator(documentOpenerMock.Object, spanCalculatorMock.Object, logger);
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
        public void TryNavigate_LocationIsNull_DocumentNotOpened()
        {
            var result = testSubject.TryNavigate(null);

            result.Should().BeFalse();

            documentOpenerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void TryNavigate_LocationHasNoFilePath_DocumentNotOpened(string filePath)
        {
            var location = CreateLocationWithFilePath(filePath);

            var result = testSubject.TryNavigate(location);

            result.Should().BeFalse();

            documentOpenerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TryNavigate_LocationHasFilePath_SpanIsInvalid_DocumentNotOpened()
        {
            var location = CreateLocationWithFilePath("c:\\test.cpp");

            SetupMocks(location, null);

            var result = testSubject.TryNavigate(location);

            result.Should().BeFalse();

            documentOpenerMock.Verify(x => x.Navigate(It.IsAny<ITextView>(), It.IsAny<SnapshotSpan>()),
                Times.Never);

            documentOpenerMock.VerifyAll();
            documentOpenerMock.VerifyNoOtherCalls();

            spanCalculatorMock.VerifyAll();
            spanCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TryNavigate_LocationHasFilePath_SpanIsValid_DocumentOpenedAndNavigated()
        {
            var location = CreateLocationWithFilePath("c:\\test.cpp");
            var mockSnapshotSpan = new SnapshotSpan();
            
            SetupMocks(location, mockSnapshotSpan);

            var result = testSubject.TryNavigate(location);

            result.Should().BeTrue();

            documentOpenerMock.VerifyAll();
            documentOpenerMock.VerifyNoOtherCalls();

            spanCalculatorMock.VerifyAll();
            spanCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TryNavigate_FailedToOpenDocument_NoException()
        {
            var location = CreateLocationWithFilePath("c:\\test.cpp");
            var mockSnapshotSpan = new SnapshotSpan();
            var setupException = new NotImplementedException("this is a test");

            SetupMocks(location, mockSnapshotSpan, openDocumentException: setupException);

            VerifyExceptionCaughtAndLogged(setupException, location);
        }

        [TestMethod]
        public void TryNavigate_FailedToCalculateSpan_NoException()
        {
            var location = CreateLocationWithFilePath("c:\\test.cpp");
            var mockSnapshotSpan = new SnapshotSpan();
            var setupException = new NotImplementedException("this is a test");

            SetupMocks(location, mockSnapshotSpan, calculateSpanException: setupException);

            VerifyExceptionCaughtAndLogged(setupException, location);
        }

        [TestMethod]
        public void TryNavigate_FailedToNavigate_NoException()
        {
            var location = CreateLocationWithFilePath("c:\\test.cpp");
            var mockSnapshotSpan = new SnapshotSpan();
            var setupException = new NotImplementedException("this is a test");

            SetupMocks(location, mockSnapshotSpan, navigateException: setupException);

            VerifyExceptionCaughtAndLogged(setupException, location);
        }

        private static IAnalysisIssueLocation CreateLocationWithFilePath(string filePath)
        {
            var location = new Mock<IAnalysisIssueLocation>();
            location.Setup(x => x.FilePath).Returns(filePath);

            return location.Object;
        }

        private void VerifyExceptionCaughtAndLogged(Exception setupException, IAnalysisIssueLocation location)
        {
            bool result = true;
            Action act = () => result = testSubject.TryNavigate(location);
            act.Should().NotThrow();

            result.Should().BeFalse();

            logger.AssertPartialOutputStringExists(setupException.Message);
        }

        private void SetupMocks(IAnalysisIssueLocation location,
            SnapshotSpan? mockSnapshotSpan,
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

            var setupCalculateSpan = spanCalculatorMock.Setup(x => x.CalculateSpan(location, mockSnapshot));

            if (calculateSpanException != null)
            {
                setupCalculateSpan.Throws(calculateSpanException);
            }
            else
            {
                setupCalculateSpan.Returns(mockSnapshotSpan);
            }

            if (mockSnapshotSpan != null)
            {
                var setupNavigate = documentOpenerMock.Setup(x => x.Navigate(mockTextView.Object, mockSnapshotSpan.Value));

                if (navigateException != null)
                {
                    setupNavigate.Throws(navigateException);
                }
            }
        }
    }
}

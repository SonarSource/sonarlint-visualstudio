/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor;

[TestClass]
public class LocationNavigatorTests
{
    private IDocumentNavigator documentOpenerMock;
    private IIssueSpanCalculator spanCalculatorMock;
    private TestLogger logger;

    private ITextView textViewMock;
    private ITextSnapshot textViewCurrentSnapshotMock;

    private LocationNavigator testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        documentOpenerMock = Substitute.For<IDocumentNavigator>();
        spanCalculatorMock = Substitute.For<IIssueSpanCalculator>();
        logger = new TestLogger();

        testSubject = new LocationNavigator(documentOpenerMock, spanCalculatorMock, logger);

        textViewCurrentSnapshotMock = Substitute.For<ITextSnapshot>();
        textViewMock = Substitute.For<ITextView>();
        textViewMock.TextBuffer.CurrentSnapshot.Returns(textViewCurrentSnapshotMock);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<LocationNavigator, ILocationNavigator>(
            MefTestHelpers.CreateExport<IDocumentNavigator>(),
            MefTestHelpers.CreateExport<IIssueSpanCalculator>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void TryNavigatePartial_LocationIsNull_ArgumentNullException()
    {
        Action act = () => testSubject.TryNavigatePartial(null);

        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("locationVisualization");

        documentOpenerMock.DidNotReceiveWithAnyArgs().Open(default);
        spanCalculatorMock.DidNotReceiveWithAnyArgs().CalculateSpan(default, default);
    }

    [TestMethod]
    public void TryNavigatePartial_ErrorOpeningDocument_NoException()
    {
        var location = CreateLocation("c:\\test.cpp");

        var openDocumentException = new NotImplementedException("this is a test");
        documentOpenerMock
            .When(x => x.Open("c:\\test.cpp"))
            .Do(_ => throw openDocumentException);

        VerifyExceptionCaughtAndLogged(openDocumentException, location, NavigationResult.Failed);
        VerifyNoNavigation();
    }

    [TestMethod]
    public void TryNavigatePartial_ErrorOpeningDocument_CriticalException_NotCaught()
    {
        var location = CreateLocation("c:\\test.cpp");

        documentOpenerMock
            .When(x => x.Open("c:\\test.cpp"))
            .Do(_ => throw new DivideByZeroException());

        Action act = () => testSubject.TryNavigatePartial(location);
        act.Should().ThrowExactly<DivideByZeroException>();
    }

    [TestMethod]
    public void TryNavigatePartial_ErrorOpeningDocument_LocationSpanIsInvalidated()
    {
        var previousSpan = CreateNonEmptySpan();
        var location = CreateLocation("c:\\test.cpp", previousSpan);

        documentOpenerMock
            .When(x => x.Open("c:\\test.cpp"))
            .Do(_ => throw new NotImplementedException());

        testSubject.TryNavigatePartial(location);

        location.Span.Value.IsEmpty.Should().BeTrue();
    }

    [TestMethod]
    public void TryNavigatePartial_ErrorCalculatingSpan_NoException()
    {
        var location = CreateLocation("c:\\test.cpp");
        SetupDocumentCanBeOpened("c:\\test.cpp");

        var calculateSpanException = new NotImplementedException("this is a test");
        spanCalculatorMock
            .When(x => x.CalculateSpan(location.Location.TextRange, textViewCurrentSnapshotMock))
            .Do(_ => throw calculateSpanException);

        VerifyExceptionCaughtAndLogged(calculateSpanException, location, NavigationResult.OpenedFile);
        VerifyNoNavigation();
    }

    [TestMethod]
    public void TryNavigatePartial_ErrorCalculatingSpan_CriticalException_NotCaught()
    {
        var location = CreateLocation("c:\\test.cpp");
        SetupDocumentCanBeOpened("c:\\test.cpp");

        spanCalculatorMock
            .When(x => x.CalculateSpan(location.Location.TextRange, textViewCurrentSnapshotMock))
            .Do(_ => throw new DivideByZeroException());

        Action act = () => testSubject.TryNavigatePartial(location);
        act.Should().ThrowExactly<DivideByZeroException>();
    }

    [TestMethod]
    public void TryNavigatePartial_ErrorCalculatingSpan_LocationSpanIsInvalidated()
    {
        var location = CreateLocation("c:\\test.cpp");
        SetupDocumentCanBeOpened("c:\\test.cpp");

        spanCalculatorMock
            .When(x => x.CalculateSpan(location.Location.TextRange, textViewCurrentSnapshotMock))
            .Do(_ => throw new NotImplementedException());

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
        documentOpenerMock
            .When(x => x.Navigate(textViewMock, nonEmptySpan))
            .Do(_ => throw navigateException);

        VerifyExceptionCaughtAndLogged(navigateException, location, NavigationResult.OpenedFile);
    }

    [TestMethod]
    public void TryNavigatePartial_ErrorNavigating_CriticalException_NotCaught()
    {
        var nonEmptySpan = CreateNonEmptySpan();
        var location = CreateLocation("c:\\test.cpp", nonEmptySpan);
        SetupDocumentCanBeOpened("c:\\test.cpp");

        documentOpenerMock
            .When(x => x.Navigate(textViewMock, nonEmptySpan))
            .Do(_ => throw new DivideByZeroException());

        Action act = () => testSubject.TryNavigatePartial(location);
        act.Should().ThrowExactly<DivideByZeroException>();
    }

    [TestMethod]
    public void TryNavigatePartial_ErrorNavigating_LocationSpanIsInvalidated()
    {
        var previousSpan = CreateNonEmptySpan();
        var location = CreateLocation("c:\\test.cpp", previousSpan);
        SetupDocumentCanBeOpened("c:\\test.cpp");

        documentOpenerMock
            .When(x => x.Navigate(textViewMock, previousSpan))
            .Do(_ => throw new NotImplementedException());

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

        spanCalculatorMock.DidNotReceiveWithAnyArgs().CalculateSpan(default, default);
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

        spanCalculatorMock.DidNotReceiveWithAnyArgs().CalculateSpan(default, default);
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

        spanCalculatorMock.Received(1).CalculateSpan(location.Location.TextRange, textViewCurrentSnapshotMock);
        VerifyNoNavigation();
    }

    [TestMethod]
    public void TryNavigatePartial_CalculatedSpanIsNotEmpty_NavigateToCalculatedSpan()
    {
        var location = CreateLocation("c:\\test.cpp");
        SetupDocumentCanBeOpened("c:\\test.cpp");

        var nonEmptySpan = CreateNonEmptySpan();
        SetupCalculatedSpan(location.Location, nonEmptySpan);

        var result = testSubject.TryNavigatePartial(location);
        result.Should().Be(NavigationResult.OpenedLocation);

        spanCalculatorMock.Received(1).CalculateSpan(location.Location.TextRange, textViewCurrentSnapshotMock);
        VerifyNavigation(nonEmptySpan);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryNavigatePartial_LocationSpanIsUpdatedToCalculatedSpan(bool newSpanIsEmpty)
    {
        var location = CreateLocation("c:\\test.cpp");
        SetupDocumentCanBeOpened("c:\\test.cpp");

        var calculatedSpan = newSpanIsEmpty ? new SnapshotSpan() : CreateNonEmptySpan();
        SetupCalculatedSpan(location.Location, calculatedSpan);

        testSubject.TryNavigatePartial(location).Should().Be(
            newSpanIsEmpty ? NavigationResult.OpenedFile : NavigationResult.OpenedLocation);

        location.Span.Should().Be(calculatedSpan);
    }

    [TestMethod]
    public void TryNavigateFile_FilePathIsNull_ArgumentNullException()
    {
        Action act = () => testSubject.TryNavigateFile(null);

        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("filePath");
        documentOpenerMock.DidNotReceiveWithAnyArgs().Open(default);
    }

    [TestMethod]
    public void TryNavigateFile_ErrorOpeningDocument_NoException()
    {
        var filePath = "c:\\test.cpp";
        var openDocumentException = new NotImplementedException("this is a test");
        documentOpenerMock
            .When(x => x.Open(filePath))
            .Do(_ => throw openDocumentException);

        var result = testSubject.TryNavigateFile(filePath);

        result.Should().Be(NavigationResult.Failed);
        logger.AssertPartialOutputStringExists(openDocumentException.Message);
    }

    [TestMethod]
    public void TryNavigateFile_ErrorOpeningDocument_CriticalException_NotCaught()
    {
        var filePath = "c:\\test.cpp";
        documentOpenerMock
            .When(x => x.Open(filePath))
            .Do(_ => throw new DivideByZeroException());

        Action act = () => testSubject.TryNavigateFile(filePath);

        act.Should().ThrowExactly<DivideByZeroException>();
    }

    [TestMethod]
    public void TryNavigateFile_DocumentOpenSucceeds_ReturnsOpenedFile()
    {
        var filePath = "c:\\test.cpp";
        documentOpenerMock.Open(filePath).Returns(textViewMock);

        var result = testSubject.TryNavigateFile(filePath);

        result.Should().Be(NavigationResult.OpenedFile);
    }

    [TestMethod]
    public void TryNavigateFile_DocumentOpenFails_ReturnsFailed()
    {
        var filePath = "c:\\test.cpp";
        documentOpenerMock
            .When(x => x.Open(filePath))
            .Do(_ => throw new Exception("fail"));

        var result = testSubject.TryNavigateFile(filePath);

        result.Should().Be(NavigationResult.Failed);
    }

    private SnapshotSpan CreateNonEmptySpan()
    {
        var mockTextSnapshot = Substitute.For<ITextSnapshot>();
        mockTextSnapshot.Length.Returns(20);

        return new SnapshotSpan(mockTextSnapshot, new Span(0, 10));
    }

    private static IAnalysisIssueLocationVisualization CreateLocation(string filePath, SnapshotSpan? span = null)
    {
        var location = Substitute.For<IAnalysisIssueLocationVisualization>();
        location.Location.Returns(Substitute.For<IAnalysisIssueLocation>());
        location.CurrentFilePath.Returns(filePath);
        location.Span = span;
        return location;
    }

    private void SetupDocumentCanBeOpened(string filePath)
    {
        documentOpenerMock.Open(filePath).Returns(textViewMock);
    }

    private void SetupCalculatedSpan(IAnalysisIssueLocation analysisIssueLocation, SnapshotSpan span)
    {
        spanCalculatorMock.CalculateSpan(analysisIssueLocation.TextRange, textViewCurrentSnapshotMock).Returns(span);
    }

    private void VerifyExceptionCaughtAndLogged(
        Exception setupException,
        IAnalysisIssueLocationVisualization location,
        NavigationResult expectedResult)
    {
        testSubject.TryNavigatePartial(location).Should().Be(expectedResult);
        logger.AssertPartialOutputStringExists(setupException.Message);
    }

    private void VerifyNoNavigation()
    {
        documentOpenerMock.DidNotReceive().Navigate(Arg.Any<ITextView>(), Arg.Any<SnapshotSpan>());
    }

    private void VerifyNavigation(SnapshotSpan span)
    {
        documentOpenerMock.Received(1).Navigate(textViewMock, span);
    }
}

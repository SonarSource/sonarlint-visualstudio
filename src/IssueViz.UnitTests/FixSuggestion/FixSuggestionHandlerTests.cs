/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using FileEditDto = SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models.FileEditDto;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.FixSuggestion;

[TestClass]
public class FixSuggestionHandlerTests
{
    private const string ConfigurationScopeRoot = @"C:\";
    private readonly ShowFixSuggestionParams suggestionWithOneChange = CreateFixSuggestionParams(changes: CreateChangesDto(1, 1, "var a=1;"));
    private IDiffViewService diffViewService;
    private IDocumentNavigator documentNavigator;
    private IFixSuggestionNotification fixSuggestionNotification;
    private IIDEWindowService ideWindowService;
    private IIssueSpanCalculator issueSpanCalculator;
    private ILogger logger;
    private IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator;
    private FixSuggestionHandler testSubject;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        threadHandling = new NoOpThreadHandler();
        logger = Substitute.For<ILogger>();
        documentNavigator = Substitute.For<IDocumentNavigator>();
        issueSpanCalculator = Substitute.For<IIssueSpanCalculator>();
        openInIdeConfigScopeValidator = Substitute.For<IOpenInIdeConfigScopeValidator>();
        ideWindowService = Substitute.For<IIDEWindowService>();
        fixSuggestionNotification = Substitute.For<IFixSuggestionNotification>();
        diffViewService = Substitute.For<IDiffViewService>();

        testSubject = new FixSuggestionHandler(
            threadHandling,
            logger,
            documentNavigator,
            issueSpanCalculator,
            openInIdeConfigScopeValidator,
            ideWindowService,
            fixSuggestionNotification,
            diffViewService);
        MockConfigScopeRoot();
        issueSpanCalculator.IsSameHash(Arg.Any<SnapshotSpan>(), Arg.Any<string>()).Returns(true);
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<FixSuggestionHandler>();

    [TestMethod]
    public void ApplyFixSuggestion_RunsOnUIThread()
    {
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        var fixSuggestionHandler = new FixSuggestionHandler(
            threadHandlingMock,
            logger,
            documentNavigator,
            issueSpanCalculator,
            openInIdeConfigScopeValidator,
            ideWindowService,
            fixSuggestionNotification,
            diffViewService);

        fixSuggestionHandler.ApplyFixSuggestion(suggestionWithOneChange);

        threadHandlingMock.ReceivedWithAnyArgs().RunOnUIThread(default);
    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChangeAccepted_AppliesChange()
    {
        var suggestedChange = suggestionWithOneChange.fixSuggestion.fileEdit.changes[0];
        MockCalculateSpan(suggestedChange);
        var textView = MockOpenFile();
        var edit = MockTextEdit(textView);
        MockChangeAccepted();

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
            ideWindowService.BringToFront();
            fixSuggestionNotification.Clear();
            documentNavigator.Open(@"C:\myFile.cs");
            textView.TextBuffer.CreateEdit();
            issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), suggestedChange.beforeLineRange.startLine, suggestedChange.beforeLineRange.endLine);
            edit.Replace(Arg.Any<Span>(), suggestedChange.after);
            edit.Apply();
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
        });
    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChangeNotAccepted_DoesNotApplyChange()
    {
        var suggestedChange = suggestionWithOneChange.fixSuggestion.fileEdit.changes[0];
        MockCalculateSpan(suggestedChange);
        var textView = MockOpenFile();
        var edit = MockTextEdit(textView);
        MockChangeNotAccepted();

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
            ideWindowService.BringToFront();
            fixSuggestionNotification.Clear();
            documentNavigator.Open(@"C:\myFile.cs");
            textView.TextBuffer.CreateEdit();
            issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), suggestedChange.beforeLineRange.startLine, suggestedChange.beforeLineRange.endLine);
            edit.Apply();
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
        });

        edit.DidNotReceive().Replace(Arg.Any<Span>(), suggestedChange.after);
    }

    [TestMethod]
    public void ApplyFixSuggestion_TwoChanges_AppliesChangeOnce()
    {
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>()).Returns(_ => CreateMockedSnapshotSpan(string.Empty));
        var suggestionWithTwoChanges = CreateFixSuggestionParams(changes: [CreateChangesDto(1, 1, "var a=1;"), CreateChangesDto(2, 2, "var b=0;")]);
        var textView = MockOpenFile();
        var edit = MockTextEdit(textView);
        MockChangeAccepted();

        testSubject.ApplyFixSuggestion(suggestionWithTwoChanges);

        issueSpanCalculator.Received(2).CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>());
        edit.Received(2).Replace(Arg.Any<Span>(), Arg.Any<string>());
        edit.Received(1).Apply();
    }

    /// <summary>
    ///     The changes are applied from bottom to top to avoid changing the line numbers
    ///     of the changes that are below the current change.
    ///     This is important when the change is more lines than the original line range.
    /// </summary>
    [TestMethod]
    public void ApplyFixSuggestion_WhenMoreThanOneFixes_ApplyThemFromBottomToTop()
    {
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>()).Returns(_ => CreateMockedSnapshotSpan(string.Empty));
        MockOpenFile();
        ChangesDto[] changes = [CreateChangesDto(1, 1, "var a=1;"), CreateChangesDto(3, 3, "var b=0;")];
        var suggestionParams = CreateFixSuggestionParams(changes: changes);
        MockChangeAccepted();

        testSubject.ApplyFixSuggestion(suggestionParams);

        Received.InOrder(() =>
        {
            issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), 3, 3);
            issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), 1, 1);
        });
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChange_BringWindowToFront()
    {
        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        ideWindowService.Received().BringToFront();
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChange_BringFocusToFirstChangedLines()
    {
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>()).Returns(_ => CreateMockedSnapshotSpan(string.Empty));
        ChangesDto[] changes = [CreateChangesDto(1, 1, "var a=1;"), CreateChangesDto(3, 3, "var a=1;")];
        var suggestionParams = CreateFixSuggestionParams(changes: changes);
        var firstSuggestedChange = suggestionParams.fixSuggestion.fileEdit.changes[0];
        var firstAffectedSnapshot = MockCalculateSpan(firstSuggestedChange);
        var textView = MockOpenFile();
        MockConfigScopeRoot();
        MockChangeAccepted();

        testSubject.ApplyFixSuggestion(suggestionParams);

        textView.ViewScroller.ReceivedWithAnyArgs(1).EnsureSpanVisible(Arg.Any<SnapshotSpan>(), default);
        textView.ViewScroller.Received(1).EnsureSpanVisible(firstAffectedSnapshot, EnsureSpanVisibleOptions.AlwaysCenter);
    }

    [TestMethod]
    public void ApplyFixSuggestion_Throws_Logs()
    {
        var exceptionMsg = "error";
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>()).Throws(new Exception(exceptionMsg));

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
            logger.WriteLine(FixSuggestionResources.ProcessingRequestFailed, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId, exceptionMsg);
        });
        logger.DidNotReceive().WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenConfigRootScopeNotFound_ShouldLogFailureAndShowNotification()
    {
        var reason = "Scope not found";
        MockFailedConfigScopeRoot(reason);
        var suggestionParams = CreateFixSuggestionParams("SpecificConfigScopeId");

        testSubject.ApplyFixSuggestion(suggestionParams);

        logger.Received().WriteLine(FixSuggestionResources.GetConfigScopeRootPathFailed, "SpecificConfigScopeId", "Scope not found");
        fixSuggestionNotification.Received(1).InvalidRequest(reason);
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenLineNumbersDoNotMatch_ShouldLogFailure()
    {
        FailWhenApplyingEdit("Line numbers do not match");

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        logger.Received().WriteLine(FixSuggestionResources.ProcessingRequestFailed,
            suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId,
            "Line numbers do not match");
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChangeAndExceptionIsThrown_ShouldDisposeEdit()
    {
        var edit = FailWhenApplyingEdit();

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        edit.DidNotReceiveWithAnyArgs().Replace(default, default);
        edit.Received().Dispose();
    }

    [TestMethod]
    public void ApplyFixSuggestion_FileCanNotBeOpened_LogsAndShowsNotification()
    {
        var errorMessage = "error";
        documentNavigator.Open(Arg.Any<string>()).Throws(new Exception(errorMessage));

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        logger.Received().WriteLine(Resources.ERR_OpenDocumentException, GetAbsolutePathOfFile(suggestionWithOneChange), errorMessage);
        fixSuggestionNotification.Received(1).UnableToOpenFile(Arg.Is<string>(msg => msg == GetAbsolutePathOfFile(suggestionWithOneChange)));
    }

    [TestMethod]
    public void ApplyFixSuggestion_FileContentIsNull_LogsAndShowsNotification()
    {
        documentNavigator.Open(Arg.Any<string>()).Returns((ITextView)null);

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        logger.DidNotReceive().WriteLine(Resources.ERR_OpenDocumentException, Arg.Any<string>(), Arg.Any<string>());
        fixSuggestionNotification.Received(1).UnableToOpenFile(Arg.Is<string>(msg => msg == GetAbsolutePathOfFile(suggestionWithOneChange)));
    }

    [TestMethod]
    public void ApplyFixSuggestion_ClearsPreviousNotification()
    {
        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        fixSuggestionNotification.Received(1).Clear();
    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChange_IssueCanNotBeLocated_ShowsNotificationAndDoesNotApplySuggestion()
    {
        var edit = MockTextEdit();
        issueSpanCalculator.IsSameHash(Arg.Any<SnapshotSpan>(), Arg.Any<string>()).Returns(false);

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        VerifyFixSuggestionNotApplied(edit);
    }

    [TestMethod]
    public void ApplyFixSuggestion_TwoChanges_OneIssueCanNotBeLocated_ShowsNotificationAndDoesNotApplySuggestion()
    {
        var suggestionParams = CreateFixSuggestionParams(changes: [CreateChangesDto(1, 1, "var a=1;"), CreateChangesDto(2, 2, "var b=0;")]);
        var edit = MockTextEdit();
        issueSpanCalculator.IsSameHash(Arg.Any<SnapshotSpan>(), Arg.Any<string>()).Returns(
            _ => true,
            _ => false);

        testSubject.ApplyFixSuggestion(suggestionParams);

        VerifyFixSuggestionNotApplied(edit);
    }

    [TestMethod]
    public void ApplyFixSuggestion_TwoChanges_ShowsCorrectDiffView()
    {
        var suggestionWithTwoChanges = CreateFixSuggestionParams(changes: [CreateChangesDto(1, 1, "int a=1;", "var a=1;"), CreateChangesDto(2, 2, "int b=0;", "var b=0;")]);
        MockCalculateSpan(suggestionWithTwoChanges.fixSuggestion.fileEdit.changes[0]);
        MockCalculateSpan(suggestionWithTwoChanges.fixSuggestion.fileEdit.changes[1]);
        var textView = MockOpenFile();
        var edit = MockTextEdit(textView);
        MockChangeAccepted();

        testSubject.ApplyFixSuggestion(suggestionWithTwoChanges);

        Received.InOrder(() =>
        {
            diffViewService.ShowDiffView(Arg.Is<FixSuggestionDetails>(x => IsExpectedFixSuggestionDetails(x, 1, 2)),
                Arg.Any<ChangeModel>(), // difficult to mock the GetText of the SnapshotSpan
                Arg.Is<ChangeModel>(x => IsExpectedChangeModel(x, "var b=0;", edit.Snapshot.ContentType)));
            diffViewService.ShowDiffView(Arg.Is<FixSuggestionDetails>(x => IsExpectedFixSuggestionDetails(x, 2, 2)),
                Arg.Any<ChangeModel>(),
                Arg.Is<ChangeModel>(x => IsExpectedChangeModel(x, "var a=1;", edit.Snapshot.ContentType)));
        });
    }

    [TestMethod]
    public void ApplyFixSuggestion_TwoChangesAndJustOneAccepted_ReplacesJustOne()
    {
        var suggestionWithTwoChanges = CreateFixSuggestionParams(changes: [CreateChangesDto(1, 1, "int a=1;", "var a=1;"), CreateChangesDto(2, 2, "int b=0;", "var b=0;")]);
        MockCalculateSpan(suggestionWithTwoChanges.fixSuggestion.fileEdit.changes[0]);
        MockCalculateSpan(suggestionWithTwoChanges.fixSuggestion.fileEdit.changes[1]);
        var textView = MockOpenFile();
        var edit = MockTextEdit(textView);
        MockDiffView(1, "var b=0;", true);
        MockDiffView(2, "var a=1;", false);

        testSubject.ApplyFixSuggestion(suggestionWithTwoChanges);

        edit.Received(1).Replace(Arg.Any<Span>(), "var b=0;");
        edit.DidNotReceive().Replace(Arg.Any<Span>(), "var a=1;");
    }

    private bool IsExpectedFixSuggestionDetails(FixSuggestionDetails fixSuggestionDetails, int expectedIndex, int expectedTotal) =>
        fixSuggestionDetails.ChangeIndex == expectedIndex &&
        fixSuggestionDetails.TotalChangesFixes == expectedTotal &&
        fixSuggestionDetails.FileName == @"C:\myFile.cs";

    private bool IsExpectedChangeModel(ChangeModel changeModel, string expectedText, IContentType contentType) => changeModel.Text == expectedText && changeModel.ContentType == contentType;

    private void MockConfigScopeRoot() =>
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(Arg.Any<string>(), out Arg.Any<string>(), out Arg.Any<string>()).Returns(
            x =>
            {
                x[1] = ConfigurationScopeRoot;
                return true;
            });

    private void MockFailedConfigScopeRoot(string failureReason) =>
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(Arg.Any<string>(), out Arg.Any<string>(), out Arg.Any<string>()).Returns(
            x =>
            {
                x[2] = failureReason;
                return false;
            });

    private ITextView MockOpenFile()
    {
        var textView = Substitute.For<ITextView>();
        documentNavigator.Open(Arg.Any<string>()).Returns(textView);
        return textView;
    }

    private SnapshotSpan MockCalculateSpan(ChangesDto suggestedChange) => MockCalculateSpan(suggestedChange.before, suggestedChange.beforeLineRange.startLine, suggestedChange.beforeLineRange.endLine);

    private SnapshotSpan MockCalculateSpan(string text, int startLine, int endLine)
    {
        var affectedSnapshot = CreateMockedSnapshotSpan(text);
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), startLine, endLine).Returns(affectedSnapshot);
        issueSpanCalculator.IsSameHash(affectedSnapshot, text).Returns(true);
        return affectedSnapshot;
    }

    private static SnapshotSpan CreateMockedSnapshotSpan(string text)
    {
        var mockTextSnapshot = Substitute.For<ITextSnapshot>();
        mockTextSnapshot.Length.Returns(text.Length + 9999);

        return new SnapshotSpan(mockTextSnapshot, new Span(0, text.Length));
    }

    private ITextEdit FailWhenApplyingEdit(string reason = "")
    {
        var edit = Substitute.For<ITextEdit>();
        var textView = MockOpenFile();
        textView.TextBuffer.CreateEdit().Returns(edit);
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>())
            .Throws(new Exception(reason));
        return edit;
    }

    private static ShowFixSuggestionParams CreateFixSuggestionParams(
        string scopeId = "scopeId",
        string suggestionKey = "suggestionKey",
        string idePath = @"myFile.cs",
        params ChangesDto[] changes)
    {
        var fixSuggestion = new FixSuggestionDto(suggestionKey, "refactor", new FileEditDto(idePath, changes.ToList()));
        var suggestionParams = new ShowFixSuggestionParams(scopeId, "key", fixSuggestion);
        return suggestionParams;
    }

    private static ChangesDto CreateChangesDto(
        int startLine,
        int endLine,
        string before,
        string after = "") =>
        new(new LineRangeDto(startLine, endLine), before, after);

    private static string GetAbsolutePathOfFile(ShowFixSuggestionParams suggestionParams) => Path.Combine(ConfigurationScopeRoot, suggestionParams.fixSuggestion.fileEdit.idePath);

    private ITextEdit MockTextEdit(ITextView textView = null)
    {
        var edit = Substitute.For<ITextEdit>();
        textView ??= MockOpenFile();
        textView.TextBuffer.CreateEdit().Returns(edit);
        return edit;
    }

    private void VerifyFixSuggestionNotApplied(ITextEdit edit)
    {
        Received.InOrder(() =>
        {
            fixSuggestionNotification.UnableToLocateIssue(Arg.Is<string>(msg => msg == GetAbsolutePathOfFile(suggestionWithOneChange)));
            edit.Dispose();
        });
        edit.DidNotReceive().Apply();
    }

    private void MockChangeAccepted() => MockDiffView(true);

    private void MockChangeNotAccepted() => MockDiffView(false);

    private void MockDiffView(bool accepted) => diffViewService.ShowDiffView(Arg.Any<FixSuggestionDetails>(), Arg.Any<ChangeModel>(), Arg.Any<ChangeModel>()).Returns(accepted);

    private void MockDiffView(int index, string after, bool accepted) =>
        diffViewService.ShowDiffView(Arg.Is<FixSuggestionDetails>(x => x.ChangeIndex == index), Arg.Any<ChangeModel>(), Arg.Is<ChangeModel>(x => x.Text == after)).Returns(accepted);
}

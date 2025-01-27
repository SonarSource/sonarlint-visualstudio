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
    private readonly ShowFixSuggestionParams suggestionWithTwoChanges = CreateFixSuggestionParams(changes: [CreateChangesDto(1, 1, "var a=1;"), CreateChangesDto(1, 1, "var b=0;")]);
    private IDiffViewService diffViewService;
    private IDocumentNavigator documentNavigator;
    private IFixSuggestionNotification fixSuggestionNotification;
    private IIDEWindowService ideWindowService;
    private ITextViewEditor textViewEditor;
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
        textViewEditor = Substitute.For<ITextViewEditor>();
        openInIdeConfigScopeValidator = Substitute.For<IOpenInIdeConfigScopeValidator>();
        ideWindowService = Substitute.For<IIDEWindowService>();
        fixSuggestionNotification = Substitute.For<IFixSuggestionNotification>();
        diffViewService = Substitute.For<IDiffViewService>();

        testSubject = new FixSuggestionHandler(
            threadHandling,
            logger,
            documentNavigator,
            textViewEditor,
            openInIdeConfigScopeValidator,
            ideWindowService,
            fixSuggestionNotification,
            diffViewService);
        MockConfigScopeRoot();
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
            textViewEditor,
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
        var changes = suggestionWithOneChange.fixSuggestion.fileEdit.changes;
        MockOpenFile();
        MockDiffView(changes);

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
            ideWindowService.BringToFront();
            fixSuggestionNotification.Clear();
            documentNavigator.Open(@"C:\myFile.cs");
            diffViewService.ShowDiffView(Arg.Any<ITextBuffer>(), changes);
            textViewEditor.ApplyChanges(Arg.Any<ITextBuffer>(), changes, true);
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
        });
    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChangeNotAccepted_DoesNotApplyChange()
    {
        var changes = suggestionWithOneChange.fixSuggestion.fileEdit.changes;
        MockOpenFile();
        MockDiffView([]);

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
            ideWindowService.BringToFront();
            fixSuggestionNotification.Clear();
            documentNavigator.Open(@"C:\myFile.cs");
            diffViewService.ShowDiffView(Arg.Any<ITextBuffer>(), changes);
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionWithOneChange.configurationScopeId, suggestionWithOneChange.fixSuggestion.suggestionId);
        });
        textViewEditor.DidNotReceiveWithAnyArgs().ApplyChanges(default, default, default);
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChange_BringWindowToFront()
    {
        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        ideWindowService.Received().BringToFront();
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChangeSucceeded_BringFocusToFirstChangedLines()
    {
        var changes = suggestionWithTwoChanges.fixSuggestion.fileEdit.changes;
        var textView = MockOpenFile();
        MockDiffView(suggestionWithTwoChanges.fixSuggestion.fileEdit.changes);
        textViewEditor.ApplyChanges(Arg.Any<ITextBuffer>(), Arg.Any<List<ChangesDto>>(), Arg.Any<bool>()).Returns(true);

        testSubject.ApplyFixSuggestion(suggestionWithTwoChanges);

        textViewEditor.Received(1).FocusLine(textView, changes[0].beforeLineRange.startLine);
    }

    [TestMethod]
    public void ApplyFixSuggestion_Throws_Logs()
    {
        MockDiffView(suggestionWithTwoChanges.fixSuggestion.fileEdit.changes);
        var exceptionMsg = "error";
        textViewEditor.ApplyChanges(Arg.Any<ITextBuffer>(), Arg.Any<List<ChangesDto>>(), Arg.Any<bool>()).Throws(new Exception(exceptionMsg));

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
    public void ApplyFixSuggestion_OneChange_ChangesCanNotBeApplied_ShowsNotification()
    {
        MockDiffView(suggestionWithOneChange.fixSuggestion.fileEdit.changes);
        textViewEditor.ApplyChanges(Arg.Any<ITextBuffer>(), Arg.Any<List<ChangesDto>>(), Arg.Any<bool>()).Returns(false);

        testSubject.ApplyFixSuggestion(suggestionWithOneChange);

        VerifyFixSuggestionNotApplied();
    }

    [TestMethod]
    public void ApplyFixSuggestion_TwoChanges_ShowsCorrectDiffView()
    {
        var textView = MockOpenFile();

        testSubject.ApplyFixSuggestion(suggestionWithTwoChanges);

        diffViewService.Received(1).ShowDiffView(textView.TextBuffer, suggestionWithTwoChanges.fixSuggestion.fileEdit.changes);
    }

    [TestMethod]
    public void ApplyFixSuggestion_TwoChangesAndJustOneAccepted_AppliesJustOne()
    {
        var textView = MockOpenFile();
        var acceptedChange = suggestionWithTwoChanges.fixSuggestion.fileEdit.changes[0];
        diffViewService.ShowDiffView(Arg.Any<ITextBuffer>(), Arg.Any<List<ChangesDto>>()).Returns([acceptedChange]);

        testSubject.ApplyFixSuggestion(suggestionWithTwoChanges);

        textViewEditor.Received(1).ApplyChanges(textView.TextBuffer, Arg.Is<List<ChangesDto>>(x => x.SequenceEqual(new List<ChangesDto> { acceptedChange })), abortOnOriginalTextChanged: true);
    }

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
        textView.TextBuffer.Returns(Substitute.For<ITextBuffer>());
        documentNavigator.Open(Arg.Any<string>()).Returns(textView);
        return textView;
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

    private void VerifyFixSuggestionNotApplied() => fixSuggestionNotification.Received(1).UnableToLocateIssue(Arg.Is<string>(msg => msg == GetAbsolutePathOfFile(suggestionWithOneChange)));

    private void MockDiffView(List<ChangesDto> changes) => diffViewService.ShowDiffView(Arg.Any<ITextBuffer>(), Arg.Any<List<ChangesDto>>()).Returns(changes);
}

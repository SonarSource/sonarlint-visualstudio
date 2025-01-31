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
    private const string ConfigScopeId = "scopeId";
    private const string SuggestionId = "suggestionKey";
    private const string IdePath = @"myFile.cs";
    private readonly List<FixSuggestionChange> OneChange = [CreateChanges(1, 1, "var a=1;")];
    private readonly List<FixSuggestionChange> TwoChanges = [CreateChanges(1, 1, "var a=1;"), CreateChanges(1, 1, "var b=0;")];
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
        var testSubjectNew = new FixSuggestionHandler(
            threadHandlingMock,
            logger,
            documentNavigator,
            textViewEditor,
            openInIdeConfigScopeValidator,
            ideWindowService,
            fixSuggestionNotification,
            diffViewService);

        testSubjectNew.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, OneChange);

        threadHandlingMock.ReceivedWithAnyArgs().RunOnUIThread(default);
    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChangeAccepted_AppliesChange()
    {
        MockOpenFile();
        MockDiffViewWitAcceptedChanges(OneChange, OneChange);

        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, OneChange);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, ConfigScopeId, SuggestionId);
            ideWindowService.BringToFront();
            fixSuggestionNotification.Clear();
            documentNavigator.Open(@"C:\myFile.cs");
            diffViewService.ShowDiffView(Arg.Any<ITextBuffer>(), OneChange);
            textViewEditor.ApplyChanges(Arg.Any<ITextBuffer>(), Arg.Is<IReadOnlyList<FixSuggestionChange>>(x => x.SequenceEqual(OneChange)), true);
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, ConfigScopeId, SuggestionId);
        });

    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChangeNotAccepted_DoesNotApplyChange()
    {
        MockOpenFile();
        MockDiffViewWitAcceptedChanges(OneChange, []);

        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, OneChange);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, ConfigScopeId, SuggestionId);
            ideWindowService.BringToFront();
            fixSuggestionNotification.Clear();
            documentNavigator.Open(@"C:\myFile.cs");
            diffViewService.ShowDiffView(Arg.Any<ITextBuffer>(), OneChange);
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, ConfigScopeId, SuggestionId);
        });
        textViewEditor.DidNotReceiveWithAnyArgs().ApplyChanges(default, default, default);
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChange_BringWindowToFront()
    {
        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, OneChange);

        ideWindowService.Received().BringToFront();
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChangeSucceeded_BringFocusLineOfToFirstAcceptedChange()
    {
        var textView = MockOpenFile();
        MockDiffViewWitAcceptedChanges(TwoChanges, [TwoChanges[1]]);
        textViewEditor.ApplyChanges(Arg.Any<ITextBuffer>(), Arg.Any<IReadOnlyList<FixSuggestionChange>>(), Arg.Any<bool>()).Returns(true);

        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, TwoChanges);

        textViewEditor.Received(1).FocusLine(textView, TwoChanges[1].BeforeStartLine);
    }

    [TestMethod]
    public void ApplyFixSuggestion_Throws_Logs()
    {
        MockDiffViewWitAcceptedChanges(TwoChanges, TwoChanges);
        var exceptionMsg = "error";
        textViewEditor.ApplyChanges(Arg.Any<ITextBuffer>(), Arg.Any<IReadOnlyList<FixSuggestionChange>>(), Arg.Any<bool>()).Throws(new Exception(exceptionMsg));

        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, TwoChanges);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, ConfigScopeId, SuggestionId);
            logger.WriteLine(FixSuggestionResources.ProcessingRequestFailed, ConfigScopeId, SuggestionId, exceptionMsg);
        });
        logger.DidNotReceive().WriteLine(FixSuggestionResources.DoneProcessingRequest, ConfigScopeId, SuggestionId);
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenConfigRootScopeNotFound_ShouldLogFailureAndShowNotification()
    {
        var reason = "Scope not found";
        MockFailedConfigScopeRoot(reason);

        testSubject.ApplyFixSuggestion("SOMEOTHERSCOPE", SuggestionId, IdePath, OneChange);

        logger.Received().WriteLine(FixSuggestionResources.GetConfigScopeRootPathFailed, "SOMEOTHERSCOPE", "Scope not found");
        fixSuggestionNotification.Received(1).InvalidRequest(reason);
    }

    [TestMethod]
    public void ApplyFixSuggestion_FileCanNotBeOpened_LogsAndShowsNotification()
    {
        var errorMessage = "error";
        documentNavigator.Open(Arg.Any<string>()).Throws(new Exception(errorMessage));

        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, OneChange);

        logger.Received().WriteLine(Resources.ERR_OpenDocumentException, GetAbsolutePathOfFile(IdePath), errorMessage);
        fixSuggestionNotification.Received(1).UnableToOpenFile(Arg.Is<string>(msg => msg == GetAbsolutePathOfFile(IdePath)));
    }

    [TestMethod]
    public void ApplyFixSuggestion_FileContentIsNull_LogsAndShowsNotification()
    {
        documentNavigator.Open(Arg.Any<string>()).Returns((ITextView)null);

        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, OneChange);

        logger.DidNotReceive().WriteLine(Resources.ERR_OpenDocumentException, Arg.Any<string>(), Arg.Any<string>());
        fixSuggestionNotification.Received(1).UnableToOpenFile(Arg.Is<string>(msg => msg == GetAbsolutePathOfFile(IdePath)));
    }

    [TestMethod]
    public void ApplyFixSuggestion_ClearsPreviousNotification()
    {
        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, OneChange);

        fixSuggestionNotification.Received(1).Clear();
    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChange_ChangesCanNotBeApplied_ShowsNotification()
    {
        MockDiffViewWitAcceptedChanges(OneChange, OneChange);
        textViewEditor.ApplyChanges(Arg.Any<ITextBuffer>(), Arg.Any<IReadOnlyList<FixSuggestionChange>>(), Arg.Any<bool>()).Returns(false);

        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, OneChange);

        VerifyFixSuggestionNotApplied();
    }

    [TestMethod]
    public void ApplyFixSuggestion_TwoChanges_ShowsCorrectDiffView()
    {
        var textView = MockOpenFile();

        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, TwoChanges);

        diffViewService.Received(1).ShowDiffView(textView.TextBuffer, TwoChanges);
    }

    [TestMethod]
    public void ApplyFixSuggestion_TwoChangesAndJustOneAccepted_AppliesJustOne()
    {
        var textView = MockOpenFile();
        MockDiffViewWitAcceptedChanges(TwoChanges, [TwoChanges[0]]);

        testSubject.ApplyFixSuggestion(ConfigScopeId, SuggestionId, IdePath, TwoChanges);

        textViewEditor.Received(1).ApplyChanges(textView.TextBuffer, Arg.Is<IReadOnlyList<FixSuggestionChange>>(x => x.SequenceEqual(new List<FixSuggestionChange> { TwoChanges[0] })), abortOnOriginalTextChanged: true);
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

        params ChangesDto[] changes)
    {
        var fixSuggestion = new FixSuggestionDto(SuggestionId, "refactor", new FileEditDto(IdePath, changes.ToList()));
        var suggestionParams = new ShowFixSuggestionParams(ConfigScopeId, "key", fixSuggestion);
        return suggestionParams;
    }

    private static FixSuggestionChange CreateChanges(
        int startLine,
        int endLine,
        string before,
        string after = "") =>
        new(startLine, endLine, before, after);

    private static string GetAbsolutePathOfFile(string idePath) => Path.Combine(ConfigurationScopeRoot, idePath);

    private void VerifyFixSuggestionNotApplied() => fixSuggestionNotification.Received(1).UnableToLocateIssue(Arg.Is<string>(msg => msg == GetAbsolutePathOfFile(IdePath)));

    private void MockDiffViewWitAcceptedChanges(IEnumerable<FixSuggestionChange> changes, List<FixSuggestionChange> acceptedChanges)
    {
        diffViewService.ShowDiffView(Arg.Any<ITextBuffer>(), Arg.Any<List<FixSuggestionChange>>()).Returns(changes.Select(x => new FinalizedFixSuggestionChange(x, acceptedChanges.Contains(x))).ToArray());
    }
}

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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using FileEditDto = SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models.FileEditDto;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.FixSuggestion;

[TestClass]
public class FixSuggestionHandlerTests
{
    private readonly FixSuggestionDto suggestionDto = new("id", "refactor", new FileEditDto(@"C:\myFile.cs", [new ChangesDto(new LineRangeDto(1, 2), "var a=1;", "")]));
    private FixSuggestionHandler testSubject;
    private IThreadHandling threadHandling;
    private ILogger logger;
    private IDocumentNavigator documentNavigator;
    private ISpanTranslator spanTranslator;
    private IIssueSpanCalculator issueSpanCalculator;

    [TestInitialize]
    public void TestInitialize()
    {
        threadHandling = new NoOpThreadHandler();
        logger = Substitute.For<ILogger>();
        documentNavigator = Substitute.For<IDocumentNavigator>();
        spanTranslator = Substitute.For<ISpanTranslator>();
        issueSpanCalculator = Substitute.For<IIssueSpanCalculator>();

        testSubject = new FixSuggestionHandler(threadHandling, logger, documentNavigator, spanTranslator, issueSpanCalculator);
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<FixSuggestionHandler>();
    }

    [TestMethod]
    public void ApplyFixSuggestion_RunsOnUIThread()
    {
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        var fixSuggestionHandler = new FixSuggestionHandler(threadHandlingMock, logger, documentNavigator, spanTranslator, issueSpanCalculator);

        fixSuggestionHandler.ApplyFixSuggestion(CreateFixSuggestionParams());

        threadHandlingMock.ReceivedWithAnyArgs().RunOnUIThread(default);
    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChange_AppliesChange()
    {
        var suggestionParams = CreateFixSuggestionParams();
        var suggestedChange = suggestionParams.fixSuggestion.fileEdit.changes[0];
        var affectedSnapshot = MockCalculateSpan(suggestedChange);
        var edit = Substitute.For<ITextEdit>();
        var textView = MockTextView(edit);
        MockOpenDocument(suggestionParams, textView);

        testSubject.ApplyFixSuggestion(suggestionParams);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId);
            documentNavigator.Open(suggestionDto.fileEdit.idePath);
            textView.TextBuffer.CreateEdit();
            issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), suggestedChange.beforeLineRange.startLine, suggestedChange.beforeLineRange.endLine);
            spanTranslator.TranslateTo(affectedSnapshot, Arg.Any<ITextSnapshot>(), SpanTrackingMode.EdgeExclusive);
            edit.Replace(Arg.Any<Span>(), suggestedChange.after);
            edit.Apply();
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId);
        });
    }

    [TestMethod]
    public void ApplyFixSuggestion_Throws_Logs()
    {
        var suggestionParams = CreateFixSuggestionParams();
        var exceptionMsg = "error";
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>()).Throws(new Exception(exceptionMsg));
        
        testSubject.ApplyFixSuggestion(suggestionParams);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId);
            logger.WriteLine(FixSuggestionResources.ProcessingRequestFailed, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId, exceptionMsg);
        });
        logger.DidNotReceive().WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId);
    }

    private SnapshotSpan MockCalculateSpan(ChangesDto suggestedChange)
    {
        var affectedSnapshot = new SnapshotSpan();
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), suggestedChange.beforeLineRange.startLine, suggestedChange.beforeLineRange.endLine)
            .Returns(affectedSnapshot);
        return affectedSnapshot;
    }

    private void MockOpenDocument(ShowFixSuggestionParams suggestionParams, ITextView textView)
    {
        documentNavigator.Open(suggestionParams.fixSuggestion.fileEdit.idePath).Returns(textView);
    }

    private static ITextView MockTextView(ITextEdit edit)
    {
        var textView = Substitute.For<ITextView>();
        textView.TextBuffer.CreateEdit().Returns(edit);
        return textView;
    }

    private ShowFixSuggestionParams CreateFixSuggestionParams()
    {
        return new ShowFixSuggestionParams("scopeId", "key", suggestionDto);
    }
}

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
    
    private readonly FixSuggestionDto suggestionDto = new("scopeId", "refactor", new FileEditDto(@"myFile.cs", [new ChangesDto(new LineRangeDto(1, 2), "var a=1;", "")]));
    
    private FixSuggestionHandler testSubject;
    private IThreadHandling threadHandling;
    private ILogger logger;
    private IDocumentNavigator documentNavigator;
    private ISpanTranslator spanTranslator;
    private IIssueSpanCalculator issueSpanCalculator;
    private IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator;

    [TestInitialize]
    public void TestInitialize()
    {
        threadHandling = new NoOpThreadHandler();
        logger = Substitute.For<ILogger>();
        documentNavigator = Substitute.For<IDocumentNavigator>();
        spanTranslator = Substitute.For<ISpanTranslator>();
        issueSpanCalculator = Substitute.For<IIssueSpanCalculator>();
        openInIdeConfigScopeValidator = Substitute.For<IOpenInIdeConfigScopeValidator>();

        testSubject = new FixSuggestionHandler(threadHandling, logger, documentNavigator, spanTranslator, issueSpanCalculator, openInIdeConfigScopeValidator);
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<FixSuggestionHandler>();
    }

    [TestMethod]
    public void ApplyFixSuggestion_RunsOnUIThread()
    {
        MockConfigScopeRoot();
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        var fixSuggestionHandler = new FixSuggestionHandler(threadHandlingMock, logger, documentNavigator, spanTranslator, issueSpanCalculator, openInIdeConfigScopeValidator);

        fixSuggestionHandler.ApplyFixSuggestion(CreateFixSuggestionParams());

        threadHandlingMock.ReceivedWithAnyArgs().RunOnUIThread(default);
    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChange_AppliesChange()
    {
        var suggestionParams = CreateFixSuggestionParams();
        var suggestedChange = suggestionParams.fixSuggestion.fileEdit.changes[0];
        var affectedSnapshot = MockCalculateSpan(suggestedChange);
        var textView = MockOpenFile();
        var edit = Substitute.For<ITextEdit>();
        textView.TextBuffer.CreateEdit().Returns(edit);
        MockConfigScopeRoot();

        testSubject.ApplyFixSuggestion(suggestionParams);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId);
            documentNavigator.Open(@"C:\myFile.cs");
            textView.TextBuffer.CreateEdit();
            issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), suggestedChange.beforeLineRange.startLine, suggestedChange.beforeLineRange.endLine);
            spanTranslator.TranslateTo(affectedSnapshot, Arg.Any<ITextSnapshot>(), SpanTrackingMode.EdgeExclusive);
            edit.Replace(Arg.Any<Span>(), suggestedChange.after);
            edit.Apply();
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId);
        });
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChange_BringFocusToChangedLines()
    {
        var suggestionParams = CreateFixSuggestionParams();
        var suggestedChange = suggestionParams.fixSuggestion.fileEdit.changes[0];
        var affectedSnapshot = MockCalculateSpan(suggestedChange);
        var textView = MockOpenFile();
        MockConfigScopeRoot();

        testSubject.ApplyFixSuggestion(suggestionParams);
        
        textView.ViewScroller.Received().EnsureSpanVisible(affectedSnapshot, EnsureSpanVisibleOptions.AlwaysCenter);
    }

    [TestMethod]
    public void ApplyFixSuggestion_Throws_Logs()
    {
        var suggestionParams = CreateFixSuggestionParams();
        var exceptionMsg = "error";
        MockConfigScopeRoot();
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>()).Throws(new Exception(exceptionMsg));
        
        testSubject.ApplyFixSuggestion(suggestionParams);

        Received.InOrder(() =>
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId);
            logger.WriteLine(FixSuggestionResources.ProcessingRequestFailed, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId, exceptionMsg);
        });
        logger.DidNotReceive().WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId);
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenConfigRootScopeNotFound_ShouldLogFailure()
    {
        MockFailedConfigScopeRoot("Scope not found");
        var suggestionParams = CreateFixSuggestionParams("SpecificConfigScopeId");
        
        testSubject.ApplyFixSuggestion(suggestionParams);
        
        logger.Received().WriteLine(FixSuggestionResources.GetConfigScopeRootPathFailed, "SpecificConfigScopeId", "Scope not found");
    }

    private void MockConfigScopeRoot()
    {
        
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(Arg.Any<string>(), out Arg.Any<string>(), out Arg.Any<string>()).Returns(
            x =>
            {
                x[1] = ConfigurationScopeRoot;
                return true;
            });
    }
    
    private void MockFailedConfigScopeRoot(string failureReason)
    {
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(Arg.Any<string>(), out Arg.Any<string>(), out Arg.Any<string>()).Returns(
            x =>
            {
                x[2] = failureReason;
                return false;
            });
    }

    private ITextView MockOpenFile()
    {
        var textView = Substitute.For<ITextView>();
        documentNavigator.Open(Arg.Any<string>()).Returns(textView);
        return textView;
    }

    private SnapshotSpan MockCalculateSpan(ChangesDto suggestedChange)
    {
        var affectedSnapshot = new SnapshotSpan();
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), suggestedChange.beforeLineRange.startLine, suggestedChange.beforeLineRange.endLine)
            .Returns(affectedSnapshot);
        return affectedSnapshot;
    }

    private ShowFixSuggestionParams CreateFixSuggestionParams(string scopeId = "scopeId")
    {
        return new ShowFixSuggestionParams(scopeId, "key", suggestionDto);
    }
}

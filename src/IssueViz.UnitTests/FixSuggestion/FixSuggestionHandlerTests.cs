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
    
    private FixSuggestionHandler testSubject;
    private IThreadHandling threadHandling;
    private ILogger logger;
    private IDocumentNavigator documentNavigator;
    private IIssueSpanCalculator issueSpanCalculator;
    private IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator;
    private IIDEWindowService ideWindowService;

    [TestInitialize]
    public void TestInitialize()
    {
        threadHandling = new NoOpThreadHandler();
        logger = Substitute.For<ILogger>();
        documentNavigator = Substitute.For<IDocumentNavigator>();
        issueSpanCalculator = Substitute.For<IIssueSpanCalculator>();
        openInIdeConfigScopeValidator = Substitute.For<IOpenInIdeConfigScopeValidator>();
        ideWindowService = Substitute.For<IIDEWindowService>();

        testSubject = new FixSuggestionHandler(threadHandling, logger, documentNavigator, issueSpanCalculator, openInIdeConfigScopeValidator, ideWindowService);
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
        var fixSuggestionHandler = new FixSuggestionHandler(threadHandlingMock, logger, documentNavigator, issueSpanCalculator, openInIdeConfigScopeValidator, ideWindowService);

        fixSuggestionHandler.ApplyFixSuggestion(CreateFixSuggestionParams());

        threadHandlingMock.ReceivedWithAnyArgs().RunOnUIThread(default);
    }

    [TestMethod]
    public void ApplyFixSuggestion_OneChange_AppliesChange()
    {
        var suggestionParams = CreateFixSuggestionParams();
        var suggestedChange = suggestionParams.fixSuggestion.fileEdit.changes[0];
        MockCalculateSpan(suggestedChange);
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
            edit.Replace(Arg.Any<Span>(), suggestedChange.after);
            edit.Apply();
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, suggestionParams.configurationScopeId, suggestionParams.fixSuggestion.suggestionId);
        });
    }

    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChange_BringWindowToFront()
    {
        var suggestionParams = CreateFixSuggestionParams();
        MockConfigScopeRoot();

        testSubject.ApplyFixSuggestion(suggestionParams);
        
        ideWindowService.Received().BringToFront();
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
    
    [TestMethod]
    public void ApplyFixSuggestion_WhenLineNumbersDoNotMatch_ShouldLogFailure()
    {
        FailWhenApplyingEdit(out var suggestionWithWrongLineNumbers, "Line numbers do not match");
        
        testSubject.ApplyFixSuggestion(suggestionWithWrongLineNumbers);
        
        logger.Received().WriteLine(FixSuggestionResources.ProcessingRequestFailed, "AScopeId", "key", "Line numbers do not match");
    }
    
    [TestMethod]
    public void ApplyFixSuggestion_WhenApplyingChangeAndExceptionIsThrown_ShouldCancelEdit()
    {
        var edit = FailWhenApplyingEdit(out var suggestionWithWrongLineNumbers);

        testSubject.ApplyFixSuggestion(suggestionWithWrongLineNumbers);
        
        edit.DidNotReceiveWithAnyArgs().Replace(default, default);
        edit.Received().Cancel();
    }
    
    private static ShowFixSuggestionParams CreateFixSuggestionParams(string scopeId = "scopeId", string suggestionKey = "suggestionKey", string idePath = @"myFile.cs", List<ChangesDto> changes = null)
    {
        changes ??= [CreateChangesDto()];
        var fixSuggestion = new FixSuggestionDto(suggestionKey, "refactor", new FileEditDto(idePath, changes));
        var suggestionParams = new ShowFixSuggestionParams(scopeId, "key", fixSuggestion);
        return suggestionParams;
    }

    private static ChangesDto CreateChangesDto(int startLine = 1, int endLine = 2)
    {
        return new ChangesDto(new LineRangeDto(startLine, endLine), "var a=1;", "");
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
    
    private ITextEdit FailWhenApplyingEdit(out ShowFixSuggestionParams suggestionWithWrongLineNumbers, string reason = "")
    {
        MockConfigScopeRoot();
        var edit = Substitute.For<ITextEdit>();
        var textView = MockOpenFile();
        textView.TextBuffer.CreateEdit().Returns(edit);
        suggestionWithWrongLineNumbers = CreateFixSuggestionParams(scopeId: "AScopeId", suggestionKey: "key");
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>())
            .Throws(new Exception(reason));
        return edit;
    }
}

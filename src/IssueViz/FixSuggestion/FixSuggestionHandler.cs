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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

[Export(typeof(IFixSuggestionHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class FixSuggestionHandler : IFixSuggestionHandler
{
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private readonly IDocumentNavigator documentNavigator;
    private readonly ISpanTranslator spanTranslator;
    private readonly IIssueSpanCalculator issueSpanCalculator;

    [ImportingConstructor]
    internal FixSuggestionHandler(ILogger logger, IDocumentNavigator documentNavigator, IIssueSpanCalculator issueSpanCalculator) : 
        this(ThreadHandling.Instance, logger, documentNavigator, new SpanTranslator(), issueSpanCalculator)
    {
    }

    internal FixSuggestionHandler(
        IThreadHandling threadHandling,
        ILogger logger,
        IDocumentNavigator documentNavigator,
        ISpanTranslator spanTranslator,
        IIssueSpanCalculator issueSpanCalculator)
    {
        this.threadHandling = threadHandling;
        this.logger = logger;
        this.documentNavigator = documentNavigator;
        this.spanTranslator = spanTranslator;
        this.issueSpanCalculator = issueSpanCalculator;
    }

    public void ApplyFixSuggestion(ShowFixSuggestionParams parameters)
    {
        threadHandling.RunOnUIThread(() =>
        {
            ApplyFixSuggestionInternal(parameters);
        });
    }

    private void ApplyFixSuggestionInternal(ShowFixSuggestionParams parameters)
    {
        try
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, parameters.configurationScopeId, parameters.fixSuggestion.suggestionId);

            var textView = documentNavigator.Open(parameters.fixSuggestion.fileEdit.idePath);
            ApplySuggestedChanges(textView, parameters.fixSuggestion);

            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, parameters.configurationScopeId, parameters.fixSuggestion.suggestionId);
        }
        catch (Exception exception) when (!ErrorHandler.IsCriticalException(exception))
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequestFailed, parameters.configurationScopeId, parameters.fixSuggestion.suggestionId, exception.Message);
        }
    }

    private void ApplySuggestedChanges(ITextView textView, FixSuggestionDto fixSuggestion)
    {
        var textEdit = textView.TextBuffer.CreateEdit();
        foreach (var changeDto in fixSuggestion.fileEdit.changes)
        {
            var updatedSpan = CalculateSnapshotSpan(textView, changeDto);
            textEdit.Replace(updatedSpan, changeDto.after);
        }
        textEdit.Apply();
    }

    private SnapshotSpan CalculateSnapshotSpan(ITextView textView, ChangesDto changeDto)
    {
        var snapshotSpan = issueSpanCalculator.CalculateSpan(textView.TextSnapshot, changeDto.beforeLineRange.startLine, changeDto.beforeLineRange.endLine);
        return spanTranslator.TranslateTo(snapshotSpan, textView.TextBuffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive);
    }
}

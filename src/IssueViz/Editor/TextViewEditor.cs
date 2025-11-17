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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

namespace SonarLint.VisualStudio.IssueVisualization.Editor;

public interface ITextViewEditor
{
    bool ApplyChanges(ITextBuffer textBuffer, IReadOnlyList<FixSuggestionChange> changes, bool abortOnOriginalTextChanged);

    void FocusLine(ITextView textView, int lineNumber);

    ITextBuffer CreateTextBuffer(string text, IContentType contentType);
}

[Export(typeof(ITextViewEditor))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class TextViewEditor(IIssueSpanCalculator issueSpanCalculator, ILogger logger, ITextBufferFactoryService textBufferFactoryService) : ITextViewEditor
{
    private readonly ILogger logger = logger.ForContext(nameof(TextViewEditor));

    public bool ApplyChanges(ITextBuffer textBuffer, IReadOnlyList<FixSuggestionChange> changes, bool abortOnOriginalTextChanged)
    {
        using var textEdit = textBuffer.CreateEdit();
        for (var i = changes.Count - 1; i >= 0; i--)
        {
            var change = changes[i];
            var spanToUpdate = issueSpanCalculator.CalculateSpan(textBuffer.CurrentSnapshot, change.BeforeStartLine, change.BeforeEndLine);
            if (abortOnOriginalTextChanged && !IsSameOriginalText(spanToUpdate, change.BeforeText))
            {
                return false;
            }
            textEdit.Replace(spanToUpdate.Value, change.AfterText);
        }
        textEdit.Apply();

        return true;
    }

    public void FocusLine(ITextView textView, int lineNumber)
    {
        try
        {
            var line = textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber);
            textView.Caret.MoveTo(line.Start);
            textView.ViewScroller.EnsureSpanVisible(line.Extent);
        }
        catch (Exception ex)
        {
            // no need to crash the editor if we can't focus the line
            logger.LogVerbose(Resources.FocusLineFailed, lineNumber, ex.Message);
        }
    }

    public ITextBuffer CreateTextBuffer(string text, IContentType contentType) => textBufferFactoryService.CreateTextBuffer(text, contentType);

    private bool IsSameOriginalText(SnapshotSpan? spanToUpdate, string beforeText) => spanToUpdate.HasValue && issueSpanCalculator.IsSameHash(spanToUpdate.Value, beforeText);
}

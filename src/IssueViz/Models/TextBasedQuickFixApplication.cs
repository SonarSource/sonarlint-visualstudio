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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.IssueVisualization.Models;

internal class TextBasedQuickFixApplication(ITextBasedQuickFixVisualization textBasedQuickFixVisualization, ISpanTranslator spanTranslator) : IQuickFixApplication
{
    public ITextBasedQuickFixVisualization QuickFixVisualization { get; } = textBasedQuickFixVisualization;

    public string Message => QuickFixVisualization.Fix.Message;

    public bool CanBeApplied(ITextSnapshot currentSnapshot) => QuickFixVisualization.CanBeApplied(currentSnapshot);

    public Task<bool> ApplyAsync(ITextSnapshot currentSnapshot, CancellationToken cancellationToken)
    {
        var textBuffer = currentSnapshot.TextBuffer;
        var textEdit = textBuffer.CreateEdit();

        foreach (var edit in QuickFixVisualization.EditVisualizations)
        {
            var updatedSpan = spanTranslator.TranslateTo(edit.Span, textBuffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive);

            textEdit.Replace(updatedSpan, edit.Edit.NewText);
        }

        cancellationToken.ThrowIfCancellationRequested();

        textEdit.Apply();
        return Task.FromResult(true);
    }
}

﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.Threading;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes
{
    internal class QuickFixSuggestedAction : BaseSuggestedAction
    {
        private readonly IQuickFixVisualization quickFixVisualization;
        private readonly ITextBuffer textBuffer;

        public QuickFixSuggestedAction(IQuickFixVisualization quickFixVisualization, ITextBuffer textBuffer)
        {
            this.quickFixVisualization = quickFixVisualization;
            this.textBuffer = textBuffer;
        }

        public override string DisplayText => quickFixVisualization.Fix.Message;

        public override void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var textEdit = textBuffer.CreateEdit();

            foreach (var edit in quickFixVisualization.EditVisualizations)
            {
                //var updatedSpan = edit.Span.TranslateTo(textBuffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive);

                textEdit.Replace(edit.Span, edit.Edit.Text);
            }

            textEdit.Apply();
        }
    }
}

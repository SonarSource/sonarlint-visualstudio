/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes
{
    internal class QuickFixSuggestedAction : ISuggestedAction
    {
        private readonly IIssueSpanCalculator spanCalculator;
        private readonly ITextSnapshot snapshot;
        private readonly SnapshotSpan range;
        private readonly IReadOnlyList<IAnalysisIssueFixEdit> edits;

        public QuickFixSuggestedAction(IIssueSpanCalculator spanCalculator,
            ITextSnapshot snapshot, 
            SnapshotSpan range, 
            IAnalysisIssueFix fix)
        {
            this.spanCalculator = spanCalculator;
            this.snapshot = snapshot;
            this.range = range;
            DisplayText = fix.Message;
            edits = fix.Edits;
        }

        public string DisplayText { get; }

        public string IconAutomationText => null;

        ImageMoniker ISuggestedAction.IconMoniker => default;

        public string InputGestureText => null;

        public bool HasActionSets => false;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public bool HasPreview => false;

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);

            //var textBlock = new TextBlock
            //{
            //    Padding = new Thickness(5)
            //};

            //foreach (var edit in edits)
            //{
            //    textBlock.Inlines.Add(new Run { Text = edit.Text });
            //}

            //return Task.FromResult<object>(textBlock);
        }

        public void Dispose()
        {
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            foreach (var edit in edits)
            {
                var span = spanCalculator.CalculateSpan(edit, snapshot);

                var trackingSpan =
                    range.Snapshot.CreateTrackingSpan(span,
                        SpanTrackingMode.EdgeInclusive);

                trackingSpan.TextBuffer.Replace(trackingSpan.GetSpan(snapshot), edit.Text);
            }
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
